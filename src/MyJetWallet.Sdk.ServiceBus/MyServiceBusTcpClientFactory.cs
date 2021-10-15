using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using DotNetCoreDecorators;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service;
using MyServiceBus.Abstractions;
using MyServiceBus.TcpClient;

namespace MyJetWallet.Sdk.ServiceBus
{
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

        public static MyServiceBusTcpClient RegisterMyServiceBusTcpClient(this ContainerBuilder builder, Func<string> getHostPort, ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger<MyServiceBusTcpClient>();
            var client = Create(getHostPort, logger);

            var manager = new ServiceBusManager(client);
            builder.RegisterInstance(manager).As<IServiceBusManager>().SingleInstance();

            builder.RegisterType<ServiceBusLifeTime>().AsSelf().SingleInstance().AutoActivate();

            return client;
        }

        public static ContainerBuilder RegisterMyServiceBusPublisher<T>(this ContainerBuilder builder, MyServiceBusTcpClient client, string topicName, bool immediatelyPersist)
        {
            client.CreateTopicIfNotExists(topicName);
            
            // publisher
            builder
                .RegisterInstance(new MyServiceBusPublisher<T>(client, topicName, immediatelyPersist))
                .As<IServiceBusPublisher<T>>()
                .SingleInstance();

            return builder;
        }

        public static ContainerBuilder RegisterMyServiceBusSubscriberSingle<T>(this ContainerBuilder builder, MyServiceBusTcpClient client, string topicName, string queueName, TopicQueueType queryType)
        {
            client.CreateTopicIfNotExists(topicName);
            
            // single subscriber
            builder
                .RegisterInstance(new MyServiceBusSubscriber<T>(client, topicName, queueName, queryType, false))
                .As<ISubscriber<T>>()
                .SingleInstance();

            return builder;
        }

        public static ContainerBuilder RegisterMyServiceBusSubscriberBatch<T>(this ContainerBuilder builder, MyServiceBusTcpClient client, string topicName, string queueName, TopicQueueType queryType)
        {
            client.CreateTopicIfNotExists(topicName);
            
            // batch subscriber
            builder
                .RegisterInstance(new MyServiceBusSubscriber<T>(client, topicName, queueName, queryType, true))
                .As<ISubscriber<IReadOnlyList<T>>>()
                .SingleInstance();

            return builder;
        }

    }
}
