using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Logging;

namespace Dropbox.Api
{
    public abstract class ApiService
    {
        // 1 hour
        ////private const int TimeoutInMilliseconds = 60 * 60 * 1000;

        private readonly HttpClient _httpClient;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IApplicationHost _applicationHost;

        protected ApiService(IHttpClient httpClient, IJsonSerializer jsonSerializer, IApplicationHost applicationHost)
        {
            _httpClient = new HttpClient();
            _jsonSerializer = jsonSerializer;
            _applicationHost = applicationHost;
        }

        private string UserAgent
        {
            get
            {
                var version = _applicationHost.ApplicationVersion.ToString();
                return string.Format("Emby/{0}", version);
            }
        }

        protected abstract string BaseUrl { get; }

        protected async Task<T> PostRequest<T>(string url, string accessToken, IDictionary<string, string> data, CancellationToken cancellationToken)
        {
            var requestUrl = this.PrepareRequestUrl(url);

            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUrl))
            {
                if (!string.IsNullOrEmpty(accessToken))
                {
                    requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                }

                requestMessage.Headers.Add("User-Agent", UserAgent);
                requestMessage.Content = new FormUrlEncodedContent(data);
                var result = await _httpClient.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
                var responseStream = await result.Content.ReadAsStreamAsync().ConfigureAwait(false);
                return await _jsonSerializer.DeserializeFromStreamAsync<T>(responseStream).ConfigureAwait(false);
            }
        }

        protected async Task<T> PostRequest_v2<T>(string url, string accessToken, string data_api, string content, byte[] content_byte, CancellationToken cancellationToken, ILogger logger)
        {
            var requestUrl = this.PrepareRequestUrl(url);
            HttpContent httpContent = null;

            if (!string.IsNullOrEmpty(content))
            {
                if (content != "_download")
                {
                    httpContent = new StringContent(content, Encoding.UTF8, "application/json");
                    logger.Debug("Content: " + content);
                }
            }
            else
            {
                if (content_byte != null && content_byte.Length > 0)
                {
                    httpContent = new ByteArrayContent(content_byte);
                    httpContent.Headers.Remove("Content-Type");
                    httpContent.Headers.Add("Content-Type", "application/octet-stream");
                    logger.Debug("Content: Binary (" + content_byte.Length.ToString() + ") bytes");
                }
            }

            if (httpContent == null)
            {
                httpContent = new ByteArrayContent(new byte[] { });
                httpContent.Headers.Remove("Content-Type");
                httpContent.Headers.Add("Content-Type", "application/octet-stream");
            }

            logger.Debug("Send httpRequest");

            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUrl))
            {
                if (!string.IsNullOrEmpty(accessToken))
                {
                    requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                }

                if (!string.IsNullOrEmpty(data_api))
                {
                    requestMessage.Headers.Add("Dropbox-API-Arg", data_api);
                }

                requestMessage.Headers.Add("User-Agent", UserAgent);
                requestMessage.Content = httpContent;

                var response = await _httpClient.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);

                response.EnsureSuccessStatusCode();

                logger.Debug("Received HttpResponseInfo");

                var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

                // Workaround for ServiceStack being unable to deserialize property names containing a leading dot
                if (typeof(T) == typeof(DeltaResult))
                {

                    using (var reader = new StreamReader(responseStream))
                    {
                        var json = await reader.ReadToEndAsync().ConfigureAwait(false);

                        json = json.Replace("\".tag\":", "\"tag\":");
                        return _jsonSerializer.DeserializeFromString<T>(json);
                    }
                }

                return await _jsonSerializer.DeserializeFromStreamAsync<T>(responseStream).ConfigureAwait(false);
            }
        }

        protected async Task<T> PostRequestStream<T>(string url, string accessToken, string data_api, Stream stream, CancellationToken cancellationToken, ILogger logger)
        {
            var requestUrl = this.PrepareRequestUrl(url);

            var httpContent = new StreamContent(stream);
            httpContent.Headers.Remove("Content-Type");
            httpContent.Headers.Add("Content-Type", "application/octet-stream");

            logger.Debug("Send httpRequest");

            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUrl))
            {
                if (!string.IsNullOrEmpty(accessToken))
                {
                    requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                }

                if (!string.IsNullOrEmpty(data_api))
                {
                    requestMessage.Headers.Add("Dropbox-API-Arg", data_api);
                }

                requestMessage.Headers.Add("User-Agent", UserAgent);
                requestMessage.Content = httpContent;

                var response = await _httpClient.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);

                response.EnsureSuccessStatusCode();

                logger.Debug("Received HttpResponseInfo");

                var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

                return await _jsonSerializer.DeserializeFromStreamAsync<T>(responseStream).ConfigureAwait(false);
            }
        }

        protected async Task<Stream> GetRawRequest(string url, string accessToken, string data_api, CancellationToken cancellationToken, ILogger logger)
        {
            var requestUrl = this.PrepareRequestUrl(url);

            logger.Debug("Send httpRequest");
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUrl))
            {
                if (!string.IsNullOrEmpty(accessToken))
                {
                    requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                }

                if (!string.IsNullOrEmpty(data_api))
                {
                    requestMessage.Headers.Add("Dropbox-API-Arg", data_api);
                }

                requestMessage.Headers.Add("User-Agent", UserAgent);
                var result = await _httpClient.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
                return await result.Content.ReadAsStreamAsync().ConfigureAwait(false);
            }
        }

        private string PrepareRequestUrl(string url)
        {
            return BaseUrl + url.TrimStart('/');
        }

        ////private HttpRequestOptions PrepareHttpRequestOptions(string url, string accessToken, CancellationToken cancellationToken)
        ////{
        ////    var httpRequestOptions = new HttpRequestOptions
        ////    {
        ////        CancellationToken = cancellationToken,
        ////        Url = BaseUrl + url.TrimStart('/'),
        ////        UserAgent = UserAgent
        ////    };

        ////    if (!string.IsNullOrEmpty(accessToken))
        ////    {
        ////        httpRequestOptions.RequestHeaders["Authorization"] = "Bearer " + accessToken;
        ////    }

        ////    return httpRequestOptions;
        ////}
    }
}
