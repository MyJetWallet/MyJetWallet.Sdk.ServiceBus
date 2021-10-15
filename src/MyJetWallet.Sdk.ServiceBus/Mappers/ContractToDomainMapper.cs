using System;
using System.IO;
using ProtoBuf;

namespace MyJetWallet.Sdk.ServiceBus
{
    public static class ContractToDomainMapper
    {
        public static T ByteArrayToServiceBusContract<T>(this ReadOnlyMemory<byte> data)
        {
            if (ServiceBusContracts.IsDebug)
                Console.WriteLine($"GOT {typeof(T)} MESSAGE LEN:" + data.Length);

            var span = data.Span;

            if (span[0] == 0)
            {
                try
                {
                    span = data.Slice(1, data.Length - 1).Span;
                    var mem = new MemoryStream(data.Length);
                    mem.Write(span);
                    mem.Position = 0;
                    return ProtoBuf.Serializer.Deserialize<T>(mem);
                }
                catch (Exception ex)
                {
                    var dataBase64 = Convert.ToBase64String(data.ToArray());
                    Console.WriteLine($"Cannot deserialize message {typeof(T).Name}. Data: '{dataBase64}'");

                    throw new Exception($"Cannot deserialize message {typeof(T).Name}: {ex.Message}", ex);
                }
            }

            throw new Exception("Not supported version of Contract");
        }
    }
}