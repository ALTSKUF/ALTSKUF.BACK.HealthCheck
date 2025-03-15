using Confluent.Kafka;
// Фоновая служба для обработки запросов и отправки ответов
public class KafkaRequestHandlerService : IHostedService
{
    private readonly IConsumer<Null, string> _consumer;
    private readonly IProducer<Null, string> _producer;
    private readonly ILogger<KafkaRequestHandlerService> _logger;
    private Task _consumingTask;
    private CancellationTokenSource _cancellationTokenSource;

    public KafkaRequestHandlerService(
        IConsumer<Null, string> consumer,
        IProducer<Null, string> producer,
        ILogger<KafkaRequestHandlerService> logger)
    {
        _consumer = consumer;
        _producer = producer;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Kafka Request Handler Service started.");

        _cancellationTokenSource = new CancellationTokenSource();
        _consumingTask = Task.Run(() => HandleRequests(_cancellationTokenSource.Token));

        return Task.CompletedTask;
    }

    private async Task HandleRequests(CancellationToken cancellationToken)
    {
        _consumer.Subscribe("request-topic");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = _consumer.Consume(cancellationToken);
                var correlationId = System.Text.Encoding.UTF8.GetString(consumeResult.Message.Headers.First(h => h.Key == "CorrelationId").GetValueBytes());

                _logger.LogInformation($"Received request: {consumeResult.Message.Value}");

                // Обрабатываем запрос и формируем ответ
                var replyMessage = $"Reply to request (CorrelationId: {correlationId}) at {DateTime.UtcNow}";

                // Отправляем ответ в топик reply-topic
                _producer.Produce("reply-topic", new Message<Null, string>
                {
                    Value = replyMessage,
                    Headers = new Headers { new Header("CorrelationId", System.Text.Encoding.UTF8.GetBytes(correlationId)) }
                });

                _logger.LogInformation($"Sent reply: {replyMessage}");
            }
            catch (OperationCanceledException)
            {
                // Игнорируем, если операция была отменена
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error handling request: {ex.Message}");
            }
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Kafka Request Handler Service stopped.");

        _cancellationTokenSource.Cancel();
        await _consumingTask;

        _consumer.Close();
        _consumer.Dispose();
    }
}