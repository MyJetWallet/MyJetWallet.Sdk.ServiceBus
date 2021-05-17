﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetCoreDecorators;
using MyServiceBus.Abstractions;
using MyServiceBus.TcpClient;
using SimpleTrading.ServiceBus.CommonUtils.Serializers;

namespace MyJetWallet.Sdk.ServiceBus
{
    public class MyServiceBusSubscriber<T> : ISubscriber<T>, ISubscriber<IReadOnlyList<T>>
    {
        private readonly bool _batchSubscribe;

        private readonly List<Func<T, ValueTask>> _list = new();
        private readonly List<Func<IReadOnlyList<T>, ValueTask>> _listBatch = new();

        public MyServiceBusSubscriber(MyServiceBusTcpClient client, string topicName, string queueName, TopicQueueType queryType, bool batchSubscribe)
        {
            _batchSubscribe = batchSubscribe;
            if (!batchSubscribe)
            {
                client.Subscribe(topicName, queueName, queryType, HandlerSingle);
            }
            else
            {
                client.Subscribe(topicName, queueName, queryType, HandlerBatch);
            }
        }

        private async ValueTask HandlerSingle(IMyServiceBusMessage data)
        {
            var itm = data.Data.ByteArrayToServiceBusContract<T>();
            foreach (var subscribers in _list)
                await subscribers(itm);
        }

        private async ValueTask HandlerBatch(IConfirmationContext ctx, IReadOnlyList<IMyServiceBusMessage> data)
        {
            var items = data.Select(e => e.Data.ByteArrayToServiceBusContract<T>()).ToList();

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
}