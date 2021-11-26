using MyServiceBus.TcpClient;

namespace MyJetWallet.Sdk.ServiceBus
{
    public interface IServiceBusManager
    {
        void Start();
        void Stop();
        
        bool IsStarted { get; }
        
        bool IsConnected { get; }
        
        string HostPort { get; }
    }

    public class ServiceBusManager : IServiceBusManager
    {
        public string HostPort { get; }
        
        private readonly MyServiceBusTcpClient _client;

        public ServiceBusManager(MyServiceBusTcpClient client, string hostPort)
        {
            HostPort = hostPort;
            _client = client;
        }

        public void Start()
        {
            _client.Start();
            IsStarted = true;
        }

        public void Stop()
        {
            _client.Stop();
        }

        public bool IsStarted { get; set; }

        public bool IsConnected => _client?.Connected ?? false;
    }
}