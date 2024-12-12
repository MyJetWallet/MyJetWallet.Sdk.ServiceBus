using System;
using System.Collections.Generic;
using Autofac;
using DotNetCoreDecorators;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service;
using MyJetWallet.Sdk.Service.LivnesProbs;
using MyServiceBus.Abstractions;
using MyServiceBus.TcpClient;

namespace MyJetWallet.Sdk.ServiceBus;

public static class MyServiceBusTcpClientFactory
{
    public static MyServiceBusTcpClient Create(Func<string> getHostPort, ILogger logger)
    {
        var name = ApplicationEnvironment.HostName ??
                   $"{ApplicationEnvironment.AppName}:{ApplicationEnvironment.AppVersion}";

        var serviceBusClient = new MyServiceBusTcpClient(getHostPort, name);
        serviceBusClient.Log.AddLogException(ex => logger.LogInformation(ex, "Exception in MyServiceBusTcpClient"));
        serviceBusClient.Log.AddLogInfo(info => logger.LogDebug($"MyServiceBusTcpClient[info]: {info}"));
        serviceBusClient.SocketLogs.AddLogInfo((context, msg) => logger.LogInformation($"MyServiceBusTcpClient[Socket {context?.Id}|{context?.ContextName}|{context?.Inited}][Info] {msg}"));
        serviceBusClient.SocketLogs.AddLogException((context, exception) => logger.LogInformation(exception, $"MyServiceBusTcpClient[Socket {context?.Id}|{context?.ContextName}|{context?.Inited}][Exception] {exception.Message}"));

        return serviceBusClient;
    }

    public static MyServiceBusTcpClient RegisterMyServiceBusTcpClient(
        this ContainerBuilder builder,
        Func<string> getHostPort,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger<MyServiceBusTcpClient>();
        var client = Create(getHostPort, logger);

        var manager = new ServiceBusManager(client, getHostPort?.Invoke());
        builder.RegisterInstance(manager).As<IServiceBusManager>().SingleInstance();

        builder.RegisterType<ServiceBusLifeTime>().AsSelf().As<ILivenessReporter>().SingleInstance().AutoActivate();

        return client;
    }

    public static ContainerBuilder RegisterMyServiceBusPublisher<T>(
        this ContainerBuilder builder,
        MyServiceBusTcpClient client,
        string topicName,
        bool immediatelyPersist)
    {
        client.CreateTopicIfNotExists(topicName);

        var pub = new MyServiceBusPublisher<T>(client, topicName, immediatelyPersist);

        // publisher
        builder
            .RegisterInstance(pub)
            .As<IServiceBusPublisher<T>>()
            .SingleInstance();

        return builder;
    }

    public static ContainerBuilder RegisterMyServiceBusPublisher<T>(
        this ContainerBuilder builder,
        MyServiceBusTcpClient client,
        string topicName,
        bool immediatelyPersist,
        Action<T, Dictionary<string, string>> headersHandler)
    {
        client.CreateTopicIfNotExists(topicName);

        var pub = new MyServiceBusPublisher<T>(client, topicName, immediatelyPersist);
        if (headersHandler is not null)
            pub.SetHeadersSetter(headersHandler);

        // publisher
        builder
            .RegisterInstance(pub)
            .As<IServiceBusPublisher<T>>()
            .SingleInstance();

        return builder;
    }

    public static ContainerBuilder RegisterMyServiceBusSubscriberSingle<T>(
        this ContainerBuilder builder,
        MyServiceBusTcpClient client, string topicName, string queueName, TopicQueueType queryType)
    {
        var subscriber = new MyServiceBusSubscriber<T>(client, topicName, queueName, queryType, false, 100);

        // single subscriber
        builder
            .RegisterInstance(subscriber)
            .As<ISubscriber<T>>()
            .SingleInstance();

        return builder;
    }

    public static ContainerBuilder RegisterMyServiceBusSubscriberSingle<T>(
        this ContainerBuilder builder,
        MyServiceBusTcpClient client, string topicName, string queueName, TopicQueueType queryType,
        Action<Exception> deserializeExceptionHandler)
    {
        var subscriber = new MyServiceBusSubscriber<T>(client, topicName, queueName, queryType, false, 100);
        subscriber.SetDeserializeExceptionHandler(deserializeExceptionHandler);

        // single subscriber
        builder
            .RegisterInstance(subscriber)
            .As<ISubscriber<T>>()
            .SingleInstance();

        return builder;
    }

    public static ContainerBuilder RegisterMyServiceBusSubscriberSingle<T>(
        this ContainerBuilder builder,
        MyServiceBusTcpClient client, string topicName, string queueName, TopicQueueType queryType,
        IDeduplicator<T> _deduplicator)
    {
        // single subscriber
        builder
            .RegisterInstance(new MyServiceBusSubscriber<T>(client, topicName, queueName, queryType, _deduplicator))
            .As<ISubscriber<T>>()
            .SingleInstance();

        return builder;
    }

    public static ContainerBuilder RegisterMyServiceBusSubscriberSingle<T>(
        this ContainerBuilder builder,
        MyServiceBusTcpClient client, string topicName, string queueName, TopicQueueType queryType,
        IDeduplicator<T> _deduplicator,
        Action<Exception> deserializeExceptionHandler)
    {
        var subscriber = new MyServiceBusSubscriber<T>(client, topicName, queueName, queryType, _deduplicator);
        subscriber.SetDeserializeExceptionHandler(deserializeExceptionHandler);

        // single subscriber
        builder
            .RegisterInstance(subscriber)
            .As<ISubscriber<T>>()
            .SingleInstance();

        return builder;
    }

    public static ContainerBuilder RegisterMyServiceBusSubscriberSingle<T>(
        this ContainerBuilder builder,
        MyServiceBusTcpClient client, string topicName, string queueName, TopicQueueType queryType,
        IDeduplicator<T> _deduplicator = null,
        Action<Exception> deserializeExceptionHandler = null,
        Action<T, Dictionary<string, string>> headersGetter = null)
    {
        MyServiceBusSubscriber<T> subscriber;
        if (_deduplicator is not null)
            subscriber = new MyServiceBusSubscriber<T>(client, topicName, queueName, queryType, _deduplicator);
        else
            subscriber = new MyServiceBusSubscriber<T>(client, topicName, queueName, queryType, false, 100);
        if (deserializeExceptionHandler is not null)
            subscriber.SetDeserializeExceptionHandler(deserializeExceptionHandler);
        if (headersGetter is not null)
            subscriber.SetHeadersGetter(headersGetter);

        // single subscriber
        builder
            .RegisterInstance(subscriber)
            .As<ISubscriber<T>>()
            .SingleInstance();

        return builder;
    }

    public static ContainerBuilder RegisterMyServiceBusSubscriberBatch<T>(
        this ContainerBuilder builder,
        MyServiceBusTcpClient client, string topicName, string queueName, TopicQueueType queryType,
        Action<Exception> deserializeExceptionHandler)
    {
        var subscriber = new MyServiceBusSubscriber<T>(client, topicName, queueName, queryType, true, false);
        subscriber.SetDeserializeExceptionHandler(deserializeExceptionHandler);

        // batch subscriber
        builder
            .RegisterInstance(subscriber)
            .As<ISubscriber<IReadOnlyList<T>>>()
            .SingleInstance();

        return builder;
    }

    public static ContainerBuilder RegisterMyServiceBusSubscriberBatch<T>(
        this ContainerBuilder builder,
        MyServiceBusTcpClient client, string topicName, string queueName, TopicQueueType queryType)
    {
        // batch subscriber
        builder
            .RegisterInstance(new MyServiceBusSubscriber<T>(client, topicName, queueName, queryType, true, false))
            .As<ISubscriber<IReadOnlyList<T>>>()
            .SingleInstance();

        return builder;
    }

    public static ContainerBuilder RegisterMyServiceBusSubscriberBatch<T>(
        this ContainerBuilder builder,
        MyServiceBusTcpClient client, string topicName, string queueName, TopicQueueType queryType, int chunkSize)
    {
        // batch subscriber
        builder
            .RegisterInstance(new MyServiceBusSubscriber<T>(client, topicName, queueName, queryType, true, chunkSize))
            .As<ISubscriber<IReadOnlyList<T>>>()
            .SingleInstance();

        return builder;
    }

    public static ContainerBuilder RegisterMyServiceBusSubscriberBatch<T>(
        this ContainerBuilder builder,
        MyServiceBusTcpClient client, string topicName, string queueName, TopicQueueType queryType, int chunkSize,
        Action<Exception> deserializeExceptionHandler)
    {
        var subscriber = new MyServiceBusSubscriber<T>(client, topicName, queueName, queryType, true, chunkSize);
        subscriber.SetDeserializeExceptionHandler(deserializeExceptionHandler);

        // batch subscriber
        builder
            .RegisterInstance(subscriber)
            .As<ISubscriber<IReadOnlyList<T>>>()
            .SingleInstance();

        return builder;
    }

    public static ContainerBuilder RegisterMyServiceBusSubscriberBatch<T>(
        this ContainerBuilder builder,
        MyServiceBusTcpClient client, string topicName, string queueName, TopicQueueType queryType,
        int chunkSize = 100,
        Action<Exception> deserializeExceptionHandler = null,
        Action<T, Dictionary<string, string>> headersGetter = null)
    {
        var subscriber = new MyServiceBusSubscriber<T>(client, topicName, queueName, queryType, true, chunkSize);
        if (deserializeExceptionHandler is not null)
            subscriber.SetDeserializeExceptionHandler(deserializeExceptionHandler);
        if (headersGetter is not null)
            subscriber.SetHeadersGetter(headersGetter);

        // batch subscriber
        builder
            .RegisterInstance(subscriber)
            .As<ISubscriber<IReadOnlyList<T>>>()
            .SingleInstance();

        return builder;
    }

    public static MyServiceBusDeduplicator<T> RegisterMyServiceBusDeduplicator<T>(
        this ContainerBuilder builder,
        Func<T, string> tToStrFunc,
        Func<string> noSqlWriterUrl,
        string tableName,
        string topicName,
        TimeSpan expirationTime,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger<MyServiceBusDeduplicator<T>>();

        var deduplicator = new MyServiceBusDeduplicator<T>(tToStrFunc, noSqlWriterUrl, tableName, topicName,
            expirationTime, logger);

        builder
            .RegisterInstance(deduplicator)
            .As<IDeduplicator<T>>()
            .SingleInstance();

        return deduplicator;
    }


    private static ILogger _deserializeExceptionHandlerLogger = null;
    public static Action<Exception> GetDeserializeExceptionHandlerLogger(ILoggerFactory loggerFactory, string topicName)
    {
        if (_deserializeExceptionHandlerLogger == null)
            _deserializeExceptionHandlerLogger = loggerFactory.CreateLogger("ServiceBusDeserializeLogger");

        return ex =>
        {
            _deserializeExceptionHandlerLogger.LogError(ex, "Cannot Deserialize message from TOPIC {topicName}. Message was SKIPPED", topicName);
        };
    }
}
