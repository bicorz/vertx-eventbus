using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace io.vertx.core.eventbus.impl
{
    public class Message<T> : IMessage<T>
    {
        private readonly Hashtable headers;
        private readonly IEventBus eventBus;

        public string Address { get; private set; }
        public T Body { get; private set; }
        public string ReplyAddress { get; set; }
        public bool IsSend { get; private set; }

        public Message(IEventBus eventBus, string address, T body, string replyAddress, bool isSend)
        {
            this.eventBus = eventBus;
            this.headers = new Hashtable();

            this.Address = address;
            this.Body = body;
            this.ReplyAddress = replyAddress;
            this.IsSend = isSend;

        }

        public void Fail(int failureCode, string message)
        {
            throw new NotImplementedException();
        }

        public void Reply(object message)
        {
            this.eventBus.Send(this.ReplyAddress, message);
        }

        public void Reply<R>(object message, Action<IAsyncResult<IMessage<R>>> replyHandlers)
        {
            this.eventBus.Send(this.ReplyAddress, message, replyHandlers);
        }

        public Hashtable Headers()
        {
            throw new NotImplementedException();
        }
    }
}
