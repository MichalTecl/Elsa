using System.Collections.Generic;

namespace Elsa.EditorBuilder
{
    public class EntityListingPage<T>
    {
        public EntityListingPage(IEnumerable<T> items, string nextPageTag)
        {
            Items = new List<T>(items);
            NextPageTag = nextPageTag;
        }

        public EntityListingPage(IEnumerable<T> items):this(items, null) {}

        public virtual string NextPageTag { get; }

        public virtual ICollection<T> Items { get; }
    }
}
