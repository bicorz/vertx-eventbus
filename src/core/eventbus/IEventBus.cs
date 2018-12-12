using System;

namespace io.vertx.core.eventbus
{

    public interface IEventBus
    {
        IEventBus Send(string address, object message);
        IEventBus Send<T>(string address, object message, Action<IAsyncResult<IMessage<T>>> replyHandle);
        IEventBus Send(string address, object message, DeliveryOptions options);
        IEventBus Send<T>(string address, object message, DeliveryOptions options, Action<IAsyncResult<IMessage<T>>> replyHandle);

        IEventBus Publish(string address, object message);
        IEventBus Publish(string address, object message, DeliveryOptions options);

        IMessageConsumer<T> Consumer<T>(string address);
        IMessageConsumer<T> Consumer<T>(string address, Action<IMessage<T>> handler);

        //IMessageConsumer<T> Sender<T>(string address);
        //IMessageConsumer<T> Sender<T>(string address, Action<IMessage<T>> handler);

        //IMessageConsumer<T> Publisher<T>(string address);
        //IMessageConsumer<T> Publisher<T>(string address, Action<IMessage<T>> handler);
    }

}
