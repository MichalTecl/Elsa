using System;
using Elsa.Commerce.Core.Warehouse;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Inventory.Batches;
using Newtonsoft.Json;

namespace Elsa.Commerce.Core.Model
{
    public sealed class BatchKey
    {
        private readonly int? m_batchId;
        private int? m_materialId;
        private string m_batchNumber;

        private BatchKey(int? batchId, string batchNumber, int? materialId)
        {
            m_batchId = batchId;
            m_batchNumber = batchNumber;
            m_materialId = materialId;
        }

        [Obsolete("For JSON deserialization only")]
        [JsonConstructor]
        public BatchKey(string keyString)
        {
            var parsedKey = Parse(keyString);
            m_materialId = parsedKey.m_materialId;
            m_batchNumber = parsedKey.m_batchNumber;
        }

        public BatchKey(int batchId):this(batchId, null, null) { }

        public BatchKey(int materialId, string batchNumber):this(null, batchNumber, materialId) { }

        public int GetMaterialId(IBatchKeyResolver repo)
        {
            EnsureLoaded(repo);
            return m_materialId ?? -1;
        }

        public string GetBatchNumber(IBatchKeyResolver repo)
        {
            EnsureLoaded(repo);
            return m_batchNumber;
        }

        public string ToString(IBatchKeyResolver repo)
        {
            EnsureLoaded(repo);
            return $"{m_batchNumber}:{m_materialId}";
        }

        public bool Match(IMaterialBatch batch, IBatchKeyResolver repo)
        {
            EnsureLoaded(repo);

            return batch.MaterialId == m_materialId &&
                   batch.BatchNumber.Equals(m_batchNumber, StringComparison.InvariantCultureIgnoreCase);
        }

        public bool Match(BatchKey key, IBatchKeyResolver repo)
        {
            if (key == null)
            {
                return false;
            }

            var keyBnr = key.GetBatchNumber(repo);
            var keyMat = key.GetMaterialId(repo);
            var thiBnr = GetBatchNumber(repo);
            var thiMat = GetMaterialId(repo);

            return keyBnr.Equals(thiBnr, StringComparison.InvariantCultureIgnoreCase) && keyMat == thiMat;
        }

        private void EnsureLoaded(IBatchKeyResolver resolver)
        {
            if (m_materialId == null || string.IsNullOrWhiteSpace(m_batchNumber))
            {
                if (resolver == null)
                {
                    throw new InvalidOperationException("No resolver");
                }

                var key = resolver.GetBatchNumberAndMaterialIdByBatchId(m_batchId ?? -1).Ensure();
                m_materialId = key.Item1;
                m_batchNumber = key.Item2;
            }
        }

        public static BatchKey Parse(string key)
        {
            var parts = key.Split(':');

            return new BatchKey(int.Parse(parts[1].Trim()), parts[0].Trim());
        }

        public string UnsafeToString()
        {
            return ToString(null);
        }
    }
}
