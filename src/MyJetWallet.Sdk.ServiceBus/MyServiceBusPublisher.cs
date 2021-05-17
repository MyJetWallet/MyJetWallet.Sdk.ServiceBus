using System.Threading.Tasks;
using DotNetCoreDecorators;
using MyServiceBus.TcpClient;
using SimpleTrading.ServiceBus.CommonUtils.Serializers;

namespace MyJetWallet.Sdk.ServiceBus
{
    public class MyServiceBusPublisher<T> : IPublisher<T>
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

        public async ValueTask PublishAsync(T valueToPublish)
        {
            await this._client.PublishAsync(_topicName, valueToPublish.ServiceBusContractToByteArray(), _immediatelyPersist);
        }
    }
}