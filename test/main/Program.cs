using io.vertx.core.eventbus;
using io.vertx.core.eventbus.impl;
using io.vertx.core.logging;
using io.vertx.ext.tcpbridge.client;
using log4net;
using log4net.Config;
using Newtonsoft.Json.Linq;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace main
{
    class Program
    {
        static void Main(string[] args)
        {
            SimpleTcpBridgeClientTest();

            Console.WriteLine("EOF");
            Console.ReadKey();
        }

        static void SimpleTcpBridgeClientTest()
        {
            
            TcpBridgeClient client = new TcpBridgeClient("127.0.0.1", 7000);
            JObject data = new JObject();
            data["hi"] = "from c#";
            client.Send("test", data);

        }

        static void SimpleBusTest()
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

        }
    }
}
