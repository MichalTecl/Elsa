using Elsa.Core.Entities.Commerce.Common;

using Newtonsoft.Json;

namespace Elsa.Core.Entities.Commerce.Core
{
    public interface IProjectRelatedEntity
    {
        [JsonIgnore]
        int ProjectId { get; set; }

        [JsonIgnore]
        IProject Project { get; }
    }
}
