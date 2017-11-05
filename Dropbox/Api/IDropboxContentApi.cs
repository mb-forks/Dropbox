using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Logging;

namespace Dropbox.Api
{
    public interface IDropboxContentApi
    {
        Task<ChunkedUpload_Start_Result> ChunkedUpload_Start(byte[] content, string accessToken, CancellationToken cancellationToken, ILogger logger);
        Task ChunkedUpload_Append(string session_id, byte[] content, int offset, string accessToken, CancellationToken cancellationToken, ILogger logger);
        Task ChunkedUpload_Commit(string path, string session_id, int offset, string accessToken, CancellationToken cancellationToken, ILogger logger);

        Task<Stream> Files(string path, string accessToken, CancellationToken cancellationToken, ILogger logger);
    }
}
