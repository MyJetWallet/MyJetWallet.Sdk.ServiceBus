using System;
using MyNoSqlServer.Abstractions;

namespace MyJetWallet.Sdk.ServiceBus.Models;

public class DeduplicationNoSqlEntity : MyNoSqlDbEntity
{
    public static string GeneratePartitionKey(string topicName) => topicName;

    public static string GenerateRowKey(string messageId) => messageId;


    public static DeduplicationNoSqlEntity Create(string topicName, string handledMessageId, TimeSpan expiration) =>
        new()
        {
            PartitionKey = GeneratePartitionKey(topicName),
            RowKey = GenerateRowKey(handledMessageId),
            Expires = DateTime.UtcNow.Add(expiration)
        };
}