using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Logging;

namespace Dropbox.Api
{
    public interface IDropboxApi
    {
        Task<AuthorizationToken> AcquireToken(string code, string appKey, string appSecret, CancellationToken cancellationToken);
        Task<Metadata> Metadata(string path, string accessToken, CancellationToken cancellationToken, ILogger logger);
        Task Delete(string path, string accessToken, CancellationToken cancellationToken, ILogger logger);
        Task<MediaResult> Media(string path, string accessToken, CancellationToken cancellationToken, ILogger logger);
        Task<DeltaResult> Delta(string cursor, string accessToken, CancellationToken cancellationToken, ILogger logger);
        Task<DeltaResult> FilesInFolder(string folderPath, string accessToken, CancellationToken cancellationToken, ILogger logger);
    }
}
