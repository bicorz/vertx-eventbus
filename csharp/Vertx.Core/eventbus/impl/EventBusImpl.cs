using io.vertx.core.logging;
using io.verx.core.eventbus;
using log4net;
using System.Collections;

namespace io.vertx.core.eventbus.impl
{
    public class EventBusImpl : EventBus
    {

        private static readonly ILog Log = Logger.GetLogger(typeof(EventBusImpl));

        protected readonly Hashtable handlersHash = new Hashtable();

        public virtual void Consumer(string address, Handler<Message<object>> handler)
        {
            lock(this.handlersHash)
            {
                Log.InfoFormat("consumer register action for address {0}", address);
                var handlers = (Handlers<Message<object>>) handlersHash[address];
                if(handlers == null)
                {
                    handlers = new Handlers<Message<object>>();
                    handlersHash[address] = handlers;
                }
                handlers.List.Add(handler);
            }
        }

        public virtual void RemoveConsumer(string address, Handler<Message<object>> handler)
        {
            lock (this.handlersHash)
            {
                Log.InfoFormat("consumer remove action for address {0}", address);
                var handlers = (Handlers<Message<object>>) handlersHash[address];
                if (handlers != null)
                {
                    handlers.List.Remove(handler);
                }
            }
        }

        public virtual void Send(string address, object body, string replyAddress, DeliveryOptions deliveryOptions, Handler<Message<object>> replyHandler)
        {
            lock (this.handlersHash)
            {
                Log.InfoFormat("send to address {0} with replyAddress {1}", address, replyAddress);
                var handlers = (Handlers<Message<object>>) handlersHash[address];
                if (handlers != null)
                {
                    var chosenHandler = handlers.Choose();
                    if (chosenHandler != null)
                    {
                        Message<object> message = new Message<object>(address, replyAddress, null, body, true);
                        chosenHandler.BeginInvoke(message, 
                        (iAsyncResult) => 
                        {
                            replyHandler?.Invoke(null);
                        }, chosenHandler.Target);
                    }
                }
            }
        }

        public virtual void Publish(string address, object body, DeliveryOptions deliveryOptions)
        {
            lock (this.handlersHash)
            {
                Log.InfoFormat("publish to address {0}", address);
                var handlers = (Handlers<Message<object>>) handlersHash[address];
                if (handlers != null)
                {
                    foreach(var handler in handlers.List)
                    {
                        Message<object> message = new Message<object>(address, null, null, body, true);
                        handler.BeginInvoke(message, null, handler.Target);
                    }
                }
            }
        }

    }
}
