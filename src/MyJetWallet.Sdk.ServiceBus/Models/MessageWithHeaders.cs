using System.Collections.Generic;

namespace MyJetWallet.Sdk.ServiceBus.Models;

public class MessageWithHeaders<T>
{
    public T Data { get; set; }
    public Dictionary<string, string> Headers { get; set; } = [];
}
