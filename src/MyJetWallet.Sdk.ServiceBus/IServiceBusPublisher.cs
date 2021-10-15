using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyJetWallet.Sdk.ServiceBus
{
    public interface IServiceBusPublisher<in T>
    {
        Task PublishAsync(T message);
        
        Task PublishAsync(IEnumerable<T> messageList);
    }
}