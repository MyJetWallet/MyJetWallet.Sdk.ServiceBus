using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyServiceBus.TcpClient;

namespace MyJetWallet.Sdk.ServiceBus
{
    public class MyServiceBusPublisher<T> : IServiceBusPublisher<T>
    {
        private readonly MyServiceBusTcpClient _client;
        private readonly string _topicName;
        private readonly bool _immediatelyPersist;

        public MyServiceBusPublisher(MyServiceBusTcpClient client, string topicName, bool immediatelyPersist)
        {
            this._client = client;
            _topicName = topicName;
            _immediatelyPersist = immediatelyPersist;
            this._client.CreateTopicIfNotExists(topicName);
        }

        public async ValueTask PublishAsync11(T valueToPublish)
        {
            await this._client.PublishAsync(_topicName, valueToPublish.ServiceBusContractToByteArray(), _immediatelyPersist);
        }

        public Task PublishAsync(T message)
        {
            return _client.PublishAsync(_topicName, message.ServiceBusContractToByteArray(), _immediatelyPersist);
        }

        public Task PublishAsync(IEnumerable<T> messageList)
        {
            var batch = messageList.Select(e => e.ServiceBusContractToByteArray()).ToList();
            return _client.PublishAsync(_topicName, batch, _immediatelyPersist);
        }
    }
}