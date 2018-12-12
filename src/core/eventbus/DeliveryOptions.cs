using System;
using System.Collections.Generic;
using System.Text;

namespace io.vertx.core.eventbus
{
    public class DeliveryOptions
    {
        public const long DefaultTimeout = 1 * 1000;
        
        public long Timeout { get; set; }

        public DeliveryOptions()
        {
            this.Timeout = DefaultTimeout;
        }

    }
}
