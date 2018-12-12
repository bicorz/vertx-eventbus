using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace io.vertx.core.eventbus.impl
{
    class Handlers<T>
    {

        private int pos;
        public readonly List<T> List = new List<T>();

        public T Choose()
        {
            while (true)
            {
                int size = this.List.Count;
                if (size == 0)
                {
                    return (T)(object)null;
                }

                int p = Interlocked.Increment(ref this.pos);
                if (p >= size - 1)
                {
                    p = 0;
                }
                try
                {
                    return this.List[p];
                }
                catch (ArgumentOutOfRangeException)
                {
                    this.pos = 0;
                }
            }
        }

    }
}
