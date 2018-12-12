using io.vertx.core.eventbus;
using io.vertx.core.eventbus.impl;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace main
{
    class Program
    {
        static void Main(string[] args)
        {
            IEventBus bus = new EventBus();
            var consumer = bus.Consumer<object>("test", (message) =>
            {
                Console.WriteLine("@test: "+message.Body);
                message.Reply("who called me?(from test)");
            });

            Console.WriteLine("EOF.");
            Console.ReadKey();
        }
    }
}
