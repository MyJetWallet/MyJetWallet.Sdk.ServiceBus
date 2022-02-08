using System.Runtime.Serialization;
using Microsoft.Extensions.Logging.Abstractions;
using MyJetWallet.Sdk.ServiceBus;
using MyServiceBus.Abstractions;
using MyServiceBus.TcpClient;

Console.WriteLine("Press Enter to start");
Console.ReadLine();

var sbClient = new MyServiceBusTcpClient(() => "192.168.70.80:6421", "ServiceBusTestApp");
sbClient.Start();
var pub = new MyServiceBusPublisher<TestMessage>(sbClient, TestMessage.TopicName, false);

var deduplicator = new MyServiceBusDeduplicator<TestMessage>(t => t.Id.ToString(), () => "http://192.168.70.80:5123",
    "test-dedup-table", TestMessage.TopicName, TimeSpan.FromMinutes(1), new NullLogger<Program>());
var sub = new MyServiceBusSubscriber<TestMessage>(sbClient, TestMessage.TopicName, "ServiceBusTestApp",
    TopicQueueType.DeleteOnDisconnect, deduplicator);

sub.Subscribe(Handle);

for (var i = 0; i <= 100; i++)
{
    var id = i;
    if (i % 5 == 0)
        id = 1111111;
    
    await pub.PublishAsync(new TestMessage()
    {
        Id = id
    });
}


ValueTask Handle(TestMessage message)
{
    Console.WriteLine(message.Id);
    return ValueTask.CompletedTask;
}


Console.WriteLine("Press Enter to exit");
Console.ReadLine();

[DataContract]
public class TestMessage
{
    public const string TopicName = "test-message";
    [DataMember(Order = 1)]public int Id;
}