using System;
using System.Collections.Generic;
using System.Text;

namespace io.vertx.core
{
    public interface IAsyncResult<T>
    {
        T Result { get; }
        bool IsSucceeded { get; }
        bool IsFailed { get; }
        Exception Cause { get; }
    }
}
