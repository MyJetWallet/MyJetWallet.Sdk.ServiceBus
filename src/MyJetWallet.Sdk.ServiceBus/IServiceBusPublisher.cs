using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyJetWallet.Sdk.ServiceBus;

public interface IServiceBusPublisher<in T>
{
    Task PublishAsync(T message);
    
    Task PublishAsync(IEnumerable<T> messageList);
    
    Task PublishAsync(T message, Dictionary<string, string> headers);
    
    Task PublishAsync(IEnumerable<T> messageList, Dictionary<string, string> headers);
}