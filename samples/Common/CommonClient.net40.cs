// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NET40

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Hubs;

namespace Microsoft.AspNet.SignalR.Client.Samples
{
    public class CommonClient
    {
        private TextWriter _traceWriter;

        public CommonClient(TextWriter traceWriter)
        {
            _traceWriter = traceWriter;
        }

        public void Run(string url)
        {
            try
            {
                RunHubConnectionAPI(url);
            }
            catch (Exception exception)
            {
                _traceWriter.WriteLine("Exception: {0}", exception);
                throw;
            }
        }       

        private void RunHubConnectionAPI(string url)
        {
        }

        private void RunDemo(string url)
        {
        }

        private void RunRawConnection(string serverUrl)
        {
        }


        private void RunStreaming(string serverUrl)
        {
        }

        private void RunAuth(string serverUrl)
        {
        }

        private void RunWindowsAuth(string url)
        {
        }

        private void RunHeaderAuthHub(string url)
        {
        }   
    }    
}

#endif
