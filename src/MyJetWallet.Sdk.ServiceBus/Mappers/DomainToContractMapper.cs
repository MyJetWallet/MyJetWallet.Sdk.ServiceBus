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
                if (src == null)
                    throw new ArgumentException("Packet cannot be null", nameof(src));
                
                MemoryStream memoryStream = new MemoryStream();
                memoryStream.WriteByte((byte) 0);
                Serializer.Serialize<object>((Stream) memoryStream, src);
                byte[] array = memoryStream.ToArray();
            
                if (ServiceBusContracts.IsDebug)
                    Console.WriteLine($"Serialize {(object) src.GetType()} MESSAGE LEN:" + array.Length);
            
                return array;
            }
            catch (Exception ex)
            {
                throw new Exception($"Cannot serialize {src.GetType().Name}: {ex.Message}", ex);
            }
            
        }
    }
}