using System;
using System.Collections.Generic;
using System.Text;

namespace io.vertx.core
{
    public class AsyncResult<T>
    {
        public T Result { get; }
        public Exception Cause { get; }
        public bool IsSucceeded { get;  }
        public bool IsFailed { get;  }
    }
}
