using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Dropbox.Api
{
    public struct DeltaEntry
    {
        public MetadataResult Metadata { get; set; }
        public string Property_Groups { get; set; }
        public string Shared { get; set; }
        public string Hash { get; set; }

        // TODO: find how to use ServiceStack to deserialize this 2-items array of different types (Json.NET can be removed after that)
        public static DeltaEntry Parse(string json)
        {
            JsonConvert.DefaultSettings = () =>
            {
                var settings = new JsonSerializerSettings();
                settings.Converters.Add(new TupleStringMetadataConverter());
                return settings;
            };

            var tuple = JsonConvert.DeserializeObject<List<Tuple<MetadataResult, string, string, string>>>(json);

            return new DeltaEntry
            {
                Metadata = tuple[0].Item1,
                Property_Groups = tuple[0].Item2,
                Shared = tuple[0].Item2,
                Hash = tuple[0].Item2
            };
        }
    }
}
