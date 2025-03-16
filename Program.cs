using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Threading.Tasks;

Console.WriteLine("Consumer started. Waiting for messages...");

// Асинхронный метод для запуска получателя
await StartListeningAsync();

async Task StartListeningAsync()
{
    var factory = new ConnectionFactory() { HostName = "localhost" };

    // Асинхронное создание соединения
    using var connection = await factory.CreateConnectionAsync();
    using var channel = await connection.CreateChannelAsync();

    // Объявляем очереди
    await channel.QueueDeclareAsync(
        queue: "request_queue",
        durable: false,
        exclusive: false,
        autoDelete: false,
        arguments: null);

    await channel.QueueDeclareAsync(
        queue: "response_queue",
        durable: false,
        exclusive: false,
        autoDelete: false,
        arguments: null);

    // Создаем асинхронного потребителя
    var consumer = new AsyncEventingBasicConsumer(channel);
    consumer.ReceivedAsync += async (model, ea) =>
    {
        var body = ea.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);
        Console.WriteLine(" [x] Received {0}", message);

        // Обработка сообщения и формирование ответа
        string responseMessage = "Response to: " + message;
        var responseBody = Encoding.UTF8.GetBytes(responseMessage);

        // Асинхронная отправка ответа в очередь
        await channel.BasicPublishAsync(
            exchange: "",
            routingKey: "response_queue",
            body: responseBody);

        Console.WriteLine(" [x] Sent {0}", responseMessage);
    };

    // Начинаем асинхронное прослушивание очереди
    channel.BasicConsumeAsync(
        queue: "request_queue",
        autoAck: true,
        consumer: consumer);

    Console.WriteLine(" Press [Enter] to exit.");
    Console.ReadLine();
}