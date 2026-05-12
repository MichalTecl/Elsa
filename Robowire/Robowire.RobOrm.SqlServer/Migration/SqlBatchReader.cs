using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Robowire.RobOrm.SqlServer.Migration
{
    internal static class SqlBatchReader
    {
        public static IEnumerable<string> GetBatches(string script)
        {
            using (var reader = new StringReader(script ?? string.Empty))
            {
                foreach (var batch in GetBatches(reader))
                {
                    yield return batch;
                }
            }
        }

        public static IEnumerable<string> GetBatches(TextReader scriptReader)
        {
            var sb = new StringBuilder();

            while (true)
            {
                var line = scriptReader.ReadLine();
                if (line == null)
                {
                    break;
                }

                if (line.Trim().TrimEnd(';').Equals("go", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (TryPopBatch(sb, out var batch))
                    {
                        yield return batch;
                    }

                    continue;
                }

                sb.AppendLine(line);
            }

            if (TryPopBatch(sb, out var finalBatch))
            {
                yield return finalBatch;
            }
        }

        private static bool TryPopBatch(StringBuilder sb, out string batch)
        {
            batch = sb.ToString().Trim();
            sb.Clear();

            return !string.IsNullOrWhiteSpace(batch);
        }
    }
}
