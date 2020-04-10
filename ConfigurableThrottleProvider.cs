using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Options;

namespace PoC.HttpRequest.Throttling
{
    //this is generic class. DynamicSemaphoreSlim can be used directly in
    // others class. however code duplication is possible.
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
            _changeListener?.Dispose();
            foreach (var key in _cache.Keys)
            {
                if (_cache.TryGetValue(key, out var slim))
                {
                    slim.Dispose();
                }
            }
            _cache.Clear();
            
        }
    }
}
