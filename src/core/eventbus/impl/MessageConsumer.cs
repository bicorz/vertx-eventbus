using System;
using System.Collections.Generic;
using System.Text;

namespace io.vertx.core.eventbus.impl
{
    class MessageConsumer<T> : IMessageConsumer<T>
    {

        private readonly EventBus eventBus;

        public Action<IMessage<T>> HandlerAction { get; private set; }
        public string Address { get; private set; }
        public bool IsRegistered { get; private set; }

        public MessageConsumer(EventBus eventBus, string address)
        {
            this.eventBus = eventBus;
            this.IsRegistered = false;
            this.Address = address;
        }

        public IMessageConsumer<T> Handler(Action<IMessage<T>> handler)
        {
            this.HandlerAction = handler;
            this.IsRegistered = true;
            return this;
        }

        public void UnRegister()
        {
            this.eventBus.UnRegister<T>(this);
            this.HandlerAction = null;
            this.IsRegistered = false;
        }

    }
}
