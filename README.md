# using

**Module:**

```csharp
public class ServiceBusModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        var serviceBusClient = builder.RegisterMyServiceBusTcpClient(Program.ReloadedSettings(e => e.SpotServiceBusHostPort), ApplicationEnvironment.HostName, Program.LogFactory);

        var queryName = "Liquidity-Reports";


        // publisher (IPublisher<PortfolioTrade>)
        builder.RegisterMyServiceBusPublisher<PortfolioTrade>(serviceBusClient, PortfolioTrade.TopicName, false);


        // batch subscriber (ISubscriber<IReadOnlyList<PortfolioTrade>>)
        builder.RegisterMyServiceBusSubscriberBatch<PortfolioTrade>(serviceBusClient, PortfolioTrade.TopicName, queryName, TopicQueueType.Permanent);


        // single subscriber (ISubscriber<PortfolioTrade>)
        builder.RegisterMyServiceBusSubscriberSingle<PortfolioTrade>(serviceBusClient, PortfolioPosition.TopicName, queryName, TopicQueueType.Permanent);
    }
}
```

**LifeTime:**

```csharp
public class ApplicationLifetimeManager : ApplicationLifetimeManagerBase
{
    private readonly MyServiceBusTcpClient _myServiceBusTcpClient;

    public ApplicationLifetimeManager(IHostApplicationLifetime appLifetime, MyServiceBusTcpClient myServiceBusTcpClient)
        : base(appLifetime)
    {
        _myServiceBusTcpClient = myServiceBusTcpClient;
    }

    protected override void OnStarted()
    {
        _myServiceBusTcpClient.Start();
    }

    protected override void OnStopping()
    {
        _myServiceBusTcpClient.Stop();
    }
}
```

**Model:**

```csharp
[DataContract]
public class PortfolioTrade
{
    public const string TopicName = "spot-liquidity-engine-trade";

    [DataMember(Order = 1)] public string TradeId { get; set; }
    [DataMember(Order = 2)] public string Source { get; set; }
    [DataMember(Order = 3)] public bool IsInternal { get; set; }
}
```
