using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Serialization;

namespace Dropbox.Api
{
    public class DropboxApi : ApiService, IDropboxApi
    {
        public DropboxApi(IHttpClient httpClient, IJsonSerializer jsonSerializer, IApplicationHost applicationHost)
            : base(httpClient, jsonSerializer, applicationHost)
        { }

        protected override string BaseUrl
        {
            get { return "https://api.dropboxapi.com/2/"; }
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

        public async Task<MetadataResult> Metadata(string path, string accessToken, CancellationToken cancellationToken)
        {
            var url = "/files/get_metadata";
            var data = new Dictionary<string, string>();
            data["path"] = path;
            data["include_deleted"] = false;

            return await PostRequest<MetadataResult>(url, accessToken, cancellationToken);
        }

        public async Task Delete(string path, string accessToken, CancellationToken cancellationToken)
        {
            var data = new Dictionary<string, string>();
            data["path"] = path;

            await PostRequest<object>("/file/delete_v2", accessToken, data, cancellationToken);
        }

        public async Task<MediaResult> Media(string path, string accessToken, CancellationToken cancellationToken)
        {
            var url = "/files/get_temporary_link";

            var data = new Dictionary<string,string>();
            data["path"] = path;

            return await PostRequest<MediaResult>(url, accessToken, data, cancellationToken);
        }

        public async Task<DeltaResult> Delta(string cursor, string accessToken, CancellationToken cancellationToken)
        {
            var data = new Dictionary<string, string>();
            string url = "/files/list_folder";

            if (!string.IsNullOrEmpty(cursor))
            {
                data["cursor"] = cursor;
                url = "/files/list_folder/continue";
            }

            return await PostRequest<DeltaResult>(url, accessToken, data, cancellationToken);
        }
    }
}
