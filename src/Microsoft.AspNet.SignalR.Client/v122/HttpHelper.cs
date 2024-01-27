using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;

using System.Text;
using System.Threading.Tasks;

#if !NET40 && !NETSTANDARD1_3

using System.Net.Http;
namespace Microsoft.AspNet.SignalR.Client.v122
{
	public static class HttpHelper
	{
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are flowed back to the caller.")]
        public static Task<HttpWebResponse> GetHttpResponseAsync(this HttpWebRequest request)
        {
            try
            {
                return Task.Factory.FromAsync<HttpWebResponse>(request.BeginGetResponse, ar => (HttpWebResponse)request.EndGetResponse(ar), null);
            }
            catch (Exception ex)
            {
                return TaskAsyncHelper.FromError<HttpWebResponse>(ex);
            }
        }

        public static Task<HttpWebResponse> GetAsync(string url, Action<HttpWebRequest> requestPreparer)
        {
            HttpWebRequest request = CreateWebRequest(url);
            if (requestPreparer != null)
            {
                requestPreparer(request);
            }
            return request.GetHttpResponseAsync();
        }

        private static HttpWebRequest CreateWebRequest(string url)
        {
            HttpWebRequest request = null;
            request = (HttpWebRequest)WebRequest.Create(url);
            return request;
        }

        public static HttpResponseMessage ConvertToHttpResponseMessage(HttpWebResponse httpWebRespons)
        {
            using (var responseApi = httpWebRespons)
            {
                var response = new HttpResponseMessage(responseApi.StatusCode);
                using (var reader = new StreamReader(responseApi.GetResponseStream()))
                {
                    var objText = reader.ReadToEnd();
                    response.Content = new StringContent(objText, Encoding.UTF8, "application/json");
                }
                return response;
            }
        }
    }
}

#endif