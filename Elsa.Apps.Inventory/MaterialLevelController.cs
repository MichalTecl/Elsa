using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Commerce.Core.Model;
using Elsa.Commerce.Core.Warehouse;
using Elsa.Common;
using Elsa.Common.Logging;
using Elsa.Common.Utils;

using Robowire.RoboApi;

namespace Elsa.Apps.Inventory
{
    [Controller("materialLevel")]
    public class MaterialLevelController : ElsaControllerBase
    {
        private readonly IMaterialBatchFacade m_batchFacade;

        public MaterialLevelController(IWebSession webSession, ILog log, IMaterialBatchFacade batchFacade)
            : base(webSession, log)
        {
            m_batchFacade = batchFacade;
        }

        public IEnumerable<MaterialLevelModel> GetLevels()
        {
            return m_batchFacade.GetMaterialLevels(true).OrderBy(m => m.PercentLevel);
        }

        public LevelWarningBucket GetCurrentWarning()
        {
            var sb = new StringBuilder();

            foreach (var lowMaterial in m_batchFacade.GetMaterialLevels().OrderBy(m => m.PercentLevel).Where(l => l.ActualValue < l.MinValue))
            {
                sb.AppendLine($"{StringUtil.FormatDecimal(lowMaterial.ActualValue)} {lowMaterial.Unit} {lowMaterial.MaterialName}");
            }

            if (sb.Length == 0)
            {
                return new LevelWarningBucket(false, null);
            }

            return new LevelWarningBucket(true, sb.ToString());
        }
        
        public class LevelWarningBucket
        {
            public LevelWarningBucket(bool hasWarning, string warning)
            {
                HasWarning = hasWarning;
                Warning = warning;
            }

            public bool HasWarning { get; }
            public string Warning { get; }
        }
    }
}
