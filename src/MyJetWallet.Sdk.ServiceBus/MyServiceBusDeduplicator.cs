using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service.Tools;
using MyJetWallet.Sdk.ServiceBus.Models;
using MyNoSqlServer.Abstractions;
using MyNoSqlServer.DataWriter;

namespace MyJetWallet.Sdk.ServiceBus;

public class MyServiceBusDeduplicator<T> : IDeduplicator<T>
{
    private readonly Func<T, string> _toStringFunc;
    private readonly IMyNoSqlServerDataWriter<DeduplicationNoSqlEntity> _writer;
    private readonly string _topicName;
    private readonly TimeSpan _expirationTime;
    private readonly Dictionary<string, DateTime> _registry = new();
    private readonly MyTaskTimer _timer;

    public MyServiceBusDeduplicator(Func<T, string> toStringFunc, Func<string> getUrl, string tableName, string topicName, TimeSpan expirationTime, ILogger logger)
    {
        _toStringFunc = toStringFunc;
        _topicName = topicName;
        _expirationTime = expirationTime;
        _writer = new MyNoSqlServerDataWriter<DeduplicationNoSqlEntity>(getUrl, tableName, true);
        _timer = new MyTaskTimer(nameof(MyServiceBusDeduplicator<T>), _expirationTime, logger, CleanRegistry);
        _timer.Start();
    }

    public async Task<bool> IsDuplicate(T item)
    {
        if (!_registry.Any())
            await RefreshRegistry();

        return _registry.ContainsKey(_toStringFunc(item));
    }

    public async Task AddToRegistry(T item)
    {
        if (!_registry.Any())
            await RefreshRegistry();
        
        var entity = DeduplicationNoSqlEntity.Create(_topicName, _toStringFunc(item), _expirationTime);
        await _writer.InsertOrReplaceAsync(entity);
        _registry.TryAdd(_toStringFunc(item), DateTime.UtcNow);
    }

    private async Task RefreshRegistry()
    {
        var entities = await _writer.GetAsync();
        foreach (var entity in entities.ToList())
        {
            _registry.TryAdd(entity.PartitionKey, entity.Expires ?? DateTime.UtcNow);
        }
    }

    private Task CleanRegistry()
    {
        foreach (var (key, expiration) in _registry)
        {
            if (expiration < DateTime.UtcNow - _expirationTime)
            {
                _registry.Remove(key);
            }
        }
        return Task.CompletedTask;
    }
    
}