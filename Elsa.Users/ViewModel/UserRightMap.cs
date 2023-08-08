using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Common.Interfaces;

namespace Elsa.Users.ViewModel
{
    public class UserRightMap 
    {
        private readonly List<UserRightViewModel> m_models = new List<UserRightViewModel>();

        private UserRightMap(IEnumerable<UserRightViewModel> models)
        {
            m_models.AddRange(models);
        }

        public UserRightMap(IEnumerable<UserRight> userRights, Dictionary<string, int> symbolIdIndex)
        {
            var index = userRights.ToDictionary(right => right, right => new UserRightViewModel(symbolIdIndex[right.Symbol], right.Symbol, right.Extends?.Symbol) { Description = right.Description});

            foreach (var kv in index)
            {
                if (kv.Key.Extends == null)
                {
                    m_models.Add(kv.Value);
                }
                else
                {
                    index[kv.Key.Extends].ChildRights.Add(kv.Value);
                }
            }
        }

        public UserRightMap Clone(Func<UserRightViewModel, bool> filter)
        {
            void Clone(IEnumerable<UserRightViewModel> sourceCollection, IList<UserRightViewModel> targetCollection)
            {
                foreach (var source in sourceCollection.Where(filter))
                {
                    var copy = new UserRightViewModel(source.Id, source.Symbol, source.ParentRightSymbol)
                    {
                        Description = source.Description
                    };

                    targetCollection.Add(copy);

                    Clone(source.ChildRights, copy.ChildRights);
                }
            }

            var targetList = new List<UserRightViewModel>(m_models.Count);
            Clone(m_models, targetList);

            return new UserRightMap(targetList);
        }

        public IEnumerable<UserRightViewModel> GetAssignments(Func<UserRightViewModel, bool> isAssigned)
        {
            var clone = Clone(i => true);

            void Visit(IEnumerable<UserRightViewModel> r, Func<UserRightViewModel, bool> vtor)
            {
                foreach (var ur in r)
                {
                    if (isAssigned(ur))
                    {
                        ur.Assigned = true;
                        Visit(ur.ChildRights, vtor);
                    }
                    else
                    {
                        ur.Assigned = false;
                        Visit(ur.ChildRights, i => false);
                    }
                }
            }

            Visit(clone.m_models, isAssigned);
            return clone.m_models;
        }

        private IEnumerable<string> Unwrap(IEnumerable<UserRightViewModel> models) 
        {
            foreach(var m in models) 
            {
                yield return m.Symbol;

                foreach(var ch in Unwrap(m.ChildRights))
                {
                    yield return ch;
                }
            }
        }

        public override string ToString()
        {
            return string.Join(", ", Unwrap(m_models));
        }
    }
}
