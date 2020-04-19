using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Common;

namespace Elsa.App.MaterialLevels.Components.Model
{
    public class InventoryModel
    {
        private readonly InventoryModel m_aggregation;
        private List<string> m_warnings = null;

        public InventoryModel(InventoryModel aggregation)
        {
            m_aggregation = aggregation;
        }

        public int Id { get; set; }

        public string Name { get; set; }

        public int? WarningsCount => m_warnings?.Count;

        public string Warnings => m_warnings == null ? string.Empty : string.Join(Environment.NewLine, m_warnings.OrderBy(v => v));

        public void AddWarning(string materialName, Amount available)
        {
            m_aggregation?.AddWarning(materialName, available);

            m_warnings = m_warnings ?? new List<string>();

            m_warnings.Add($"{materialName} {available}");
        }

        public void Close()
        {
            m_warnings = m_warnings ?? new List<string>(0);
        }
    }
}
