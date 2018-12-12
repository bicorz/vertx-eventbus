using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace io.vertx.core.eventbus.impl
{
    public class MessageProducer<T> : IMessageProducer<T>
    {
        public string Address { get; private set; }

        public IMessageProducer<T> Send(T message)
        {
            throw new NotImplementedException();
        }

        public IMessageProducer<T> Send(T message, Action<Task<IMessage<T>>> replyHandler)
        {
            throw new NotImplementedException();
        }
    }
}
