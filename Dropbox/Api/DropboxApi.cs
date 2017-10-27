using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Logging;

namespace Dropbox.Api
{
    public class DropboxApi : ApiService, IDropboxApi
    {
        public DropboxApi(IHttpClient httpClient, IJsonSerializer jsonSerializer, IApplicationHost applicationHost)
            : base(httpClient, jsonSerializer, applicationHost)
        { }

        protected override string BaseUrl
        {
            get { return "https://api.dropboxapi.com/"; }
        }

        public async Task<AuthorizationToken> AcquireToken(string code, string appKey, string appSecret, CancellationToken cancellationToken)
        {
            var data = new Dictionary<string, string>
            {
                { "code", code },
                { "grant_type", "authorization_code" },
                { "client_id", appKey },
                { "client_secret", appSecret }
            };

            return await PostRequest<AuthorizationToken>("/oauth2/token", null, data, cancellationToken);
        }

        public async Task<MetadataResult> Metadata(string path, string accessToken, CancellationToken cancellationToken, ILogger logger)
        {
            var url = "/2/files/get_metadata";
            string data = "{\"path\":\"" + path + "\", \"include_deleted\": false}";

            var result = await PostRequest_v2<MetadataResult>(url, accessToken, null, data, null, cancellationToken, logger);
            return result;
        }

        public async Task Delete(string path, string accessToken, CancellationToken cancellationToken, ILogger logger)
        {
            var url = "/2/file/delete_v2";
            string data = "{\"path\":\"" + path + "\"}";

            await PostRequest_v2<object>(url, accessToken, null, data, null, cancellationToken, logger);
        }

        public async Task<MediaResult> Media(string path, string accessToken, CancellationToken cancellationToken, ILogger logger)
        {
            var url = "/2/files/get_temporary_link";
            string data = "{\"path\":\"" + path + "\"}";

            return await PostRequest_v2<MediaResult>(url, accessToken, null, data, null, cancellationToken, logger);
        }

        public async Task<DeltaResult> Delta(string cursor, string accessToken, CancellationToken cancellationToken, ILogger logger)
        {
            string data = "";
            string url = "/2/files/list_folder";

            if (!string.IsNullOrEmpty(cursor))
            {
                data = "{\"cursor\":\"" + cursor + "\"}";
                url = "/2/files/list_folder/continue";
            }

            return await PostRequest_v2<DeltaResult>(url, accessToken, null, data, null, cancellationToken, logger);
        }
    }
}
