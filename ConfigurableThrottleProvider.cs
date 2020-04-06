using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace PoC.HttpRequest.Throttling
{
    public class ConfigurableThrottleProvider : IDisposable
    {
        private readonly ConcurrentDictionary<string, DynamicSemaphoreSlim> _cache = new ConcurrentDictionary<string, DynamicSemaphoreSlim>();
        private readonly IDisposable _changeListener;
        private readonly IOptionsMonitor<ThrottleOptions> _throttleOptions;
        public ConfigurableThrottleProvider(IOptionsMonitor<ThrottleOptions> throttleOptions)
        {
            _throttleOptions = throttleOptions;
            _changeListener = SubscribeOnChanges();
        }

        private IDisposable SubscribeOnChanges()
        {
            return _throttleOptions.OnChange((opt, name) =>
            {
                if (!_cache.TryGetValue(name, out var dynamicSlim))
                    return;

                dynamicSlim.TryChangeCount(opt.MaxCount);
            });
        }

        public DynamicSemaphoreSlim Get(string name)
            => _cache.GetOrAdd(name, (n) =>
             {
                 var options = _throttleOptions.Get(n);
                 return new DynamicSemaphoreSlim(options.MaxCount);
             });

        public void Dispose()
        {
            foreach (var key in _cache.Keys)
            {
                if (_cache.TryGetValue(key, out var slim))
                {
                    slim.Dispose();
                }
            }
            _cache.Clear();
            _changeListener?.Dispose();
        }
    }
}
