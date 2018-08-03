﻿using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Dropbox.Api;
using Dropbox.Configuration;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Services;

namespace Dropbox.RestServices
{
    [Authenticated]
    public class DropboxRestfulService : IService
    {
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

        public void Delete(DeleteSyncTarget request)
        {
            _configurationRetriever.RemoveSyncAccount(request.Id);
        }

        public async Task Post(AddSyncTarget request)
        {
            var accessToken = await GetAccessToken(request.Code).ConfigureAwait(false);

            var syncAccount = new DropboxSyncAccount
            {
                Id = Guid.NewGuid().ToString(),
                Name = WebUtility.UrlDecode(request.Name),
                EnableForEveryone = request.EnableForEveryone,
                UserIds = request.UserIds,
                AccessToken = accessToken
            };

            if (!string.IsNullOrEmpty(request.Id))
            {
                syncAccount.Id = request.Id;
            }

            _configurationRetriever.AddSyncAccount(syncAccount);
        }

        public DropboxSyncAccount Get(GetSyncTarget request)
        {
            return _configurationRetriever.GetSyncAccount(request.Id);
        }

        private async Task<string> GetAccessToken(string code)
        {
            var config = _configurationRetriever.GetGeneralConfiguration();
            var token = await _dropboxApi.AcquireToken(code, config.DropboxAppKey, config.DropboxAppSecret, CancellationToken.None).ConfigureAwait(false);
            return token.access_token;
        }
    }
}
