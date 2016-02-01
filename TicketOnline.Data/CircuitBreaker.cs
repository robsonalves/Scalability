using System;
using System.Threading.Tasks;

namespace TicketOnline.Data
{
    public class CircuitBreakerOpenException : Exception
    {
        public CircuitBreakerOpenException(Exception ex) : base("Circuit breaker open", ex)
        {

        }
    }

    public enum CircuitBreakerState
    {
        Closed,
        Open,
        HalfOpen
    }
    public class CircuitBreaker
    {
        private CircuitBreakerState _state;
        private DateTime lastFailureTime;
        private Exception lastException;
        private const int SECONDS_TO_WAIT_FOR_HALFOPEN = 60;
        private object padLock;

        public CircuitBreaker()
        {
            _state = CircuitBreakerState.Closed;
            padLock = new object();
        }

        public void Execute(Action action)
        {
            if (IsClosed())
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    ProcessException(ex);
                }
            }
            if (IsOpen())
            {
                if (CanTryHalfOpen())
                {
                    try
                    {
                        action();
                        ResetCircuitBreaker();
                    }
                    catch (Exception ex)
                    {
                        ProcessException(ex);
                    }
                }
                else
                {
                    throw new CircuitBreakerOpenException(lastException);
                }
            }
        }

        public async Task ExecuteAsync(Func<Task> action)
        {
            if (IsClosed())
            {
                try
                {
                    await action();
                }
                catch (Exception ex)
                {
                    ProcessException(ex);
                }
            }
            if (IsOpen())
            {
                if (CanTryHalfOpen())
                {
                    try
                    {
                        await action();
                        ResetCircuitBreaker();
                    }
                    catch (Exception ex)
                    {
                        ProcessException(ex);
                    }
                }
                else
                {
                    throw new CircuitBreakerOpenException(lastException);
                }
            }
        }

        private void ResetCircuitBreaker()
        {
            lock (padLock)
            {
                _state = CircuitBreakerState.Closed;
            }
        }

        private bool CanTryHalfOpen()
        {
            var minumumTryDatetime = lastFailureTime.AddSeconds(SECONDS_TO_WAIT_FOR_HALFOPEN);
            return DateTime.Now >= minumumTryDatetime;
        }

        private bool IsClosed()
        {
            return _state == CircuitBreakerState.Closed;
        }
        private bool IsOpen()
        {
            return _state == CircuitBreakerState.Open;
        }

        private void ProcessException(Exception ex)
        {
            lock (padLock)
            {
                lastFailureTime = DateTime.Now;
                lastException = ex;
                _state = CircuitBreakerState.Open;
            }
            throw new CircuitBreakerOpenException(lastException);
        }

    }
}
