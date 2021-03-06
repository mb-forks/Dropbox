﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Dropbox.Api;
using Dropbox.Configuration;
using MediaBrowser.Controller.Sync;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Sync;
using MediaBrowser.Controller.Library;

namespace Dropbox
{
    public class DropboxServerSyncProvider : IServerSyncProvider, IHasDynamicAccess, IRemoteSyncProvider
    {
        // 10mb
        private const long StreamBufferSize = 10 * 1024 * 1024;

        private IConfigurationRetriever _configurationRetriever
        {
            get
            {
                return Plugin.Instance.ConfigurationRetriever;
            }
        }

        private IDropboxApi _dropboxApi
        {
            get
            {
                return Plugin.Instance.DropboxApi;
            }
        }

        private IDropboxContentApi _dropboxContentApi
        {
            get
            {
                return Plugin.Instance.DropboxContentApi;
            }
        }

        private readonly ILogger _logger;
        private IUserManager _userManager;

        public DropboxServerSyncProvider(ILogManager logManager, IUserManager userManager)
        {
            _logger = logManager.GetLogger("Dropbox");
            _userManager = userManager;
        }

        public string Name
        {
            get { return Constants.Name; }
        }

        public bool SupportsRemoteSync
        {
            get { return true; }
        }

        public List<SyncTarget> GetAllSyncTargets()
        {
            return _configurationRetriever.GetSyncAccounts().Select(CreateSyncTarget).ToList();
        }

        public List<SyncTarget> GetSyncTargets(long userId)
        {
            var userIdString = _userManager.GetGuid(userId).ToString("N");

            return _configurationRetriever.GetUserSyncAccounts(userIdString).Select(CreateSyncTarget).ToList();
        }

        public async Task<SyncedFileInfo> SendFile(SyncJob syncJob, string originalMediaPath, Stream inputStream, bool isMedia, string[] outputPathParts, SyncTarget target, IProgress<double> progress, CancellationToken cancellationToken)
        {
            var path = GetFullPath(outputPathParts, target);
            _logger.Debug("Sending file {0} to {1}", path, target.Name);

            var syncAccount = _configurationRetriever.GetSyncAccount(target.Id);

            await UploadFile(path, inputStream, progress, syncAccount.AccessToken, cancellationToken).ConfigureAwait(false);

            return new SyncedFileInfo
            {
                Id = path,
                Path = path,
                Protocol = MediaProtocol.Http
            };
        }

        public string GetFullPath(IEnumerable<string> path, SyncTarget target)
        {
            return "/" + string.Join("/", path);
        }

        public async Task<SyncedFileInfo> GetSyncedFileInfo(string id, SyncTarget target, CancellationToken cancellationToken)
        {
            _logger.Debug("Getting synced file info for {0} from {1}", id, target.Name);

            try
            {
                return await TryGetSyncedFileInfo(id, target, cancellationToken).ConfigureAwait(false);
            }
            catch (HttpException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new FileNotFoundException("File not found", ex);
                }

                throw;
            }
        }

        public async Task<bool> DeleteFile(SyncJob syncJob, string path, SyncTarget target, CancellationToken cancellationToken)
        {
            try
            {
                var syncAccount = _configurationRetriever.GetSyncAccount(target.Id);
                await _dropboxApi.Delete(path, syncAccount.AccessToken, cancellationToken, _logger).ConfigureAwait(false);
                return true;
            }
            catch (HttpException ex)
            {
                _logger.ErrorException("FolderSync: Error removing {0} from {1}.", ex, path, target.Name);
                return false;
            }
        }

        public Task<Stream> GetFile(string id, SyncTarget target, IProgress<double> progress, CancellationToken cancellationToken)
        {
            try
            {
                var syncAccount = _configurationRetriever.GetSyncAccount(target.Id);
                return _dropboxContentApi.Files(id, syncAccount.AccessToken, cancellationToken, _logger);
            }
            catch (HttpException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new FileNotFoundException("File not found", ex);
                }

                throw;
            }
        }

        public Task<QueryResult<FileSystemMetadata>> GetFiles(string[] directoryPathParts, SyncTarget target, CancellationToken cancellationToken)
        {
            var syncAccount = _configurationRetriever.GetSyncAccount(target.Id);
            var path = FindPathFromFileQuery(directoryPathParts, target);

            return FilesInFolder(path, syncAccount.AccessToken, cancellationToken);
        }

        public Task<QueryResult<FileSystemMetadata>> GetFiles(SyncTarget target, CancellationToken cancellationToken)
        {
            var syncAccount = _configurationRetriever.GetSyncAccount(target.Id);
            return FindFilesMetadata(syncAccount.AccessToken, cancellationToken);
        }

        private SyncTarget CreateSyncTarget(DropboxSyncAccount syncAccount)
        {
            return new SyncTarget
            {
                Id = syncAccount.Id,
                Name = syncAccount.Name
            };
        }

        private async Task UploadFile(string path, Stream stream, IProgress<double> progress, string accessToken, CancellationToken cancellationToken)
        {
            progress.Report(0.0);

            string session_id = null;
            var streamLength = stream.Length;
            long offset = 0;
            stream.Position = 0;

            ////var buffer = await FillBuffer(stream, cancellationToken).ConfigureAwait(false);

            var chunkSize = Math.Min(streamLength, StreamBufferSize);
            
            var sectionStream = new SubSectionStream(stream, 0, chunkSize);

            if (chunkSize > 0)
            {
                var result = await _dropboxContentApi.ChunkedUpload_Start(sectionStream, accessToken, cancellationToken, _logger).ConfigureAwait(false);
                session_id = result.session_id;
                offset += chunkSize;
                progress.Report((double)offset / streamLength * 100);
            }

            while (offset < streamLength)
            {
                chunkSize = Math.Min(streamLength - offset, StreamBufferSize);
                sectionStream = new SubSectionStream(stream, offset, chunkSize);
                await _dropboxContentApi.ChunkedUpload_Append(session_id, sectionStream, offset, accessToken, cancellationToken, _logger).ConfigureAwait(false);
                offset += chunkSize;
                progress.Report((double)offset / streamLength * 100);
            }

            if (offset > 0)
            {
                await _dropboxContentApi.ChunkedUpload_Commit(path, session_id, offset, accessToken, cancellationToken, _logger).ConfigureAwait(false);
            }
        }

        private async Task<SyncedFileInfo> TryGetSyncedFileInfo(string id, SyncTarget target, CancellationToken cancellationToken)
        {
            var syncAccount = _configurationRetriever.GetSyncAccount(target.Id);

            var shareResult = await _dropboxApi.Media(id, syncAccount.AccessToken, cancellationToken, _logger).ConfigureAwait(false);

            return new SyncedFileInfo
            {
                Path = shareResult.link,
                Protocol = MediaProtocol.Http,
                Id = id
            };
        }

        private string FindPathFromFileQuery(string[] parts, SyncTarget target)
        {
            if (parts != null && parts.Length > 0)
            {
                return GetFullPath(parts, target);
            }

            return string.Empty;
        }

        ////private async Task<QueryResult<FileSystemMetadata>> FindFileMetadata(string path, string accessToken, CancellationToken cancellationToken)
        ////{
        ////    try
        ////    {
        ////        var metadata = await _dropboxApi.Metadata(path, accessToken, cancellationToken, _logger);
        ////        return new QueryResult<FileSystemMetadata>
        ////        {
        ////            Items = new[] { CreateFileMetadata(metadata) },
        ////            TotalRecordCount = 1
        ////        };
        ////    }
        ////    catch (HttpException ex)
        ////    {
        ////        if (ex.StatusCode == HttpStatusCode.Conflict)
        ////        {
        ////            _logger.Debug("No Data, maybe a 409");
        ////            return new QueryResult<FileSystemMetadata>();
        ////        }

        ////        throw;
        ////    }
        ////}

        private async Task<QueryResult<FileSystemMetadata>> FindFilesMetadata(string accessToken, CancellationToken cancellationToken)
        {
            var files = new List<FileSystemMetadata>();
            var deltaResult = new DeltaResult { has_more = true };

            while (deltaResult.has_more)
            {
                deltaResult = await _dropboxApi.Delta(deltaResult.cursor, accessToken, cancellationToken, _logger).ConfigureAwait(false);

                var newFiles = deltaResult.entries
                    .Where(e => e.id != null)
                    .Select(CreateFileMetadata);

                files.AddRange(newFiles);
            }

            return new QueryResult<FileSystemMetadata>
            {
                Items = files.ToArray(),
                TotalRecordCount = files.Count
            };
        }

        private async Task<QueryResult<FileSystemMetadata>> FilesInFolder(string folder, string accessToken, CancellationToken cancellationToken)
        {
            var files = new List<FileSystemMetadata>();
            var deltaResult = new DeltaResult { has_more = true };

            while (deltaResult.has_more)
            {
                deltaResult = await _dropboxApi.FilesInFolder(folder, accessToken, cancellationToken, _logger).ConfigureAwait(false);

                var newFiles = deltaResult.entries
                    .Where(e => e.id != null)
                    .Select(CreateFileMetadata)
                    .Where(fsi => !fsi.IsDirectory);

                files.AddRange(newFiles);
            }

            return new QueryResult<FileSystemMetadata>
            {
                Items = files.ToArray(),
                TotalRecordCount = files.Count
            };
        }

        private static FileSystemMetadata CreateFileMetadata(Metadata metadata)
        {
            return new FileSystemMetadata
            {
                FullName = metadata.path_display,
                IsDirectory = (metadata.tag == "folder"),
                //MimeType = metadata.mime_type,
                Name = metadata.path_display.Split('/').Last()
            };
        }
    }
}
