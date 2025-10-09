using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Threading.Tasks;

Console.WriteLine("Starting RabbitMQ smoke test...");

var factory = new ConnectionFactory() { HostName = "localhost", Port = 5672 };
using var connection = factory.CreateConnection();
using var channel = connection.CreateModel();

var exchange = "mordecai.game.events";
channel.ExchangeDeclare(exchange, ExchangeType.Topic, durable: true);

// Subscriber: server-named queue
var q = channel.QueueDeclare().QueueName;
channel.QueueBind(q, exchange, "chat.*.*");

var consumer = new EventingBasicConsumer(channel);
var receivedTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
consumer.Received += (s, ea) =>
{
    var body = ea.Body.ToArray();
    var json = Encoding.UTF8.GetString(body);
    Console.WriteLine("Subscriber received: " + json);
    receivedTcs.TrySetResult(true);
};
channel.BasicConsume(q, autoAck: true, consumer);

// Publisher: send a chat.global message
var message = new { MessageType = "Test", Text = "Hello from smoke test" };
var jsonText = JsonSerializer.Serialize(message);
var bodyBytes = Encoding.UTF8.GetBytes(jsonText);
channel.BasicPublish(exchange, "chat.test.1", null, bodyBytes);
Console.WriteLine("Published message: " + jsonText);

// Wait for subscriber to receive (timeout 10s)
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
try
{
    await receivedTcs.Task.WaitAsync(cts.Token);
    Console.WriteLine("Smoke test succeeded: message received");
}
catch (Exception ex)
{
    Console.WriteLine("Smoke test failed: " + ex.Message);
}

Console.WriteLine("Exiting smoke test");