using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Dropbox.Api
{
    public struct DeltaEntry
    {
        public MetadataResult Metadata { get; set; }

        // TODO: find how to use ServiceStack to deserialize this 2-items array of different types (Json.NET can be removed after that)
        public static DeltaEntry Parse(string json)
        {
            return new DeltaEntry
            {
                Metadata = MetadataResult.json(JsonConvert.DeserializeObject<Dictionary<string, string>>(json))
            };
        }
    }
}
