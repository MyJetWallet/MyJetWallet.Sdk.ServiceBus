using MyJetWallet.Sdk.ServiceBus.Models;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;

namespace MyJetWallet.Sdk.ServiceBus
{
    public static class ContractToDomainMapper
    {
        public static MessageWithHeaders<T> ByteArrayToServiceBusContract<T>(this ReadOnlyMemory<byte> data)
        {
            var result = new MessageWithHeaders<T>();
            if (ServiceBusContracts.IsDebug)
                Console.WriteLine($"GOT {typeof(T)} MESSAGE LEN:" + data.Length);

            var span = data.Span;
            try
            {
                switch(span[0])
                {
                    case 0:
                        span = data[1..].Span;
                        break;
                    case 1:
                        var headerLength = BitConverter.ToInt16(span.Slice(1, 2));
                        result.Headers = DeserializeHeaders(data, headerLength);
                        var startMessage = 3 + headerLength; // 1 byte for version, 2 bytes for header length
                        span = data[startMessage..].Span;
                        break;
                    default:
                        throw new Exception("Not supported version of Contract");
                }
                using var mem = new MemoryStream(span.Length);
                mem.Write(span);
                mem.Position = 0;
                result.Data = Serializer.Deserialize<T>(mem);
                return result;
            }
            catch (Exception ex)
            {
                var dataBase64 = Convert.ToBase64String(data.ToArray());
                Console.WriteLine($"Cannot deserialize message {typeof(T).Name}. Data: '{dataBase64}'");

                throw new Exception($"Cannot deserialize message {typeof(T).Name}: {ex.Message}", ex);
            }

            throw new Exception("Not supported version of Contract");
        }

        private static Dictionary<string,string> DeserializeHeaders(ReadOnlyMemory<byte> data, short headerLength)
        {
            using var mem = new MemoryStream(headerLength);
            mem.Write(data.Slice(3, headerLength).Span);
            mem.Position = 0;
            return Serializer.Deserialize<Dictionary<string, string>>(mem);
        }
    }
}