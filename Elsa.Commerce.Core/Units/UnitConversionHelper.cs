using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Common;
using Elsa.Common.Caching;
using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Inventory;

using Robowire;
using Robowire.RobOrm.Core;

namespace Elsa.Commerce.Core.Units
{
    public class UnitConversionHelper : IUnitConversionHelper
    {
        private readonly ISession m_session;
        private readonly IDatabase m_database;
        private readonly IPerProjectDbCache m_cache;
        private readonly IServiceLocator m_serviceLocator;

        public UnitConversionHelper(IPerProjectDbCache cache, IServiceLocator serviceLocator, IDatabase database, ISession session)
        {
            m_cache = cache;
            m_serviceLocator = serviceLocator;
            m_database = database;
            m_session = session;
        }

        public decimal ConvertAmount(int sourceUnitId, int targetUnitId, decimal sourceAmount)
        {
            if (sourceUnitId == targetUnitId)
            {
                return sourceAmount;
            }

            return GetConvertor(sourceUnitId, targetUnitId)
                .ConvertAmount(sourceUnitId, targetUnitId, sourceAmount);
        }

        public IMaterialUnit GetPrefferedUnit(IMaterialUnit a, IMaterialUnit b)
        {
            //TODO
            return a;
        }

        private ConvertorInstance GetConvertor(int sourceUnit, int targetUnit)
        {
            return m_cache.ReadThrough(
                $"unitConversionHelper:convertor_{sourceUnit}_to_{targetUnit}",
                () =>
                    {
                        var conversion =
                            GetAllConversions()
                                .FirstOrDefault(c => c.SourceUnitId == sourceUnit && c.TargetUnitId == targetUnit);
                        if (conversion == null)
                        {
                            throw new InvalidOperationException($"No unit conversion defined from {sourceUnit} to {targetUnit}");
                        }

                        return new ConvertorInstance(conversion, m_serviceLocator);
                    });
        }

        private IEnumerable<IUnitConversion> GetAllConversions()
        {

            return m_cache.ReadThrough(
                "unitConversionHelper:all_conversion_records",
                () =>
                    {
                        var pure =
                            m_database.SelectFrom<IUnitConversion>()
                                .Where(uc => uc.ProjectId == m_session.Project.Id)
                                .Execute()
                                .ToList();

                        var result = new List<IUnitConversion>(pure.Count * 2);

                        result.AddRange(pure);

                        foreach (var pureItem in
                            pure.Where(p => string.IsNullOrWhiteSpace(p.ConvertorClass) && p.Multiplier.HasValue))
                        {
                            if (
                                result.Any(
                                    r =>
                                        r.SourceUnitId == pureItem.TargetUnitId
                                        && r.TargetUnitId == pureItem.SourceUnitId))
                            {
                                continue;
                            }

                            result.Add(new InvertedConversion(pureItem));
                        }

                        return result;
                    });
        }

        private sealed class InvertedConversion : IUnitConversion
        {
            public InvertedConversion(IUnitConversion source)
            {
                Multiplier = 1m / (source.Multiplier ?? 1m);
                SourceUnitId = source.TargetUnitId;
                TargetUnitId = source.SourceUnitId;
            }

            public int ProjectId { get; set; }

            public IProject Project { get; }

            public int Id { get; }

            public int SourceUnitId { get; set; }

            public IMaterialUnit SourceUnit { get; }

            public int TargetUnitId { get; set; }

            public IMaterialUnit TargetUnit { get; }

            public decimal? Multiplier { get; set; }

            public string ConvertorClass { get; set; }
        }

        private sealed class ConvertorInstance : IUnitConvertor
        {
            private readonly IUnitConvertor m_convertor;

            public ConvertorInstance(IUnitConversion conversionEntry, IServiceLocator locator)
            {
                if (!string.IsNullOrWhiteSpace(conversionEntry.ConvertorClass))
                {
                    m_convertor = locator.InstantiateNow<IUnitConvertor>(conversionEntry.ConvertorClass);
                }
                else
                {
                    if (conversionEntry.Multiplier == null)
                    {
                        throw new InvalidOperationException("Conversion must have convertor class or mutiplier");
                    }

                    m_convertor = new DefaultConvertor(conversionEntry.Multiplier.Value);
                }
            }

            private sealed class DefaultConvertor : IUnitConvertor
            {
                private readonly decimal m_multiplier;

                public DefaultConvertor(decimal multiplier)
                {
                    m_multiplier = multiplier;
                }

                public decimal ConvertAmount(int sourceUnitId, int targetUnitId, decimal sourceAmount)
                {
                    return sourceAmount * m_multiplier;
                }
            }

            public decimal ConvertAmount(int sourceUnitId, int targetUnitId, decimal sourceAmount)
            {
                return m_convertor.ConvertAmount(sourceUnitId, targetUnitId, sourceAmount);
            }
        }
       
    }
}
