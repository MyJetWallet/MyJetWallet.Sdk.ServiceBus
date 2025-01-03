using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProtoBuf;

namespace MyJetWallet.Sdk.ServiceBus
{
    public static class DomainToContractMapper
    {
        public static byte[] ServiceBusContractToByteArray(this object src)
        {
            try
            {
                using var stream = new MemoryStream();
                stream.WriteByte(0); // First byte is a version contract;

                Serializer.Serialize(stream, src);

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
        
        public static byte[] ServiceBusContractToByteArray(this object src, Dictionary<string, string> headers)
        {
            if (headers == null || !headers.Any())
                return ServiceBusContractToByteArray(src);
            
            try
            {
                using var stream = new MemoryStream();

                stream.WriteByte(1);

                using var headerStream = new MemoryStream();
                Serializer.Serialize(headerStream, headers);
                if(headerStream.Length > ushort.MaxValue)
                {
                    throw new Exception("Headers are too large to serialize");
                }
                byte[] sizeBytes = BitConverter.GetBytes((ushort)headerStream.Length);
                stream.Write(sizeBytes, 0, sizeBytes.Length);

                headerStream.WriteTo(stream);

                Serializer.Serialize(stream, src);

                var result = stream.ToArray();

                if (ServiceBusContracts.IsDebug)
                    Console.WriteLine($"Serialize {src.GetType()} MESSAGE LEN:" + result.Length);

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Cannot serialize with headers {src.GetType().Name}: {ex.Message}", ex);
            }
        }
    }
}