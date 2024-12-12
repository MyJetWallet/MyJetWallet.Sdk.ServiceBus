using System.Runtime.Serialization;
using MyJetWallet.Sdk.ServiceBus;
using MyServiceBus.Abstractions;
using MyServiceBus.TcpClient;

Console.WriteLine("Press Enter to start");
Console.ReadLine();

var sbClient = new MyServiceBusTcpClient(() => "myservicebus.tech.svc.cluster.local:6421", "AlexTest");

var pub = new MyServiceBusPublisher<TestMessage>(sbClient, TestMessage.TopicName, false);
pub.SetHeadersHandler(headers => headers.Add("commonKey", "commonValue"));

var sub = new MyServiceBusSubscriber<TestMessage>(sbClient, TestMessage.TopicName, "test-query",
    TopicQueueType.PermanentWithSingleConnection, true, 2);
sub.SetHeadersHandler(headers =>
    Console.WriteLine($"Headers:\n{string.Join('\n', headers.Select(h => $"{h.Key}={h.Value}"))}"));

sub.Subscribe(Handle);

sbClient.Start();

await Task.Delay(10000);
Console.WriteLine("Application is started");
Task.Run(() => Publish(1));

Console.ReadLine();

Console.WriteLine("StartSecond!!!");



var sbClient2 = new MyServiceBusTcpClient(() => "myservicebus.tech.svc.cluster.local:6421", "AlexTest-2");

var sub2 = new MyServiceBusSubscriber<TestMessage>(sbClient2, TestMessage.TopicName, "test-query",
    TopicQueueType.PermanentWithSingleConnection, false, 2);

sub2.Subscribe(Handle2);
//sbClient2.Start();



Console.ReadLine();

sbClient.Stop();
Console.WriteLine("Application is stoped");






async Task Publish(int startValue)
{
    var i = startValue;

    while (i < startValue + 100)
    {
        var headers = new Dictionary<string, string>()
        {
            {"key1", i.ToString()}
        };
        await Task.Delay(5000);
        await pub.PublishAsync(new TestMessage()
        {
            Id = ++i
        }, headers);
        Console.WriteLine($"Publish: {i}");
    }
}


async ValueTask Handle(IReadOnlyList<TestMessage> messageList)
{
    Console.WriteLine($"Receive data count : {messageList.Count}");

    foreach (var message in messageList)
    {
        Console.WriteLine($"receive: {message.Id}");
        Thread.Sleep(10000);
        Console.WriteLine($"handled: {message.Id}");
    }

}

async ValueTask Handle2(TestMessage message)
{
    Console.WriteLine($"2_receive: {message.Id}");
    await Task.Delay(10000);
    Console.WriteLine($"@_handled: {message.Id}");
}


Console.WriteLine("Press Enter to exit");
Console.ReadLine();

[DataContract]
public class TestMessage
{
    public const string TopicName = "test-message";
    [DataMember(Order = 1)] public int Id;
}