﻿using System.Collections.Generic;

namespace Elsa.Users.ViewModel
{
    public class RoleMapNode
    {
        private bool m_canEdit = false;

        public int Id { get; set; }

        public string Name { get; set; }

        public int? ParentRoleId { get; set; }

        public bool CanEdit
        {
            get => m_canEdit && ParentRoleId != null;
            set => m_canEdit = value;
        }
        
        public List<RoleMapNode> ChildRoles { get; } = new List<RoleMapNode>();

        internal RoleMapNode Clone()
        {
            var clone = new RoleMapNode()
            {
                Id = Id,
                Name = Name,
                ParentRoleId = ParentRoleId
            };

            foreach (var child in ChildRoles)
            {
                ChildRoles.Add(child.Clone());
            }

            return clone;
        }
    }
}
