using System;
using System.Collections;

namespace io.vertx.core.eventbus
{

    public interface IMessage<T>
    {
        string Address { get; }
        T Body { get;  }
        string ReplyAddress { get;  }
        bool IsSend { get; }

        Hashtable Headers();
        void Reply(object message);
        void Reply<R>(object message, Action<IAsyncResult<IMessage<R>>> replyHandlers);
        void Fail(int failureCode, String message);
    }

}
