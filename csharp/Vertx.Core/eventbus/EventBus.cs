using io.vertx.core;
using io.vertx.core.eventbus;
using System;
using System.Collections.Generic;
using System.Text;

namespace io.verx.core.eventbus
{

    public interface EventBus
    {

        void Send(string address, object body, string replyAddress, DeliveryOptions deliveryOptions, Handler<Message<object>> replyHandler);

        void Publish(string address, object body, DeliveryOptions deliveryOptions);

        void Consumer(string address, Handler<Message<object>> handler);

        void RemoveConsumer(string address, Handler<Message<object>> handler);

    }

}
