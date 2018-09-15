using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Logging;

namespace Dropbox.Api
{
    public interface IDropboxContentApi
    {
        Task<ChunkedUpload_Start_Result> ChunkedUpload_Start(Stream stream, string accessToken, CancellationToken cancellationToken, ILogger logger);
        Task ChunkedUpload_Append(string session_id, Stream stream, long offset, string accessToken, CancellationToken cancellationToken, ILogger logger);
        Task ChunkedUpload_Commit(string path, string session_id, long offset, string accessToken, CancellationToken cancellationToken, ILogger logger);

        Task<Stream> Files(string path, string accessToken, CancellationToken cancellationToken, ILogger logger);
    }
}
