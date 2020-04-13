using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PoC.HttpRequest.Throttling
{
    public class DynamicSemaphoreSlim : IDisposable
    {
        //0 - false, 1 - true
        private int updateMode = 0;

        private SemaphoreSlim semaphoreSlim;
        private CancellationTokenSource cancellationTokenSource;
        private int semaphoreCount;


        public DynamicSemaphoreSlim(int count)
        {
            cancellationTokenSource = new CancellationTokenSource();
            semaphoreSlim = new SemaphoreSlim(count, count);
            semaphoreCount = count;
        }

        //True when changes has been applied. //False when not applied. (update in progress)
        public bool TryChangeCount(int newCount)
        {
            if (Interlocked.Exchange(ref updateMode, 1) != 0)
                return false; // update in progress ignore it.

            try
            {
                if (newCount != semaphoreCount)
                {
                    var oldSemaphore = semaphoreSlim;
                    var oldCancellationTokenSource = cancellationTokenSource;

                    semaphoreSlim = new SemaphoreSlim(newCount, newCount);
                    cancellationTokenSource = new CancellationTokenSource();
                    semaphoreCount = newCount;
                    
                    oldCancellationTokenSource.Cancel(throwOnFirstException: false);
                    oldCancellationTokenSource.Dispose();
                    oldSemaphore.Dispose();
                }
                return true;
            } // possible memory leak?
            finally
            {
                Interlocked.Exchange(ref updateMode, 0);
            }
        }

        public async Task WaitAsync(CancellationToken cancellationToken)
        {
            try
            {
                using (var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cancellationTokenSource.Token))
                    await semaphoreSlim.WaitAsync(linked.Token);
            }
            catch(Exception e) when (e is ObjectDisposedException || (e is OperationCanceledException && !cancellationToken.IsCancellationRequested))
            {
                if (semaphoreSlim != null) //semaphore has been changed. re-enter to queue. 
                    await WaitAsync(cancellationToken);
            }
        }

        public void Release()
        {
            try
            {
                semaphoreSlim.Release();
            }//there can be exceptions when semaphore has been changed.
            catch (ObjectDisposedException) { }
            catch (SemaphoreFullException) { }
        }

        public void Dispose()
        {
            semaphoreSlim?.Dispose();
            semaphoreSlim = null;
        }
    }
}
