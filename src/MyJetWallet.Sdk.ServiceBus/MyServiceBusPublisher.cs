using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyServiceBus.TcpClient;

namespace MyJetWallet.Sdk.ServiceBus;

public class MyServiceBusPublisher<T> : IServiceBusPublisher<T>
{
    private readonly MyServiceBusTcpClient _client;
    private readonly string _topicName;
    private readonly bool _immediatelyPersist;
    private Action<T, Dictionary<string, string>> _headersSetter;

    public MyServiceBusPublisher(MyServiceBusTcpClient client, string topicName, bool immediatelyPersist)
    {
        _client = client;
        _topicName = topicName;
        _immediatelyPersist = immediatelyPersist;
        _client.CreateTopicIfNotExists(topicName);
    }

    public void SetHeadersSetter(Action<T, Dictionary<string, string>> headersSetter)
    {
        _headersSetter = headersSetter ?? throw new Exception("headersSetter cannot be null");
    }
    
    public Task PublishAsync(T message)
    {
        if(_headersSetter is not null)
        {
            Dictionary<string, string> headers = [];
            _headersSetter.Invoke(message, headers);
            return _client.PublishAsync(_topicName, message.ServiceBusContractToByteArray(headers), _immediatelyPersist);
        }
        
        return _client.PublishAsync(_topicName, message.ServiceBusContractToByteArray(), _immediatelyPersist);
    }
    
    public Task PublishAsync(IEnumerable<T> messageList)
    {
        if (messageList == null)
            return Task.CompletedTask;
        
        List<byte[]> batch = [];

        foreach (var message in messageList)
        {
            byte[] payload;
            if (_headersSetter != null)
            {
                Dictionary<string, string> headerList = [];
                _headersSetter?.Invoke(message, headerList);
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
        
        headers ??= [];
        _headersSetter?.Invoke(message, headers);
        var payload = message.ServiceBusContractToByteArray(headers);
        
        return _client.PublishAsync(_topicName, payload, _immediatelyPersist);
    }

    public Task PublishAsync(IEnumerable<T> messageList, Dictionary<string, string> headers)
    {
        if (messageList == null)
            return Task.CompletedTask;

        headers ??= [];
        
        List<byte[]> batch = [];

        foreach (var message in messageList)
        {
            var headerList = new Dictionary<string, string>(headers);
            _headersSetter?.Invoke(message, headerList);
            var payload = message.ServiceBusContractToByteArray(headerList);
            batch.Add(payload);
        }
        
        return _client.PublishAsync(_topicName, batch, _immediatelyPersist);
    }
}