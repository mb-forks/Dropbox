﻿using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Logging;

namespace Dropbox.Api
{
    public class DropboxContentApi : ApiService, IDropboxContentApi
    {
        public DropboxContentApi(IHttpClient httpClient, IJsonSerializer jsonSerializer, IApplicationHost applicationHost)
            : base(httpClient, jsonSerializer, applicationHost)
        { }

        protected override string BaseUrl
        {
            get { return "https://content.dropboxapi.com/"; }
        }

        public Task<ChunkedUpload_Start_Result> ChunkedUpload_Start(Stream stream, string accessToken, CancellationToken cancellationToken, ILogger logger)
        {
            const string url = "/2/files/upload_session/start";
            const string data_api = "{\"close\": false}";

            return PostRequestStream<ChunkedUpload_Start_Result>(url, accessToken, data_api, stream, cancellationToken, logger);
        }

        public Task ChunkedUpload_Append(string session_id, Stream stream, long offset, string accessToken, CancellationToken cancellationToken, ILogger logger)
        {
            const string url = "/2/files/upload_session/append_v2";
            string data_api = "{\"cursor\": {\"session_id\": \"" + session_id + "\",\"offset\":" + offset + "},\"close\": false}";

            return PostRequestStream<object>(url, accessToken, data_api, stream, cancellationToken, logger);
        }

        public async Task ChunkedUpload_Commit(string path, string session_id, long offset, string accessToken, CancellationToken cancellationToken, ILogger logger)
        {
            const string url = "/2/files/upload_session/finish";

            string data_api = "{\"cursor\": {\"session_id\":\"" + session_id + "\",\"offset\":" + offset + "}, \"commit\": { \"path\":\"" + path + "\", \"mode\":\"overwrite\"}}";

            var result = await PostRequest_v2<object>(url, accessToken, data_api, "_download", null, cancellationToken, logger).ConfigureAwait(false);
        }

        public Task<Stream> Files(string path, string accessToken, CancellationToken cancellationToken, ILogger logger)
        {
            const string url = "/2/files/download";
            string data_api = "{\"path\":\"" + path + "\"}";

            return GetRawRequest(url, accessToken, data_api, cancellationToken, logger);
        }
    }
}
