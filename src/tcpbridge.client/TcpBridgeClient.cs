using io.vertx.core;
using io.vertx.core.eventbus;
using io.vertx.core.eventbus.impl;
using io.vertx.core.logging;
using io.vertx.core.net;
using log4net;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        IMessageConsumer<T> Register<T>(string address, Action<IMessage<T>> handler);
        void UnRegister<T>(IMessageConsumer<T> consumer);

        void Send(string address, object body);
        void Send<T>(string address, object body, DeliveryOptions deliveryOptions, Action<IAsyncResult<IMessage<T>>> replyHandle);

        void Publish(string address, object body);
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
        private readonly IEventBus eventBus;

        private readonly ManualResetEvent SocketReady;
        private readonly ManualResetEvent IsClosed;
        private readonly Thread workerThread;

        private readonly ConcurrentDictionary<string, IMessageConsumer<object>> addressRegistry;

        private TcpClient Client { get; set; }
        private Stream SocketStream { get; set; }
        private bool IsSocketClosed { get; set; }

        private readonly object writeLock = new object();
        private readonly UTF8Encoding encoding = new UTF8Encoding();

        private delegate void Action();

        //proxy constructors
        public TcpBridgeClient(IEventBus eventBus, string address, int port) : this(eventBus, address, port, null) { }
        public TcpBridgeClient(string address, int port) : this(new EventBus(), address, port, null) { }
        public TcpBridgeClient(string address, int port, BridgeClientOption option) : this(new EventBus(), address, port, option) { }

        //Main constructor. if vertx provided, it will use that's eventbus. otherwise it will use its own eventbus.
        public TcpBridgeClient(IEventBus eventBus, string address, int port, BridgeClientOption option)
        {
            this.option = new BridgeClientOption(option);

            this.address = address;
            this.port = port;
            this.eventBus = eventBus;

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
                if (this.IsClosed.WaitOne(0))
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
                        catch (Exception e)
                        {
                            Log.ErrorFormat("Exception while handling bridge object {0} {1}", payload, e);
                        }
                    }
                    catch (Exception e)
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
            Log.InfoFormat("Read Frame: {0}", payload);
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
                        Message<object> message = new Message<object>(this.eventBus, address, body, replyAddress, payloadType != KeyValuePublish);
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
            if (message.IsSend)
            {
                this.eventBus.Send(message.Address, message.Body);
            }
            else
            {
                this.eventBus.Publish(message.Address, message.Body);
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

        public IMessageConsumer<T> Register<T>(string address, Action<IMessage<T>> handler)
        {
            IMessageConsumer<T> toReturn = null;
            lock (addressRegistry)
            {
                if(addressRegistry.ContainsKey(address))
                {
                    throw new Exception($"already an action registered for address:{address}");
                }
                toReturn = this.eventBus.Consumer<T>(address, handler);
                addressRegistry[address] = (IMessageConsumer<object>) toReturn;
            }
            this.WriteRegisterOrRemove(true, address);
            return toReturn;
        }

        public void UnRegister<T>(IMessageConsumer<T> consumer)
        {
            bool found = false;
            lock (addressRegistry)
            {
                consumer.UnRegister();
                found = addressRegistry.TryRemove(consumer.Address, out _);
            }
            if(found)
            {
                this.WriteRegisterOrRemove(false, consumer.Address);
            }
        }

        public void Send(string address, object body)
        {
            this.Send<object>(address, body, null, null);
        }

        public void Publish(string address, object body)
        {
            this.Publish(address, body, null);
        }

        public void Send<T>(string address, object body, DeliveryOptions deliveryOptions, Action<IAsyncResult<IMessage<T>>> replyHandle)
        {
            Message<object> message = new Message<object>(this.eventBus, address, body, null, true);
            this.WriteSendOrPublish(message);
        }

        public void Publish(string address, object body, DeliveryOptions deliveryOptions)
        {
            Message<object> message = new Message<object>(this.eventBus, address, body, null, true);
            this.WriteSendOrPublish(message);
        }

        private bool WriteSendOrPublish(Message<object> message)
        {
            JObject sendObject = new JObject
            {
                [KeyType] = message.IsSend ? KeyValueSend : KeyValuePublish,
                [KeyAddress] = message.Address,
                [KeyBody] = (JObject)message.Body,
                [KeyReplayAddress] = message.ReplyAddress,
                //[KeyHeaders] = message.Headers?.getAsJSON()
            };
            return this.WriteFrame(sendObject);
        }

        private bool WriteRegisterOrRemove(bool isRegister, string address)
        {
            JObject sendObject = new JObject
            {
                [KeyType] = isRegister ? KeyValueRegister : KeyValueUnregister,
                [KeyAddress] = address,
                //[KeyHeaders] = headers?.getAsJSON()
            };
            return this.WriteFrame(sendObject);
        }

        private bool WriteFrame(JObject sendObject)
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
