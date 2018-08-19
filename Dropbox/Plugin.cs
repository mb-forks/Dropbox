using System;
using System.Collections.Generic;
using Dropbox.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Common.Net;
using Dropbox.Api;
using MediaBrowser.Common;

namespace Dropbox
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        public static Plugin Instance { get; private set; }
        public readonly IConfigurationRetriever ConfigurationRetriever = new ConfigurationRetriever();
        public readonly IDropboxApi DropboxApi;
        public readonly IDropboxContentApi DropboxContentApi;

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, IHttpClient httpClient, IJsonSerializer jsonSerializer, IApplicationHost applicationHost)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
            DropboxApi = new DropboxApi(httpClient, jsonSerializer, applicationHost);
            DropboxContentApi = new DropboxContentApi(httpClient, jsonSerializer, applicationHost);
        }

        private readonly Guid _id = new Guid("830fc68f-b964-4d2f-b139-48e22cd143c7");

        public override Guid Id
        {
            get { return _id; }
        }

        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = Name,
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.configPage.html"
                }
            };
        }

        public override string Name
        {
            get { return Constants.Name; }
        }

        public override string Description
        {
            get { return Constants.Description; }
        }
    }
}
