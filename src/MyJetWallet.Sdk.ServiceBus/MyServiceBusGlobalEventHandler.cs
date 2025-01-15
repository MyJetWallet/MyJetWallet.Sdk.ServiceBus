using System;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service;
using MyServiceBus.Abstractions;
using Serilog.Data;

namespace MyJetWallet.Sdk.ServiceBus;

public static class MyServiceBusGlobalEventHandler
{
    private static Func<IMyServiceBusMessage, Exception, Type,
        string, string, bool> _deserializeExceptionHandler;

    public static void SetDeserializeExceptionHandler(Func<IMyServiceBusMessage, Exception, Type,
        string, string, bool> handler)
    {
        _deserializeExceptionHandler = handler;
    }
    
    public static bool HandleDeserializeException(IMyServiceBusMessage data, Exception exception, Type type,
        string topicName, string queryName)
    {
        var result = _deserializeExceptionHandler?.Invoke(data, exception, type, topicName, queryName);

        return result == true;
    }

    public static void SetLogAndSkipDeserializeExceptionHandler(ILoggerFactory factory)
    {
        var logger = factory.CreateLogger("MyServiceBusDeserializeExceptionHandler");
        
        SetDeserializeExceptionHandler((IMyServiceBusMessage data, Exception exception, Type type,
            string topicName, string queryName) =>
        {
            var message = Convert.ToBase64String(data.Data.ToArray());
            logger.LogError(exception, "[ServiceBus] !!!!! DeserializeException received. Message will be skipped. Data: {json}", new
            {
                data = message, messageId = data.Id, type, topicName, queryName, ExceptionMessage = exception.Message
            }.ToJson());
            return true;
        });
    }
}

