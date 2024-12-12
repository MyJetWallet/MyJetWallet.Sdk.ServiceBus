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
    private Action<T, Dictionary<string, string>> _headerGetter;

    public MyServiceBusPublisher(MyServiceBusTcpClient client, string topicName, bool immediatelyPersist)
    {
        _client = client;
        _topicName = topicName;
        _immediatelyPersist = immediatelyPersist;
        _client.CreateTopicIfNotExists(topicName);
    }

    public void SetHeadersHandler(Action<T, Dictionary<string, string>> headerGetter)
    {
        _headerGetter = headerGetter ?? throw new Exception("headerGetter cannot be null");
    }
    
    public Task PublishAsync(T message)
    {
        if(_headerGetter is not null)
        {
            var headers = new Dictionary<string, string>();
            _headerGetter.Invoke(message, headers);
            return _client.PublishAsync(_topicName, message.ServiceBusContractToByteArray(headers), _immediatelyPersist);
        }
        
        return _client.PublishAsync(_topicName, message.ServiceBusContractToByteArray(), _immediatelyPersist);
    }
    
    public Task PublishAsync(IEnumerable<T> messageList)
    {
        if (messageList == null)
            return Task.CompletedTask;
        
        List<byte[]> batch = new List<byte[]>();

        foreach (var message in messageList)
        {
            byte[] payload;
            if (_headerGetter != null)
            {
                var headerList = new Dictionary<string, string>();
                _headerGetter?.Invoke(message, headerList);
                payload = message.ServiceBusContractToByteArray(headerList);
            }
            else
            {
                payload = message.ServiceBusContractToByteArray();
            }
            
            batch.Add(payload);
        }
        
        return _client.PublishAsync(_topicName, batch, _immediatelyPersist);
    }

    public Task PublishAsync(T message, Dictionary<string, string> headers)
    {
        if (message == null)
            return Task.CompletedTask;
        
        headers ??= new Dictionary<string, string>();
        _headerGetter?.Invoke(message, headers);
        var payload = message.ServiceBusContractToByteArray(headers);
        
        return _client.PublishAsync(_topicName, payload, _immediatelyPersist);
    }

    public Task PublishAsync(IEnumerable<T> messageList, Dictionary<string, string> headers)
    {
        if (messageList == null)
            return Task.CompletedTask;

        headers ??= new Dictionary<string, string>();
        
        List<byte[]> batch = new List<byte[]>();

        foreach (var message in messageList)
        {
            var headerList = new Dictionary<string, string>(headers);
            _headerGetter?.Invoke(message, headerList);
            var payload = message.ServiceBusContractToByteArray(headerList);
            batch.Add(payload);
        }
        
        return _client.PublishAsync(_topicName, batch, _immediatelyPersist);
    }
}