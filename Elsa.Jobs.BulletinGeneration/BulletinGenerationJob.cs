using System;
using Elsa.Common.Logging;
using Elsa.Jobs.Common;

namespace Elsa.Jobs.BulletinGeneration
{
    public class BulletinGenerationJob : IExecutableJob
    {
        private const string OUTPUT_DIRECTORY = @"C:\Elsa\Bulletin";
        private readonly ILog _log;
        private readonly BulletinDocumentGenerator _generator;

        public BulletinGenerationJob(ILog log, BulletinDocumentGenerator generator)
        {
            _log = log;
            _generator = generator;
        }

        public void Run(string customDataJson)
        {
            var path = _generator.Save(OUTPUT_DIRECTORY, "bulletin", DateTime.Now);
            _log.Info($"Bulletin saved to {path}");
        }
    }
}
