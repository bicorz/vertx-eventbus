using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace IO.Vertx.Core.eventbus
{
    public class Headers
    {

        private readonly Hashtable headers = new Hashtable();

        public int Count => headers.Count;

        public Headers()
        {

        }

        public void Add(string headerName, string headerValue)
        {
            headers.Add(headerName, headerValue);
        }

        public JObject getAsJSON()
        {
            JObject headersJson = new JObject();
            foreach (var headerKey in headers.Keys)
            {
                var headerValue = headers[headerKey];
                headersJson.Add((string)headerKey, (string) headerValue);
            }
            return headersJson;
        }

    }
}
