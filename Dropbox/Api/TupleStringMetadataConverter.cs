using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dropbox.Api
{
    public class TupleStringMetadataConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JArray jarray = JArray.Load(reader);
            var item1 = jarray[0].ToObject<MetadataResult>();
            //var item2 = jarray[1].ToString();
            //var item3 = jarray[2].ToString();
            //var item4 = jarray[3].ToString();

            return new List<Tuple<MetadataResult>>//, string, string, string>>
            {
                new Tuple<MetadataResult>(item1)//, item2, item3, item4)
            };
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(List<Tuple<MetadataResult /*, string, string, string*/>>).IsAssignableFrom(objectType);
        }
    }
}
