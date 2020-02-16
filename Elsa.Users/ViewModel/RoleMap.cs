using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Elsa.Users.ViewModel
{
    public class RoleMap : List<RoleMapNode>
    {
        public RoleMap Clone()
        {
            var clone = new RoleMap();

            foreach (var top in this)
            {
                clone.Add(top.Clone());
            }

            return clone;
        }

        public RoleMapNode FindRole(int roleId)
        {
            var res = new List<RoleMapNode>(1);
            FindRoles(this, r => r.Id == roleId, res, 1);

            return res.FirstOrDefault();
        }

        public Dictionary<int, RoleMapNode> GetIndex()
        {
            var res = new Dictionary<int, RoleMapNode>(Count);

            void Index(IEnumerable<RoleMapNode> source)
            {
                foreach (var s in source)
                {
                    res[s.Id] = s;

                    Index(s.ChildRoles);
                }
            }

            Index(this);

            return res;
        }

        public void Import(IEnumerable<RoleMapNode> import)
        {
            var index = GetIndex();

            void Index(RoleMapNode n)
            {
                index[n.Id] = n;

                foreach (var child in n.ChildRoles)
                {
                    Index(child);
                }
            }

            if (import != null)
            {
                foreach (var imported in import)
                {
                    Index(imported);
                }
            }

            foreach (var v in index.Values)
            {
                v.ChildRoles.Clear();
            }

            Clear();

            foreach (var x in index.Values)
            {
                if (index.TryGetValue(x.ParentRoleId ?? -1, out var parent))
                {
                    parent.ChildRoles.Add(x);
                    x.CanEdit = true;
                }
                else
                {
                    Add(x);
                    x.CanEdit = false;
                }
            }
        }
        
        private static void FindRoles(IEnumerable<RoleMapNode> source, Func<RoleMapNode, bool> predicate,
            List<RoleMapNode> result, int take = int.MaxValue)
        {
            foreach (var roleMapNode in source)
            {
                if (predicate(roleMapNode))
                {
                    result.Add(roleMapNode);

                    if (result.Count >= take)
                    {
                        return;
                    }
                }

                FindRoles(roleMapNode.ChildRoles, predicate, result, take);
            }
        }
    }
}
