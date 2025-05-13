using Elsa.Core.Entities.Commerce.Common;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Common.Data
{
    public static class AutoRepoExtensions
    {
        public static T TryGet<T>(this AutoRepo<T> repo, int id) where T : class, IIntIdEntity
        {
            return repo.GetAll().FirstOrDefault(x => x.Id == id);
        }

        public static T Get<T>(this AutoRepo<T> repo, int id) where T : class, IIntIdEntity 
        {
            return repo.TryGet(id) ?? throw new ArgumentException("Invalid entity reference");
        }
    }
}
