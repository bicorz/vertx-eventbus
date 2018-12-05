# vertx-eventbus-bridge-clients
This project is created to have vertx bridge-client for various languages. Currently we have only for .NET (all framework). 
To contribute on other languages, Pull Requests are always welcome. Below is a sample program using TcpBridgeClient.
```cs
            TcpBridgeClient tcpBridgeClient = new TcpBridgeClient( "127.0.0.1", 7000, option);
            tcpBridgeClient.Register(
                  "client.dotnet.test",                                              //registered address
                  (message) =>  Console.WriteLine("message received from server");   //callback
            );
            tcpBridgeClient.Send(
                  "server.echo",          //server address
                  "message to server",    //message
                  "client.dotnet.test",   //callback address registered above
                  );       
```
You can link the library as a nuget package at https://www.nuget.org/packages/Vertx.TcpBridge/
