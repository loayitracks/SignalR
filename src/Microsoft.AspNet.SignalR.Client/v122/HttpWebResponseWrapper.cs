using System;
using System.IO;
using System.Net;
using Microsoft.AspNet.SignalR.Client.Http;

#if !NET40 && !NETSTANDARD1_3

namespace Microsoft.AspNet.SignalR.Client.v122
{
    public class HttpWebResponseWrapper : IResponse
    {
        private readonly HttpWebResponse _response;

        public HttpWebResponseWrapper(HttpWebResponse response)
        {
            _response = response;
        }

        public Stream GetStream()
        {
            return _response.GetResponseStream();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                ((IDisposable)_response).Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}

#endif