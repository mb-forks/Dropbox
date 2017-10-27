using System.Collections.Generic;
using System.IO;
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
        private const int TimeoutInMilliseconds = 60 * 60 * 1000;

        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IApplicationHost _applicationHost;

        private string UserAgent
        {
            get
            {
                var version = _applicationHost.ApplicationVersion.ToString();
                return string.Format("Emby/{0} +http://emby.media/", version);
            }
        }

        protected abstract string BaseUrl { get; }

        protected ApiService(IHttpClient httpClient, IJsonSerializer jsonSerializer, IApplicationHost applicationHost)
        {
            _httpClient = httpClient;
            _jsonSerializer = jsonSerializer;
            _applicationHost = applicationHost;
        }

        protected async Task<T> PostRequest<T>(string url, string accessToken, IDictionary<string, string> data, CancellationToken cancellationToken)
        {
            var httpRequest = PrepareHttpRequestOptions(url, accessToken, cancellationToken);
            httpRequest.SetPostData(data);
            var result = await _httpClient.Post(httpRequest);
            return _jsonSerializer.DeserializeFromStream<T>(result.Content);
        }

        protected async Task<T> PostRequest_v2<T>(string url, string accessToken, string data_api, string content, byte[] content_byte, CancellationToken cancellationToken, ILogger logger)
        {
            var httpRequest = PrepareHttpRequestOptions(url, accessToken, cancellationToken);
            if (!string.IsNullOrEmpty(data_api))
            {
                httpRequest.RequestHeaders["Dropbox-API-Arg"] = data_api;
                logger.Debug("Dropbox-API-Arg: " + data_api);
            }

            httpRequest.TimeoutMs = TimeoutInMilliseconds;
            httpRequest.RequestContentType = "application/json";

            if (!string.IsNullOrEmpty(content))
            {
                if (content != "_download")
                {
                    httpRequest.RequestContent = content;
                    logger.Debug("Content: " + content);
                }
                else
                {
                    httpRequest.RequestContentType = "application/octet-stream";
                }
            }
            else
            {
                if (content_byte != null && content_byte.Length > 0)
                {
                    httpRequest.RequestContentType = "application/octet-stream";
                    httpRequest.RequestContentBytes = content_byte;
                    logger.Debug("Content: Binary (" + content_byte.Length.ToString() + ") bytes");
                }
            }

            logger.Debug("Send httpRequest");
            var result = await _httpClient.Post(httpRequest);
            logger.Debug("Received HttpResponseInfo");

            return _jsonSerializer.DeserializeFromStream<T>(result.Content);
        }

        private HttpRequestOptions PrepareHttpRequestOptions(string url, string accessToken, CancellationToken cancellationToken)
        {
            var httpRequestOptions = new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = BaseUrl + url.TrimStart('/'),
                UserAgent = UserAgent
            };

            if (!string.IsNullOrEmpty(accessToken))
            {
                httpRequestOptions.RequestHeaders["Authorization"] = "Bearer " + accessToken;
            }

            return httpRequestOptions;
        }
    }
}
