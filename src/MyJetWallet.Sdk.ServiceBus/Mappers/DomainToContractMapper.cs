using System;
using System.IO;
using ProtoBuf;

namespace MyJetWallet.Sdk.ServiceBus
{
    public static class DomainToContractMapper
    {
        public static byte[] ServiceBusContractToByteArray(this object src)
        {
            try
            {
                var stream = new MemoryStream();

                stream.WriteByte(0); // First byte is a version contract;

                ProtoBuf.Serializer.Serialize(stream, src);

                var result = stream.ToArray();

                if (ServiceBusContracts.IsDebug)
                    Console.WriteLine($"Serialize {src.GetType()} MESSAGE LEN:" + result.Length);

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Cannot serialize {src.GetType().Name}: {ex.Message}", ex);
            }
        }
    }
}