# using

**Module:**

```csharp
public class ServiceBusModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        var serviceBusClient = builder.RegisterMyServiceBusTcpClient(Program.ReloadedSettings(e => e.SpotServiceBusHostPort), ApplicationEnvironment.HostName, Program.LogFactory);

        var queryName = "Liquidity-Reports";


        // publisher (IServiceBusPublisher<PortfolioTrade>)
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
    private readonly ServiceBusLifeTime _myServiceBusLifeTime;

    public ApplicationLifetimeManager(IHostApplicationLifetime appLifetime, ServiceBusLifeTime myServiceBusLifeTime)
        : base(appLifetime)
    {
        _myServiceBusLifeTime = myServiceBusLifeTime;
    }

    protected override void OnStarted()
    {
        _myServiceBusLifeTime.Start();
    }

    protected override void OnStopping()
    {
        _myServiceBusLifeTime.Stop();
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

**Deduplication:**

```csharp
public class ServiceBusModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        var serviceBusClient = builder.RegisterMyServiceBusTcpClient(Program.ReloadedSettings(e => e.SpotServiceBusHostPort), ApplicationEnvironment.HostName, Program.LogFactory);

        var queryName = "Liquidity-Reports";

        var deduplicator = builder.RegisterMyServiceBusDeduplicator<PortfolioTrade>(
                    t => t.TraceId,                                             //Func to get unique id
                    Program.ReloadedSettings(t=>t.MyNoSqlWriterUrl),            
                    queryName,                                                  //NoSql table name
                    PortfolioTrade.TopicName,                                   //NoSql partition key
                    TimeSpan.FromHours(4),                                      //Expiration time
                    Program.LogFactory);


        // single subscriber (ISubscriber<PortfolioTrade>)
        // dedupication only available for single subscriber
        builder.RegisterMyServiceBusSubscriberSingle<PortfolioTrade>(serviceBusClient, PortfolioPosition.TopicName, queryName, TopicQueueType.Permanent, deduplicator);
    }
}
```
