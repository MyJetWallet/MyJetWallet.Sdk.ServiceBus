using System.Runtime.Serialization;

namespace MyJetWallet.Sdk.ServiceBus.Tests;

public class HeadersTests
{
    [DataContract]
    private class TestObj
    {
        [DataMember(Order = 1)]
        public required string Prop1 { get; init; }
    }
    private readonly Dictionary<string, string> headers = new()
    {
        {"key1", "value1"},
        {"key2", "value2"}
    };
    private readonly TestObj obj = new() { Prop1 = "test" };


    [Test]
    public void SerializeDeserializeWithHeadersTest()
    {
        ReadOnlyMemory<byte> bytes = DomainToContractMapper.ServiceBusContractToByteArray(obj, headers);

        var res = bytes.ByteArrayToServiceBusContract<TestObj>();

        Assert.That(res.Data.Prop1, Is.EqualTo(obj.Prop1));
        CollectionAssert.AreEqual(headers, res.Headers);
    }

    [Test]
    public void SerializeDeserializeWithoutHeadersTest()
    {
        ReadOnlyMemory<byte> bytes = DomainToContractMapper.ServiceBusContractToByteArray(obj);

        var res = bytes.ByteArrayToServiceBusContract<TestObj>();

        Assert.That(res.Data.Prop1, Is.EqualTo(obj.Prop1));
        CollectionAssert.IsEmpty(res.Headers);
    }
}