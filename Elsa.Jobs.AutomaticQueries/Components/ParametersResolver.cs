using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Jobs.AutomaticQueries.Database;

namespace Elsa.Jobs.AutomaticQueries.Components
{
    public class ParametersResolver : IParametersResolver
    {
        private readonly Dictionary<string, Func<object>> m_functions;

        private readonly ISession m_session;
        private readonly ILog m_log;

        public ParametersResolver(ISession session, ILog log)
        {
            m_session = session;
            m_log = log;

            m_functions = new Dictionary<string, Func<object>>
            {
                {"GET_PROJECT_ID", () => GetProjectId()},
                {"GET_PREV_MONTH_YEAR", () => GetLastMonthYear()},
                {"GET_PREV_MONTH", () => GetLastMonth()},
                {"GET_CULTURE", () => m_session.Culture },
                { "GET_WEEK_NUM", () => GetWeekNum() }
            };
        }
                
        public bool TryEvalExpression(string expression, out object result)
        {
            var normExp = expression?.Trim()?.ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(normExp) || (!m_functions.TryGetValue(normExp, out var function)))
            {
                m_log.Error($"Expression \"{expression}\" => \"{normExp}\" not recognized");
                result = null;
                return false;
            }

            try
            {
                result = function();
                return true;
            }
            catch (Exception ex)
            {
                m_log.Error($"Expression \"{expression}\" => \"{normExp}\" evaluation failed", ex);
                throw;
            }
        }

        public ParametersResolution ResolveParams(IEnumerable<IAutoQueryParameter> parameters, string titlePattern)
        {
            var result = new ParametersResolution
            {
                TransformedTitle = titlePattern
            };

            var tagSb = new StringBuilder();
            foreach (var param in parameters.OrderBy(p => p.Id))
            {
                if (!TryEvalExpression(param.Expression, out var paramValue))
                {
                    m_log.Error($"Cannot resolve parameter {param.ParameterName}:={param.Expression}");
                    throw new InvalidOperationException($"Cannot resolve parameter {param.ParameterName}:={param.Expression}");
                }

                var outputValue = (paramValue ?? "NULL").ToString();

                tagSb.Append($"{outputValue}|");

                result.TransformedTitle = result.TransformedTitle.Replace(param.ParameterName, outputValue);
                
                if (param.TriggerOnly)
                {
                    continue;
                }

                if (result.Parameters.ContainsKey(param.ParameterName))
                {
                    m_log.Info($"Warning - param {param.ParameterName} overwrites itself");
                }

                result.Parameters[param.ParameterName] = paramValue;
            }

            if (tagSb.Length > 1000)
            {
                m_log.Info($"Trigger exceeds 1000 chars, saving HashCode only: {tagSb}");
                result.Trigger = tagSb.ToString().GetHashCode().ToString();
            }
            else
            {
                result.Trigger = tagSb.ToString();
            }

            return result;
        }

        private int GetProjectId()
        {
            return m_session.Project.Id;
        }

        private int GetLastMonthYear()
        {
            return GetLastMonthYearAndMonth().Item1;
        }

        private int GetLastMonth()
        {
            return GetLastMonthYearAndMonth().Item2;
        }

        private Tuple<int, int> GetLastMonthYearAndMonth()
        {
            var now = DateTime.Now;
            now = new DateTime(now.Year, now.Month, 1).AddMonths(-1);

            return new Tuple<int, int>(now.Year, now.Month);
        }

        private object GetWeekNum()
        {
            return CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(DateTime.Now, CalendarWeekRule.FirstDay, DayOfWeek.Monday);
        }
    }
}
