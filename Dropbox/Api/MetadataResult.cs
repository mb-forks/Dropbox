using System.Collections.Generic;

namespace Dropbox.Api
{
    public class MetadataResult
    {
        public string path_display { get; set; }
        public string tag { get; set; }

        public static MetadataResult json(Dictionary<string, string> json)
        {
            return new MetadataResult
            {
                path_display = json["path_display"],
                tag = json[".tag"]
            };
        }
    }
}
