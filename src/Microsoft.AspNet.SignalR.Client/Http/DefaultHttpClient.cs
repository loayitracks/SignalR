// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NET45 || NETSTANDARD2_0

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Http;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace Microsoft.AspNet.SignalR.Client.Http
{
    /// <summary>
    /// The default <see cref="IHttpClient"/> implementation.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1001:Implement IDisposable", Justification = "Response task returned to the caller so cannot dispose Http Client")]
    public class DefaultHttpClient : IHttpClient
    {
        private readonly string _shortRunningGroup;
        private readonly string _longRunningGroup;

        public DefaultHttpClient()
        {
            string id = Guid.NewGuid().ToString();
            _shortRunningGroup = "SignalR-short-running-" + id;
            _longRunningGroup = "SignalR-long-running-" + id;
        }

        private HttpClient _longRunningClient;
        private HttpClient _shortRunningClient;

        private IConnection _connection;

        /// <summary>
        /// Initialize the Http Clients
        /// </summary>
        /// <param name="connection">Connection</param>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Handler cannot be disposed before response is disposed")]
        public void Initialize(IConnection connection)
        {
            string id = Guid.NewGuid().ToString();

            _connection = connection;

            _longRunningClient = new HttpClient(CreateHandler());

            // Disabling the Http Client timeout 
            _longRunningClient.Timeout = TimeSpan.FromMilliseconds(5000);

            _shortRunningClient = new HttpClient(CreateHandler());
            _shortRunningClient.Timeout = TimeSpan.FromMilliseconds(5000);
        }

        protected virtual HttpMessageHandler CreateHandler()
        {
            var handler = new DefaultHttpHandler(_connection);
#if !NET45
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
#endif
            return handler;
        }

        /// <summary>
        /// Makes an asynchronous http GET request to the specified url.
        /// </summary>
        /// <param name="url">The url to send the request to.</param>
        /// <param name="prepareRequest">A callback that initializes the request with default values.</param>
        /// <param name="isLongRunning">Indicates whether the request is long running</param>
        /// <returns>A <see cref="T:Task{IResponse}"/>.</returns>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Handler cannot be disposed before response is disposed")]
        public async Task<IResponse> Get(string url, Action<IRequest> prepareRequest, bool isLongRunning)
        {
            if (prepareRequest == null)
            {
                throw new ArgumentNullException("prepareRequest");
            }

            var responseDisposer = new Disposer();
            var cts = new CancellationTokenSource();

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri(url));

            var request = new HttpRequestMessageWrapper(requestMessage, () =>
            {
                cts.Cancel();
                responseDisposer.Dispose();
            });

            prepareRequest(request);

            var httpClient = GetHttpClient(isLongRunning);

            var res = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cts.Token)
                 .Then(responseMessage =>
                 {
                     if (responseMessage.IsSuccessStatusCode)
                     {
                         responseDisposer.Set(responseMessage);
                     }
                     else
                     {
                         // Dispose the response (https://github.com/SignalR/SignalR/issues/4092)
                         responseMessage.RequestMessage.Dispose();
                         responseMessage.Dispose();

                         // None of the getters on HttpResponseMessage throw ODE, so it should be safe to give the catcher of the exception
                         // access to the response. They may get an ODE if they try to read the body, but that's OK.
                         throw new HttpClientException(responseMessage);
                     }

                     return (IResponse)new HttpResponseMessageWrapper(responseMessage);
                 });

            return res;
        }

        /// <summary>
        /// Makes an asynchronous http GET request to the specified url (Copeid from Microsoft.AspNet.SignalR.Client v 1.2.2)
        /// </summary>
        /// <param name="url">The url to send the request to.</param>
        /// <param name="prepareRequest">A callback that initializes the request with default values.</param>
        /// <param name="isLongRunning">Indicates whether it is a long running request</param>
        /// <returns>A <see cref="T:Task{IResponse}"/>.</returns>
        public Task<IResponse> Get_v122(string url, Action<IRequest> prepareRequest, bool isLongRunning)
        {
            var responseDisposer = new Disposer();
            var cts = new CancellationTokenSource();

            return v122.HttpHelper.GetAsync(url, request =>
            {
                request.ConnectionGroupName = isLongRunning ? _longRunningGroup : _shortRunningGroup;
                var req = new v122.HttpWebRequestWrapper(request, () =>
                {
                    cts.Cancel();
                    responseDisposer.Dispose();
                });
                prepareRequest(req);
            }
            ).Then(response =>
            {
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    responseDisposer.Set(response);
                }
                else
                {
                    // Dispose the response (https://github.com/SignalR/SignalR/issues/4092)
                    response.Dispose();

                    // None of the getters on HttpResponseMessage throw ODE, so it should be safe to give the catcher of the exception
                    // access to the response. They may get an ODE if they try to read the body, but that's OK.
                    throw new HttpRequestException("Error while processing Get of HttpWebRequest " + response.ToString());
                }
                return (IResponse) new v122.HttpWebResponseWrapper(response);
            });
        }

       
        /// <summary>
        /// Makes an asynchronous http POST request to the specified url.
        /// </summary>
        /// <param name="url">The url to send the request to.</param>
        /// <param name="prepareRequest">A callback that initializes the request with default values.</param>
        /// <param name="postData">form url encoded data.</param>
        /// <param name="isLongRunning">Indicates whether the request is long running</param>
        /// <returns>A <see cref="T:Task{IResponse}"/>.</returns>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Handler cannot be disposed before response is disposed")]
        public Task<IResponse> Post(string url, Action<IRequest> prepareRequest, IDictionary<string, string> postData, bool isLongRunning)
        {
            if (prepareRequest == null)
            {
                throw new ArgumentNullException("prepareRequest");
            }

            var responseDisposer = new Disposer();
            var cts = new CancellationTokenSource();

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, new Uri(url));

            if (postData == null)
            {
                requestMessage.Content = new StringContent(String.Empty);
            }
            else
            {
                requestMessage.Content = new ByteArrayContent(HttpHelper.ProcessPostData(postData));
            }

            var request = new HttpRequestMessageWrapper(requestMessage, () =>
            {
                cts.Cancel();
                responseDisposer.Dispose();
            });

            prepareRequest(request);

            var httpClient = GetHttpClient(isLongRunning);

            return httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cts.Token)
                .Then(responseMessage =>
                {
                    if (responseMessage.IsSuccessStatusCode)
                    {
                        responseDisposer.Set(responseMessage);
                    }
                    else
                    {
                        // Dispose the response (https://github.com/SignalR/SignalR/issues/4092)
                        var message = responseMessage.ToString();
                        responseMessage.RequestMessage.Dispose();
                        responseMessage.Dispose();
                        throw new HttpClientException(message);
                    }

                    return (IResponse)new HttpResponseMessageWrapper(responseMessage);
                });
        }

        /// <summary>
        /// Returns the appropriate client based on whether it is a long running request
        /// </summary>
        /// <param name="isLongRunning">Indicates whether the request is long running</param>
        /// <returns></returns>
        private protected virtual HttpClient GetHttpClient(bool isLongRunning)
        {
            return isLongRunning ? _longRunningClient : _shortRunningClient;
        }
    }
}

#elif NET40
// See net40/Http/DefaultHttpClient.cs
#endif
