using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyServiceBus.TcpClient;

namespace MyJetWallet.Sdk.ServiceBus;

public class MyServiceBusPublisher<T> : IServiceBusPublisher<T>
{
    private readonly MyServiceBusTcpClient _client;
    private readonly string _topicName;
    private readonly bool _immediatelyPersist;
    private Action<Dictionary<string,string>> _headersHandler;

    public MyServiceBusPublisher(MyServiceBusTcpClient client, string topicName, bool immediatelyPersist)
    {
        _client = client;
        _topicName = topicName;
        _immediatelyPersist = immediatelyPersist;
        _client.CreateTopicIfNotExists(topicName);
    }

    public void SetHeadersHandler(Action<Dictionary<string, string>> handler)
    {
        _headersHandler = handler;
    }

    public Task PublishAsync(T message, Dictionary<string, string> headers = null)
    {
        if(_headersHandler is not null)
        {
            headers ??= [];
            _headersHandler.Invoke(headers);
        }
        return _client.PublishAsync(_topicName, message.ServiceBusContractToByteArray(headers), _immediatelyPersist);
    }

    public Task PublishAsync(IEnumerable<T> messageList, Dictionary<string, string> headers = null)
    {
        if (_headersHandler is not null)
        {
            headers ??= [];
            _headersHandler.Invoke(headers);
        }
        var batch = messageList.Select(e => e.ServiceBusContractToByteArray(headers)).ToList();
        return _client.PublishAsync(_topicName, batch, _immediatelyPersist);
    }
}