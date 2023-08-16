using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Integration.Crm.Raynet.Model.JsonCustomizations
{
    public class IdContainerResolver : JsonConverter<IdContainer>
    {
        public override IdContainer ReadJson(JsonReader reader, Type objectType, IdContainer existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            throw new NotSupportedException();
        }

        public override void WriteJson(JsonWriter writer, IdContainer value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value.Id);
        }

        public override bool CanRead => false;
    }
}
