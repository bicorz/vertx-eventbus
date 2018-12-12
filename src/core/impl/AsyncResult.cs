using System;

namespace io.vertx.core.impl
{
    public class AsyncResult<T> : IAsyncResult<T>
    {
        public T Result { get; private set; }
        public bool IsSucceeded { get; private set; }
        public bool IsFailed { get; private set; }
        public Exception Cause { get; private set; }

        public void Succeeded(T result)
        {
            this.IsSucceeded = true;
            this.Result = result;
        }

        public void Failed(Exception cause)
        {
            this.IsFailed = true;
            this.Cause = cause;
        }

    }
}
