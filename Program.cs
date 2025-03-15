using Confluent.Kafka;


var builder = WebApplication.CreateBuilder(args);

// Добавляем Kafka Consumer для получения запросов
builder.Services.AddSingleton<IConsumer<Null, string>>(sp =>
{
    var config = new ConsumerConfig
    {
        BootstrapServers = "localhost:9092",
        GroupId = "request-group",
        AutoOffsetReset = AutoOffsetReset.Earliest
    };
    return new ConsumerBuilder<Null, string>(config).Build();
});

// Добавляем Kafka Producer для отправки ответов
builder.Services.AddSingleton<IProducer<Null, string>>(sp =>
{
    var config = new ProducerConfig { BootstrapServers = "localhost:9092" };
    return new ProducerBuilder<Null, string>(config).Build();
});

// Добавляем фоновую службу для обработки запросов и отправки ответов
builder.Services.AddHostedService<KafkaRequestHandlerService>();

// Добавляем HealthChecks
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseHealthChecks("/health");

app.Run();
