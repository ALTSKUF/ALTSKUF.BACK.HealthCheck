using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

public class RpcClientBackgroundService : BackgroundService
{
    private const string QUEUE_NAME = "test_queue";

    private readonly IConnectionFactory _connectionFactory;
    private IConnection? _connection;
    private IChannel? _channel;

    public RpcClientBackgroundService()
    {
        _connectionFactory = new ConnectionFactory { HostName = "localhost" };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _connection = await _connectionFactory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();

        await _channel.QueueDeclareAsync(queue: QUEUE_NAME, durable: false, exclusive: false,
            autoDelete: false, arguments: null);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            byte[] body = ea.Body.ToArray();
            IReadOnlyBasicProperties props = ea.BasicProperties;
            var replyProps = new BasicProperties
            {
                CorrelationId = props.CorrelationId
            };

            try
            {
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine($" [.] Received message from server: {message}");

                string response = $"Processed: {message} at {DateTime.Now}";
                var responseBytes = Encoding.UTF8.GetBytes(response);

                await _channel.BasicPublishAsync(
                    exchange: string.Empty,
                    routingKey: props.ReplyTo,
                    mandatory: true,
                    basicProperties: replyProps,
                    body: responseBytes
                );

                Console.WriteLine($" [x] Sent response to server: {response}");
            }
            catch (Exception e)
            {
                Console.WriteLine($" [.] Error: {e.Message}");
            }
        };

        await _channel.BasicConsumeAsync(QUEUE_NAME, true, consumer);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null)
        {
            await _channel.CloseAsync();
        }

        if (_connection is not null)
        {
            await _connection.CloseAsync();
        }

        await base.StopAsync(cancellationToken);
    }
}