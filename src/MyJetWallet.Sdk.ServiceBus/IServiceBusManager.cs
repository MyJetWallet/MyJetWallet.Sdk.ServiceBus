using MyServiceBus.TcpClient;

namespace MyJetWallet.Sdk.ServiceBus
{
    public interface IServiceBusManager
    {
        void Start();
        void Stop();
        
        bool IsStarted { get; }
    }

    public class ServiceBusManager : IServiceBusManager
    {
        private readonly MyServiceBusTcpClient _client;

        public ServiceBusManager(MyServiceBusTcpClient client)
        {
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
    }
}