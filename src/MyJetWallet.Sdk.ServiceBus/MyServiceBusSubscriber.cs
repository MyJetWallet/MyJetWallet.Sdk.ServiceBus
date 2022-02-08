using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetCoreDecorators;
using MyServiceBus.Abstractions;
using MyServiceBus.TcpClient;

namespace MyJetWallet.Sdk.ServiceBus
{
    public class MyServiceBusSubscriber<T> : ISubscriber<T>, ISubscriber<IReadOnlyList<T>>
    {
        private readonly int _chunkSize;
        private readonly bool _batchSubscribe;
        private readonly bool _withDeduplication = false;
        private readonly List<Func<T, ValueTask>> _list = new();
        private readonly List<Func<IReadOnlyList<T>, ValueTask>> _listBatch = new();
        private readonly IDeduplicator<T> _deduplicator;

        public MyServiceBusSubscriber(MyServiceBusTcpClient client, string topicName, string queueName, TopicQueueType queryType, bool batchSubscribe, bool withDeduplication = false)
        {
            _batchSubscribe = batchSubscribe;
            if (!batchSubscribe)
            {
                if (withDeduplication)
                {
                    client.Subscribe(topicName, queueName, queryType, HandlerSingleWithDeduplication);
                }
                else
                {
                    client.Subscribe(topicName, queueName, queryType, HandlerSingle);
                }
            }
            else
            {
                client.Subscribe(topicName, queueName, queryType, HandlerBatch);
            }

            _chunkSize = 100;
        }

        public MyServiceBusSubscriber(MyServiceBusTcpClient client, string topicName, string queueName,
            TopicQueueType queryType, bool batchSubscribe, int chunkSize) : this(client,topicName, queueName, queryType, batchSubscribe, false)
        {
            _chunkSize = chunkSize;
        }
        
        public MyServiceBusSubscriber(MyServiceBusTcpClient client, string topicName, string queueName,
            TopicQueueType queryType, IDeduplicator<T> deduplicator) : this(client,topicName, queueName, queryType, false, true)
        {
            _withDeduplication = true;
            _deduplicator = deduplicator;
        }

        private async ValueTask HandlerSingle(IMyServiceBusMessage data)
        {
            var itm = data.Data.ByteArrayToServiceBusContract<T>();
            foreach (var subscribers in _list)
                await subscribers(itm);
        }
        
        private async ValueTask HandlerSingleWithDeduplication(IMyServiceBusMessage data)
        {
            var itm = data.Data.ByteArrayToServiceBusContract<T>();
            
            if(await _deduplicator.IsDuplicate(itm))
                return;
            
            foreach (var subscribers in _list)
                await subscribers(itm);

            await _deduplicator.AddToRegistry(itm);
        }

        private async ValueTask HandlerBatch(IConfirmationContext ctx, IReadOnlyList<IMyServiceBusMessage> data)
        {
            if (data.Count <= _chunkSize)
            {
                var items = data.Select(e => e.Data.ByteArrayToServiceBusContract<T>()).ToList();

                foreach (var callback in _listBatch)
                {
                    await callback(items);
                }
            }
            else
            {
                var index = 0;
                var chunk = data.Skip(index).Take(_chunkSize);
                while (chunk.Any())
                {
                    var items = chunk.Select(e => e.Data.ByteArrayToServiceBusContract<T>()).ToList();
                    
                    foreach (var callback in _listBatch)
                    {
                        await callback(items);
                    }
                    
                    ctx.ConfirmMessages(chunk.Select(e => e.Id));

                    index += _chunkSize;
                    chunk = data.Skip(index).Take(_chunkSize).ToList();
                }
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
}