using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service.Tools;

namespace MyJetWallet.Sdk.ServiceBus
{
    public class ServiceBusLifeTime
    {
        private readonly IServiceBusManager[] _clients;
        private readonly ILogger<ServiceBusLifeTime> _logger;
        private MyTaskTimer _timer;
        private bool _watcherStarted = false;

        public ServiceBusLifeTime(IServiceBusManager[] clients, ILogger<ServiceBusLifeTime> logger)
        {
            _clients = clients;
            _logger = logger;
            _timer = MyTaskTimer.Create<ServiceBusLifeTime>(TimeSpan.FromMinutes(1), logger, Watcher);
            _timer.Start();
        }

        private Task Watcher()
        {
            if (!_watcherStarted)
            {
                _watcherStarted = true;
                return Task.CompletedTask;
            }

            foreach (var client in _clients)
            {
                if (!client.IsStarted)
                {
                    _logger.LogError("ServiceBus client is NOT STARTED");
                }
            }
            
            return Task.CompletedTask;
        }

        public void Start()
        {
            foreach (var client in _clients)
            {
                try
                {
                    client.Start();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Cannot start serviceBus");
                    throw;
                }
            }
        }
        
        public void Stop()
        {
            foreach (var client in _clients)
            {
                try
                {
                    client.Stop();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Cannot stop serviceBus");
                }
            }
        }
    }
}