using io.vertx.core.eventbus;
using io.vertx.core.eventbus.impl;
using io.vertx.core.logging;
using log4net;
using log4net.Config;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace main
{
    class Program
    {
        static void Main(string[] args)
        {
            var repo = LogManager.GetRepository(Logger.LoggerRepositoryName);
            BasicConfigurator.Configure(repo);

            IEventBus bus = new EventBus();
            var consumer = bus.Consumer<object>("test", (message) =>
            {
                Console.WriteLine("@test: "+message.Body);
                message.Reply("who called me?(from test)");
            });

            bus.Send("test", "some");

            Console.WriteLine("EOF.");
            Console.ReadKey();
        }
    }
}
