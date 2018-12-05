using io.vertx.core;
using io.vertx.ext.tcpbridge.client;
using log4net.Config;
using System;
namespace TestMain
{
    class Program
    {

        static void Main(string[] args)
        {
            BasicConfigurator.Configure();

            BridgeClientOption option = new BridgeClientOption
            {
                IsSSL = true,
                CertificateValidationCallback = TcpBridgeClient.AllowAllServer
            };

            TcpBridgeClient tcpBridgeClient = new TcpBridgeClient( "127.0.0.1", 7000, option);
            tcpBridgeClient.Register("testJavaClient", (message) =>
            {
                Console.WriteLine("Message received from java");
            });
            tcpBridgeClient.Send("test", null, "testJavaClient", null, (message) =>
            {
                Console.WriteLine("Message sent succesfully");
            });
            Console.ReadKey();
            tcpBridgeClient.Close();
        }

    }
}
