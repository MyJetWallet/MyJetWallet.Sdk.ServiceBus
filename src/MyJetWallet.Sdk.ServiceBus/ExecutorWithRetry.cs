using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace MyJetWallet.Sdk.ServiceBus
{
    public class ExecutorWithRetry<T>
    {
        private int Counter { get; set; }
        private string LastId { get; set; }
        private readonly Func<T, ValueTask> _callback;
        private readonly ILogger _logger;
        private readonly Func<T, string> _errorMessageGetter;
        private readonly Func<T, string> _packageIdGetter;
        private readonly int _retryCount;
        private readonly int _delayInMs;

        public ExecutorWithRetry(Func<T, ValueTask> callback, 
            ILogger logger, 
            Func<T, string> errorMessageGetter,
            Func<T, string> packageIdGetter,
            int retryCount,
            int delayInMs)
        {
            _callback = callback;
            _logger = logger;
            _errorMessageGetter = errorMessageGetter;
            _packageIdGetter = packageIdGetter;
            _retryCount = retryCount;
            _delayInMs = delayInMs;
        }
        
        public async ValueTask Execute(T package)
        {
            try
            {
                var packageId = _packageIdGetter.Invoke(package);
                if (LastId == packageId && Counter == 0)
                {
                    return;
                }
                LastId = packageId;
                await _callback.Invoke(package);
                Counter = 0;
            }
            catch (Exception ex)
            {
                var errorMessage = _errorMessageGetter.Invoke(package);
                _logger.LogError(ex, errorMessage);
                Counter++;
                if (Counter <= _retryCount)
                {
                    await Task.Delay(_delayInMs);
                    throw;
                }
                _logger.LogError(ex, "SKIP:" + errorMessage + " Package:" + JsonConvert.SerializeObject(package));
                Counter = 0;
            }
        }
    }
}