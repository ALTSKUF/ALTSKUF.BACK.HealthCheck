using Confluent.Kafka;


var builder = WebApplication.CreateBuilder(args);

var config = new ConsumerConfig
{
    BootstrapServers = "localhost:9092",
    GroupId = "test-group",
    AutoOffsetReset = AutoOffsetReset.Earliest
};

using (var consumer = new ConsumerBuilder<Ignore, string>(config).Build())
{
    consumer.Subscribe("test-topic");

    CancellationTokenSource cts = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) => {
        e.Cancel = true; // prevent the process from terminating.
        cts.Cancel();
    };

    try
    {
        while (true)
        {
            try
            {
                var consumeResult = consumer.Consume(cts.Token);
                Console.WriteLine($"Consumed message '{consumeResult.Message.Value}' at: '{consumeResult.TopicPartitionOffset}'.");
            }
            catch (ConsumeException e)
            {
                Console.WriteLine($"Error occured: {e.Error.Reason}");
            }
        }
    }
    catch (OperationCanceledException)
    {
        // Ensure the consumer leaves the group cleanly and final offsets are committed.
        consumer.Close();
    }
}

builder.Services.AddHealthChecks();

var app = builder.Build();


app.UseHealthChecks("/health");

app.Run();
