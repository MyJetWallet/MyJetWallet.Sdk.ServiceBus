using System.Threading.Tasks;

namespace MyJetWallet.Sdk.ServiceBus;

public interface IDeduplicator<in T>
{
    Task<bool> IsDuplicate(T item);
    Task AddToRegistry(T item);
}