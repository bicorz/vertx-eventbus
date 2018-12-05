using IO.Vertx.Core.eventbus;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace io.vertx.core.eventbus
{
    public class DeliveryOptions
    {

        public long SendTimeout { get; set; }
        public readonly Headers headers = new Headers();

    }
}
