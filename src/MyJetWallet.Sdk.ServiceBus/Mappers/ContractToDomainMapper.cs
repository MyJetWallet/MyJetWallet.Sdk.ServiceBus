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
            
            if (data.Span[0] != (byte) 0)
                throw new Exception("Not supported version of Contract");

            try
            {
                ReadOnlySpan<byte> span = data.Slice(1, data.Length - 1).Span;
                MemoryStream memoryStream = new MemoryStream(data.Length);
                memoryStream.Write(span);
                memoryStream.Position = 0L;

                return Serializer.Deserialize<T>((Stream) memoryStream);
            }
            catch (Exception ex)
            {
                var dataBase64 = Convert.ToBase64String(data.ToArray());
                Console.WriteLine($"Cannot deserialize message {typeof(T).Name}. Data: '{dataBase64}'");
                
                throw new Exception($"Cannot deserialize message {typeof(T).Name}: {ex.Message}", ex);
            }
        }
    }
}