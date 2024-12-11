using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetCoreDecorators;
using MyServiceBus.Abstractions;
using MyServiceBus.TcpClient;

namespace MyJetWallet.Sdk.ServiceBus;

public class MyServiceBusSubscriber<T> : ISubscriber<T>, ISubscriber<IReadOnlyList<T>>
{
    private readonly int _chunkSize;
    private readonly bool _batchSubscribe;
    private Action<Exception> _deserializeExceptionHandler;
    private Action<Dictionary<string, string>> _headersHandler;
    private readonly bool _withDeduplication = false;
    private readonly List<Func<T, ValueTask>> _list = new();
    private readonly List<Func<IReadOnlyList<T>, ValueTask>> _listBatch = new();
    private readonly IDeduplicator<T> _deduplicator;

    public MyServiceBusSubscriber(
        MyServiceBusTcpClient client,
        string topicName, string queueName, TopicQueueType queryType, bool batchSubscribe,
        bool withDeduplication)
    {
        _batchSubscribe = batchSubscribe;
        _withDeduplication = withDeduplication;

        if (!batchSubscribe)
        {
            client.Subscribe(topicName, queueName, queryType, HandlerBatchOneByOne);
        }
        else
        {
            client.Subscribe(topicName, queueName, queryType, HandlerBatch);
        }

        _chunkSize = 100;
    }

    public MyServiceBusSubscriber(MyServiceBusTcpClient client, string topicName, string queueName,
        TopicQueueType queryType, bool batchSubscribe, int chunkSize)
            : this(client, topicName, queueName, queryType, batchSubscribe, false)
    {
        _chunkSize = chunkSize;
    }

    public MyServiceBusSubscriber(MyServiceBusTcpClient client, string topicName, string queueName,
        TopicQueueType queryType, IDeduplicator<T> deduplicator)
        : this(client, topicName, queueName, queryType, false, true)
    {
        _withDeduplication = true;
        _deduplicator = deduplicator;
    }

    public void SetDeserializeExceptionHandler(Action<Exception> deserializeExceptionHandler)
    {
        _deserializeExceptionHandler = deserializeExceptionHandler;
    }

    public void SetHeadersHandler(Action<Dictionary<string, string>> headersHandler)
    {
        _headersHandler = headersHandler;
    }

    private async ValueTask HandlerSingle(IMyServiceBusMessage data)
    {
        T item = default;
        try
        {
            var message = data.Data.ByteArrayToServiceBusContract<T>();
            item = message.Data;
            _headersHandler?.Invoke(message.Headers);
        }
        catch (Exception ex)
        {
            if (_deserializeExceptionHandler != null)
            {
                _deserializeExceptionHandler.Invoke(ex);
                return;
            }
            throw;
        }

        foreach (var subscribers in _list)
            await subscribers(item);
    }

    private async ValueTask HandlerSingleWithDeduplication(IMyServiceBusMessage data)
    {
        T item = default;
        try
        {
            var message = data.Data.ByteArrayToServiceBusContract<T>();
            item = message.Data;
            _headersHandler?.Invoke(message.Headers);
        }
        catch (Exception ex)
        {
            if (_deserializeExceptionHandler != null)
            {
                _deserializeExceptionHandler?.Invoke(ex);
                return;
            }
            throw;
        }

        if (await _deduplicator.IsDuplicate(item))
            return;

        foreach (var subscribers in _list)
            await subscribers(item);

        await _deduplicator.AddToRegistry(item);
    }

    private async ValueTask HandlerBatchOneByOne(IConfirmationContext ctx, IReadOnlyList<IMyServiceBusMessage> data)
    {
        if (!data.Any())
            return;

        await Task.Yield();

        foreach (var message in data.OrderBy(e => e.Id))
        {
            if (_withDeduplication)
                await HandlerSingleWithDeduplication(message);
            else
                await HandlerSingle(message);

            ctx.ConfirmMessages([message.Id]);
        }
    }

    private async ValueTask HandlerBatch(IConfirmationContext ctx, IReadOnlyList<IMyServiceBusMessage> data)
    {
        if (!data.Any())
            return;

        await Task.Yield();

        if (data.Count <= _chunkSize)
        {
            await HandleBatchMessages(data);
        }
        else
        {
            var index = 0;
            var chunk = data.OrderBy(e => e.Id).Skip(index).Take(_chunkSize).ToList();
            while (chunk.Any())
            {
                await HandleBatchMessages(chunk);

                ctx.ConfirmMessages(chunk.Select(e => e.Id));

                index += _chunkSize;
                chunk = data.OrderBy(e => e.Id).Skip(index).Take(_chunkSize).ToList();
            }
        }
    }

    private async Task HandleBatchMessages(IReadOnlyList<IMyServiceBusMessage> data)
    {
        if (!data.Any())
            return;

        var items = new List<T>(data.Count);

        foreach (var mes in data)
        {
            try
            {
                var message = mes.Data.ByteArrayToServiceBusContract<T>();
                var item = message.Data;
                _headersHandler?.Invoke(message.Headers);
                items.Add(item);
            }
            catch (Exception ex)
            {
                _deserializeExceptionHandler?.Invoke(ex);
            }
        }

        if (items.Count == 0)
            return;

        foreach (var callback in _listBatch)
        {
            await callback(items);
        }
    }

    public void Subscribe(Func<T, ValueTask> callback)
    {
        if (_batchSubscribe)
                throw new Exception("Cannot subscribe to single message, please use batch subscriber");

        _list.Add(callback);
    }

    public void Subscribe(Func<IReadOnlyList<T>, ValueTask> callback)
    {
        if (!_batchSubscribe)
                throw new Exception("Cannot subscribe to batch of message, please use single message subscriber");

        _listBatch.Add(callback);
    }
}