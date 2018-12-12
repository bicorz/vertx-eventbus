using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace io.vertx.core.eventbus
{
    public interface IMessageProducer<T>
    {

        string Address { get;  }

        IMessageProducer<T> Send(T message);
        IMessageProducer<T> Send(T message, Action<Task<IMessage<T>>> replyHandler);

    }
}
