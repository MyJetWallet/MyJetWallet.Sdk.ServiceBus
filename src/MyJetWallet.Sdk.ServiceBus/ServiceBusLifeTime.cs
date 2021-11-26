using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service.LivnesProbs;
using MyJetWallet.Sdk.Service.Tools;

namespace MyJetWallet.Sdk.ServiceBus
{
    public class ServiceBusLifeTime: ILivenessReporter
    {
        private readonly IServiceBusManager[] _clients;
        private readonly ILogger<ServiceBusLifeTime> _logger;
        private MyTaskTimer _timer;
        private bool _watcherStarted = false;

        private int _countFailConnect = 0;

        public ServiceBusLifeTime(IServiceBusManager[] clients, ILogger<ServiceBusLifeTime> logger)
        {
            _clients = clients;
            _logger = logger;
            _timer = MyTaskTimer.Create<ServiceBusLifeTime>(TimeSpan.FromMinutes(1), logger, Watcher);
            _timer.Start();
        }

        private async Task Watcher()
        {
            if (!_watcherStarted)
            {
                await Task.Delay(60000);
                _watcherStarted = true;
            }

            var connected = true;
            foreach (var client in _clients)
            {
                if (!client.IsStarted)
                {
                    _logger.LogError("ServiceBus client is NOT STARTED");
                }
                
                if (!client.IsConnected)
                {
                    _logger.LogError("ServiceBus client is NOT CONNECTED: {text}", client.HostPort);
                    connected = false;
                }
            }

            if (connected)
                _countFailConnect = 0;
            else
                _countFailConnect++;
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

            var retryCount = 0;
            foreach (var client in _clients)
            {
                while (retryCount < 10 && !client.IsConnected)
                {
                    Console.WriteLine($"[{retryCount}] ServiceBus client is not connected, wait 1 second ({client.HostPort})");
                    Task.Delay(1000).GetAwaiter().GetResult();
                    retryCount++;
                }
            }
            
            foreach (var client in _clients)
            {
                if (!client.IsConnected)
                {
                    _logger.LogError("ServiceBus {text} cannot connect to server!", client.HostPort);
                    throw new Exception($"ServiceBus {client.HostPort} cannot connect to server!");
                }
            }
            
            Console.WriteLine("ServiceBus is started");
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

        public (string, List<string>) GetIssues()
        {
            var list = new List<string>();
            if (_countFailConnect >= 3)
            {
                foreach (var client in _clients)
                {
                    if (!client.IsConnected)
                    {
                        list.Add($"ServiceBus client is NOT CONNECTED: {client.HostPort}");
                        _logger.LogError("Detect ISSUE: ServiceBus client is NOT CONNECTED: {text}", client.HostPort);
                    }
                }
            }

            return ("ServiceBusLifeTime", list);
        }
        
    }
}