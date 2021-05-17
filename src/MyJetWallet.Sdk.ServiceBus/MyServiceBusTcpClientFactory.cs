using System;
using Autofac;
using Microsoft.Extensions.Logging;
using MyServiceBus.TcpClient;
using SimpleTrading.ServiceBus.CommonUtils;
using SimpleTrading.ServiceBus.CommonUtils.Serializers;

namespace MyJetWallet.Sdk.ServiceBus
{
    public static class MyServiceBusTcpClientFactory
    {
        public static ILogger Logger { get; set; }

        public static MyServiceBusTcpClient Create(Func<string> getHostPort, string name, ILogger logger)
        {
            var serviceBusClient = new MyServiceBusTcpClient(getHostPort, name);
            serviceBusClient.Log.AddLogException(ex => logger.LogInformation(ex, "Exception in MyServiceBusTcpClient"));
            serviceBusClient.Log.AddLogInfo(info => logger.LogDebug($"MyServiceBusTcpClient[info]: {info}"));
            serviceBusClient.SocketLogs.AddLogInfo((context, msg) => logger.LogInformation($"MyServiceBusTcpClient[Socket {context?.Id}|{context?.ContextName}|{context?.Inited}][Info] {msg}"));
            serviceBusClient.SocketLogs.AddLogException((context, exception) => logger.LogInformation(exception, $"MyServiceBusTcpClient[Socket {context?.Id}|{context?.ContextName}|{context?.Inited}][Exception] {exception.Message}"));


            return serviceBusClient;
        }

        public static MyServiceBusTcpClient RegisterMyServiceBusTcpClient(this ContainerBuilder builder, Func<string> getHostPort, string name, ILoggerFactory loggerFactory)
        {
            if (Client != null)
            {
                throw new Exception("Client already created");
            }

            Logger = loggerFactory.CreateLogger<MyServiceBusTcpClient>();
            var client = MyServiceBusTcpClientFactory.Create(getHostPort, name, Logger);
            builder.RegisterInstance(client).AsSelf().AutoActivate().SingleInstance();

            Client = client;

            return client;
        }

        public static MyServiceBusTcpClient Client { get; private set; }

    }
}
