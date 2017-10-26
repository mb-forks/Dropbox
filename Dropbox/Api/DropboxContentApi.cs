using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Serialization;

namespace Dropbox.Api
{
    public class DropboxContentApi : ApiService, IDropboxContentApi
    {
        public DropboxContentApi(IHttpClient httpClient, IJsonSerializer jsonSerializer, IApplicationHost applicationHost)
            : base(httpClient, jsonSerializer, applicationHost)
        { }

        protected override string BaseUrl
        {
            get { return "https://content.dropboxapi.com/2/"; }
        }

        public async Task<ChunkedUpload_Start_Result> ChunkedUpload_Start(byte[] content, string accessToken, CancellationToken cancellationToken)
        {
            var url = "/files/upload_session/start";
            string data_api = "{\"close\": false}";

            return await PostRequest_v2<ChunkedUpload_Start_Result>(url, accessToken, data_api, content, cancellationToken);
        }

        public async Task ChunkedUpload_Append(string session_id, byte[] content, int offset, string accessToken, CancellationToken cancellationToken)
        {
            var url = "/files/upload_session/append_v2";
            string data_api = "{\"cursor\": {\"session_id\": \"" + session_id + "\",\"offset\":" + offset + "},\"close\": false}";

            await PostRequest_v2<object>(url, accessToken, data_api, content, cancellationToken);
        }

        public async Task ChunkedUpload_Commit(string path, string session_id, int offset, string accessToken, CancellationToken cancellationToken)
        {
            var url = "/files/upload_session/finish";

            string data_api = "{\"cursor\": {\"session_id\":\"" + session_id + "\",\"offset\":" + offset + "}, \"commit\", { \"path\":\"" + path + "\", \"mode\":\"overwrite\"}}";

            var result = await PostRequest_v2<object>(url, accessToken, data_api, null, cancellationToken);
        }

        public async Task<Stream> Files(string path, string accessToken, CancellationToken cancellationToken)
        {
            var url = "files/download" + path;
            string data_api = "{\"path\":\"" + path + "\"}";

            return await PostRequest_v2<Stream>(url, accessToken, data_api, null, cancellationToken);
        }
    }
}
