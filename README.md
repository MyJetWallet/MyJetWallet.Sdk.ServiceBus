# using

**Module:**

```csharp
public class ServiceBusModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        var serviceBusClient = builder.RegisterMyServiceBusTcpClient(Program.ReloadedSettings(e => e.SpotServiceBusHostPort), ApplicationEnvironment.HostName, Program.LogFactory);

        var queryName = "Liquidity-Reports";

        // publisher
        builder
            .RegisterInstance(new MyServiceBusPublisher<PortfolioTrade>(serviceBusClient, PortfolioTrade.TopicName, true))
            .As<IPublisher<PortfolioTrade>>()
            .SingleInstance();


        // batch subscriber
        builder
            .RegisterInstance(new MyServiceBusSubscriber<PortfolioTrade>(serviceBusClient, PortfolioTrade.TopicName, queryName, TopicQueueType.Permanent, true))
            .As<ISubscriber<IReadOnlyList<PortfolioTrade>>>()
            .SingleInstance();

        // single subscriber
        builder
            .RegisterInstance(new MyServiceBusSubscriber<PortfolioPosition>(serviceBusClient, PortfolioPosition.TopicName, queryName, TopicQueueType.Permanent, false))
            .As<ISubscriber<PortfolioTrade>>()
            .SingleInstance();
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
