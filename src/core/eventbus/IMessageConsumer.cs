using System;
using System.Collections.Generic;
using System.Text;

namespace io.vertx.core.eventbus
{
    public interface IMessageConsumer<T>
    {
        string Address { get;  }
        bool IsRegistered { get; }
        IMessageConsumer<T> Handler(Action<IMessage<T>> handler);
        void UnRegister();
    }
}
