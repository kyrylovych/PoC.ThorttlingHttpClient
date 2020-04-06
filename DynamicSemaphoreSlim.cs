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
        private const int Infinity = int.MaxValue;
        private readonly ReaderWriterLockSlim _readerWriterLockSlim = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        private readonly SemaphoreSlim _innerSlim;

        public DynamicSemaphoreSlim(int count)
        {
            _count = count;
            _innerSlim = new SemaphoreSlim(_count, Infinity);
        }

        public void TryChangeCount(int newCount)
        {
            if (newCount == _count)
                return;

            try
            {
                _readerWriterLockSlim.EnterWriteLock();
                if (newCount < _count)
                    Decrease(_count - newCount);
                else
                    Increase(newCount - _count);

                _count = newCount;
            }
            finally
            {
                _readerWriterLockSlim.ExitWriteLock();
            }
        }

        protected void Decrease(int count)
        {
            if (_count < count)
                return; // log warning?
            for (int i = 0; i < count; i++)
            {
                _innerSlim.Wait();
            }
        }

        protected void Increase(int count)
        {
            if (count < 1)
                return; //log warning or error?.
            
            _innerSlim.Release(count);
        }

        public async Task WaitAsync(CancellationToken cancellationToken)
        {
            try
            {
                _readerWriterLockSlim.EnterReadLock();
                await _innerSlim.WaitAsync(cancellationToken);
            }
            finally
            {
                _readerWriterLockSlim.ExitReadLock();
            }
        }

        public void Release()
        {
            try
            {
                _readerWriterLockSlim.EnterReadLock();
                _innerSlim.Release();
            }
            finally
            {
                _readerWriterLockSlim.ExitReadLock();
            }
        }

        public void Dispose()
        {
            _readerWriterLockSlim?.Dispose();
            _innerSlim?.Dispose();
        }
    }
}
