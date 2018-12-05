using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace io.vertx.core.eventbus.impl
{
    class Handlers<T>
    {

        private int pos;
        public readonly List<Handler<T>> List = new List<Handler<T>>();

        public Handler<T> Choose()
        {
            while (true)
            {
                int size = this.List.Count;
                if (size == 0)
                {
                    return null;
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
                catch (ArgumentOutOfRangeException e)
                {
                    this.pos = 0;
                }
            }
        }

    }
}
