using io.vertx.core.impl;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace io.vertx.core.eventbus.impl
{
    public class EventBus : IEventBus
    {

        private readonly ConcurrentDictionary<string, Handlers<MessageConsumer<object>>> consumerActionHash;
        private int genAddressCount = 0;
        
        public EventBus()
        {
            this.consumerActionHash = new ConcurrentDictionary<string, Handlers<MessageConsumer<object>>>();
        }

        public IMessageConsumer<T> Consumer<T>(string address)
        {
            IMessageConsumer<T> toReturn = new MessageConsumer<T>(this, address);
            return toReturn;
        }

        public IMessageConsumer<T> Consumer<T>(string address, Action<IMessage<T>> handler)
        {
            MessageConsumer<T> toReturn = new MessageConsumer<T>(this, address);
            Handlers<MessageConsumer<object>> actions;
            lock (consumerActionHash)
            {
                if (consumerActionHash.ContainsKey(address))
                {
                    actions = consumerActionHash[address];
                }
                else
                {
                    actions = new Handlers<MessageConsumer<object>>();
                    consumerActionHash[address] = actions;
                }
                actions.List.Add((MessageConsumer<object>)(object)toReturn);
            }
            toReturn.Handler(handler);
            return toReturn;
        }

        public void UnRegister<T>(IMessageConsumer<T> consumer)
        {
            Handlers<MessageConsumer<object>> actions;
            lock (consumerActionHash)
            {
                if (consumerActionHash.ContainsKey(consumer.Address))
                {
                    actions = consumerActionHash[consumer.Address];
                    actions.List.Remove((MessageConsumer<object>)(object)consumer);
                }
            }
        }

        public IEventBus Publish(string address, object message)
        {
            return this.Publish(address, message, new DeliveryOptions());
        }

        public IEventBus Send(string address, object message)
        {
            return this.Send(address, message, new DeliveryOptions());
        }

        public IEventBus Send<T>(string address, object message, Action<IAsyncResult<IMessage<T>>> replyHandle)
        {
            return this.Send<T>(address, message, new DeliveryOptions(), replyHandle);
        }

        public IEventBus Send(string address, object message, DeliveryOptions options)
        {
            var msg = new Message<object>(this, address, message, null, true);
            this.SendOrPubInternal<object>(msg, options, null);
            return this;
        }

        public IEventBus Send<T>(string address, object message, DeliveryOptions options, Action<IAsyncResult<IMessage<T>>> replyHandle)
        {
            var msg = new Message<object>(this, address, message, null, true);
            this.SendOrPubInternal<T>(msg, options, replyHandle);
            return this;
        }

        public IEventBus Publish(string address, object message, DeliveryOptions options)
        {
            var msg = new Message<object>(this, address, message, null, false);
            this.SendOrPubInternal<object>(msg, options, null);
            return this;
        }

        private void SendOrPubInternal<T>(Message<object> message, DeliveryOptions deliveryOptions, Action<IAsyncResult<IMessage<T>>> replyHandle)
        {
            MessageConsumer<object> consumer = null;
            lock (consumerActionHash)
            {
                if (consumerActionHash.ContainsKey(message.Address))
                {
                    var consumers = consumerActionHash[message.Address];
                    if(message.IsSend)
                    {
                        consumer = consumers.Choose();
                    }
                    else
                    {
                        foreach(var currConsumer in consumers.List)
                        {
                            Task.Run(() => currConsumer?.HandlerAction(message));
                        }
                    }
                }
            }
            if(message.IsSend)
            {
                Task<bool> actionTask = Task.Factory.StartNew(() =>
                {
                    var handlerAction = consumer?.HandlerAction;
                    if(handlerAction != null)
                    {
                        handlerAction(message);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                });
                if(replyHandle != null)
                {
                    //creating temporary address for reply handle which will be invalid after ttl
                    message.ReplyAddress = this.NewAddress();
                    var tempMessageConsumer = this.Consumer<T>(message.ReplyAddress, (msg) =>
                    {
                        var asyncResult = new AsyncResult<IMessage<T>>();
                        asyncResult.Succeeded(msg);
                        replyHandle(asyncResult);
                    });

                    //clear after ttl
                    Task timeoutTask = Task.Delay((int)deliveryOptions.Timeout);
                    timeoutTask.ContinueWith((taskResult) => tempMessageConsumer.UnRegister());

                    Task.WhenAny(timeoutTask, actionTask).ContinueWith((taskResult) =>
                    {
                        Exception ex = null;
                        if (taskResult.Result == actionTask)
                        {
                            ex = taskResult.Result.Exception;
                            if(ex == null && !actionTask.Result)
                            {
                                ex = new Exception($"No consumer found at the address:{message.Address}");
                            }
                        }
                        else
                        {
                            ex = new Exception($"SendOrPublish Timeout of {deliveryOptions.Timeout}ms");
                        }
                        if (replyHandle != null && ex != null)
                        {
                            var asyncResult = new AsyncResult<IMessage<T>>();
                            asyncResult.Failed(ex);
                            Task.Factory.StartNew(() => replyHandle(asyncResult));
                        }
                    });
                }
            }
        }

        private string NewAddress()
        {
            return $"{Interlocked.Increment(ref this.genAddressCount)}";
        }

    }
}
