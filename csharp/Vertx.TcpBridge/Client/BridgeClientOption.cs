using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Text;

namespace io.vertx.ext.tcpbridge.client
{
    public class BridgeClientOption
    {
        public bool IsSSL {get; set;}
        public int ReconnetDelayInSec { get; set; }
        public long DefaultTimeout { get; set; }

        public RemoteCertificateValidationCallback CertificateValidationCallback { get; set; }
        public LocalCertificateSelectionCallback CertificateSelectionCallback { get; set; }

        public BridgeClientOption()
        {
            this.IsSSL = false;
            this.ReconnetDelayInSec = 10;
        }

        public BridgeClientOption(BridgeClientOption option)
        {
            this.IsSSL = option == null ? false : option.IsSSL;
            this.ReconnetDelayInSec = option == null ? 10 : option.ReconnetDelayInSec;
            this.DefaultTimeout = option == null ? (long) TimeSpan.FromSeconds(10).TotalMilliseconds : option.DefaultTimeout;

            this.CertificateValidationCallback = option?.CertificateValidationCallback;
            this.CertificateSelectionCallback = option?.CertificateSelectionCallback;
        }
    }
}
