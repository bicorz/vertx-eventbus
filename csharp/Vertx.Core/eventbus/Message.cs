using io.verx.core.eventbus;
using IO.Vertx.Core.eventbus;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace io.vertx.core.eventbus
{
    public class Message<T>
    {

        public string Address { get; protected set; }
        public string ReplyAddress { get; protected set; }
        public readonly Headers Headers = new Headers();
        public T Body { get; protected set; }
        public bool IsSend { get; protected set; }

        public Message(string address, String replyAddress, Headers headers, T sentBody, bool send)
        {
            this.Address = address;
            this.ReplyAddress = ReplyAddress;
            this.Headers = headers;
            this.Body = sentBody;
            this.IsSend = send;
        }

        public void Reply<R>(object message, DeliveryOptions options, Handler<Message<R>> replyHandler)
        {

        }

    }
}
