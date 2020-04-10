using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PoC.HttpRequest.Throttling
{
    public class DynamicSemaphoreSlim : IDisposable
    {
        private int _count;
        private SemaphoreSlim _innerSlim;

        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public DynamicSemaphoreSlim(int count)
        {
            _count = count;
            _innerSlim = new SemaphoreSlim(_count, count);
        }

        //returns slim that will no longer be used.
        public void TryChangeCount(int newCount)
        {
            if (newCount == _count)
                return;

            var oldSlim = _innerSlim;
            var oldCancellationToken = _cancellationTokenSource;

            _cancellationTokenSource = new CancellationTokenSource();
            _innerSlim = new SemaphoreSlim(newCount, newCount);
            _count = newCount;
            
            oldCancellationToken.Cancel(throwOnFirstException: false);
            oldCancellationToken.Dispose();
            oldSlim.Dispose();
            Console.WriteLine("Reload completed");
        }

        public async Task WaitAsync(CancellationToken cancellationToken)
        {
            try
            {
                using (var src = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token))
                    await _innerSlim.WaitAsync(src.Token);
                
            }
            catch (Exception e) when(e is ObjectDisposedException || (e is OperationCanceledException && !cancellationToken.IsCancellationRequested))
            {
                if (!cancellationToken.IsCancellationRequested)
                  await WaitAsync(cancellationToken);
            }
        }

        public void Release()
        {
            try
            {
                _innerSlim.Release();
            }
            catch (ObjectDisposedException) { }
            catch (OperationCanceledException) { }
            catch (SemaphoreFullException) { }
        }

        public void Dispose()
        {
            _innerSlim?.Dispose();
        }
    }
}
