using io.vertx.core.eventbus.impl;
using io.vertx.core.impl;
using io.verx.core.eventbus;

namespace io.vertx.core
{

    public delegate void Handler<E>(E evt);

    public abstract class Vertx
    {

        public static Vertx vertx()
        {
            return new VertxImpl();
        }

        public static Vertx vertx(VertxOptions options)
        {
            return new VertxImpl();
        }

        public EventBus EventBus { get; }

        public Vertx() : this(null)
        {

        }

        public Vertx(VertxOptions options)
        {
            this.EventBus = new EventBusImpl();
        }

    }
}
