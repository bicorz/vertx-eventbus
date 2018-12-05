using io.vertx.core;
using io.vertx.core.eventbus;
using io.vertx.core.eventbus.impl;
using io.vertx.core.logging;
using IO.Vertx.Core.eventbus;
using IO.Vertx.Core.net;
using log4net;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;

namespace io.vertx.ext.tcpbridge.client
{

    public interface ITcpBridgeClient
    {
        void Register(string address, Handler<Message<object>> handler);
        void Unregister(string address, Handler<Message<object>> handler);
        void Send(string address, object body, string replyAddress, DeliveryOptions deliveryOptions, Handler<AsyncResult<Message<object>>> replyHandler);
        void Publish(string address, object body, DeliveryOptions deliveryOptions);
    }

    public class TcpBridgeClient : ITcpBridgeClient
    {

        //statics
        private static readonly ILog Log = Logger.GetLogger(typeof(TcpBridgeClient));

        //consts
        private const string KeyType = "type";
        private const string KeyHeaders = "headers";
        private const string KeyBody = "body";
        private const string KeyAddress = "address";
        private const string KeyReplayAddress = "replyAddress";

        private const string KeyValueSend = "send";
        private const string KeyValuePublish = "publish";
        private const string KeyValueRegister = "register";
        private const string KeyValueUnregister = "unregister";
        private const string KeyValueErr = "err";
        private const string KeyValueMessage = "message";

        //members
        private readonly BridgeClientOption option;

        private readonly string address;
        private readonly int port;

        private readonly ManualResetEvent SocketReady;
        private readonly ManualResetEvent IsClosed;
        private readonly Thread workerThread;

        private readonly Hashtable handlersHash = new Hashtable(); //holds callbacks

        private TcpClient Client { get; set; }
        private Stream SocketStream { get; set; }
        private bool IsSocketClosed { get; set; }

        private readonly object writeLock = new object();
        private readonly UTF8Encoding encoding = new UTF8Encoding();

        private delegate void Action();

        //proxy constructors
        public TcpBridgeClient(string address, int port) : this(address, port, null) { }

        //Main constructor. if vertx provided, it will use that's eventbus. otherwise it will use its own eventbus.
        public TcpBridgeClient(string address, int port, BridgeClientOption option)
        {
            this.option = new BridgeClientOption(option);

            this.address = address;
            this.port = port;

            this.SocketReady = new ManualResetEvent(false);
            this.IsClosed = new ManualResetEvent(false);
            this.workerThread = new Thread(new ThreadStart(this.Run));
            workerThread.Start();
        }

        //continuously poll socket to distribute payload from server
        void Run()
        {
            byte[] bytes = new byte[1024];
            UTF8Encoding encoding = new UTF8Encoding();

            while (true)
            {
                if(this.IsClosed.WaitOne(0))
                {
                    Log.Warn("socket closed notified. breaking worker thread.");
                    break;
                }

                try
                {
                    this.ReopenSocket();
                    this.SocketReady.Set();
                }
                catch (Exception e)
                {
                    Log.Error($"exception while opening socket. will retry after {option.ReconnetDelayInSec} sec", e);
                    this.IsClosed.WaitOne(TimeSpan.FromSeconds(option.ReconnetDelayInSec));
                    continue;
                }

                BinaryReader reader = new BinaryReader(this.SocketStream, encoding);
                while (true)
                {
                    try
                    {
                        int payloadSize = SocketUtils.ReadInt(reader);
                        byte[] payloadBytes = reader.ReadBytes(payloadSize);
                        string payloadString = encoding.GetString(payloadBytes);
                        JObject payload = JObject.Parse(payloadString);
                        try
                        {
                            this.HandleToLocalBus(payload);
                        }
                        catch(Exception e)
                        {
                            Log.ErrorFormat("Exception while handling bridge object {0} {1}", payload, e);
                        }
                    }
                    catch(Exception e)
                    {
                        Log.Error($"exception while reading frame. will reconnect after {option.ReconnetDelayInSec} sec.", e);
                        this.SocketReady.Reset();
                        this.IsClosed.WaitOne(TimeSpan.FromSeconds(option.ReconnetDelayInSec));
                        break;
                    }
                }
            }
        }

        private void HandleToLocalBus(JObject payload)
        {
            Log.InfoFormat("Read Frame: {0}" , payload);
            string address = payload[KeyAddress]?.Value<string>();
            string replyAddress = payload[KeyReplayAddress]?.Value<string>();
            string payloadType = payload[KeyType]?.Value<string>();
            JObject body = payload[KeyBody]?.Value<JObject>();
            switch (payloadType)
            {
                case KeyValueSend:
                case KeyValuePublish:
                case KeyValueMessage:
                    {
                        Message<object> message = new Message<object>(address, replyAddress, null, body, payloadType != KeyValuePublish);
                        this.CallForSendOrPublish(message);
                        break;
                    }
                default:
                    Log.ErrorFormat("unkonwn payloadType : {0}", payloadType);
                    break;
            }
        }

        void CallForSendOrPublish(Message<object> message)
        {
            bool callFound = false;
            lock (this.handlersHash)
            {
                var handlers = (Handlers<Message<object>>)handlersHash[message.Address];
                if (handlers != null)
                {
                    foreach(var handler in handlers.List)
                    {
                        handler.BeginInvoke(message, null, null);
                        callFound = true;
                        if(message.IsSend)
                        {
                            break;
                        }
                    }
                }
                if (!callFound)
                {
                    Log.ErrorFormat("handlers not found for {0}", message.Address);
                }
            }
        }

        void ReopenSocket()
        {
            if (this.Client != null)
            {
                this.Client.Close();
            }
            this.Client = new TcpClient(this.address, this.port);
            if (this.option.IsSSL)
            {
                Stream rawStream = Client.GetStream();
                SslStream sslStream = new SslStream(rawStream, false, this.option.CertificateValidationCallback, this.option.CertificateSelectionCallback);
                sslStream.AuthenticateAsClient(this.address);
                this.SocketStream = sslStream;
            }
            else
            {
                this.SocketStream = Client.GetStream();
            }
            Log.InfoFormat("socket reopen complete on address {0}:{1}", this.address, this.port);
        }


        public static bool AllowAllServer(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            Log.InfoFormat("validatingServerCertificate. SslPolicyErrors:{0}", sslPolicyErrors);
            return true;
        }

        public void Close()
        {
            this.IsClosed.Set();
            this.Client?.Close();
        }

        public void Register(string address, Handler<Message<object>> handler)
        {
            lock (this.handlersHash)
            {
                Log.InfoFormat("consumer register action for address {0}", address);
                var handlers = (Handlers<Message<object>>)handlersHash[address];
                if (handlers == null)
                {
                    handlers = new Handlers<Message<object>>();
                    handlersHash[address] = handlers;
                }
                handlers.List.Add(handler);
            }
            this.RegisterOrRemove(true, address, null);
        }

        public void Unregister(string address, Handler<Message<object>> handler)
        {
            lock (this.handlersHash)
            {
                Log.InfoFormat("consumer remove action for address {0}", address);
                var handlers = (Handlers<Message<object>>)handlersHash[address];
                if (handlers != null)
                {
                    handlers.List.Remove(handler);
                }
            }
            this.RegisterOrRemove(false, address, null);
        }

        public void Send(string address, object body, string replyAddress, DeliveryOptions deliveryOptions, Handler<AsyncResult<Message<object>>> replyHandler)
        {
            Message<object> message = new Message<object>(address, replyAddress, deliveryOptions?.headers, body, true);
            this.SendOrPublish(message);
        }

        public void Publish(string address, object body, DeliveryOptions deliveryOptions)
        {
            Message<object> message = new Message<object>(address, null, deliveryOptions.headers, body, true);
            this.SendOrPublish(message);
        }

        void SendOrPublish(Message<object> message)
        {
            JObject sendObject = new JObject
            {
                [KeyType] = message.IsSend ? KeyValueSend : KeyValuePublish,
                [KeyAddress] = message.Address,
                [KeyBody] = (JObject)message.Body,
                [KeyReplayAddress] = message.ReplyAddress,
                [KeyHeaders] = message.Headers?.getAsJSON()
            };
            this.WriteFrame(sendObject);
        }

        void RegisterOrRemove(bool isRegister, string address, Headers headers)
        {
            JObject sendObject = new JObject
            {
                [KeyType] = isRegister ? KeyValueRegister : KeyValueUnregister,
                [KeyAddress] = address,
                [KeyHeaders] = headers?.getAsJSON()
            };
            this.WriteFrame(sendObject);
        }

        bool WriteFrame(JObject sendObject)
        {
            Log.InfoFormat("WriteFrame : {0}", sendObject.ToString());
            if (!this.SocketReady.WaitOne(TimeSpan.FromSeconds(10)))
            {
                Log.ErrorFormat("Socket Not ready for address:{0}", address);
                return false;
            }
            lock (this.writeLock)
            {
                var stream = this.SocketStream;
                byte[] bytes = encoding.GetBytes(sendObject.ToString());
                SocketUtils.WriteInt(stream, bytes.Length);
                stream.Write(bytes, 0, bytes.Length);
                return true;
            }
        }

    }

}
