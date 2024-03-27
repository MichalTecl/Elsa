using Elsa.App.ImportExport.Model;
using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Microsoft.AspNetCore.Http;
using Robowire;
using Robowire.RoboApi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.ImportExport.Controllers
{
    [Controller("importexport")]
    public class ImportExportController : ElsaControllerBase
    {
        private readonly IServiceLocator _services;

        public ImportExportController(IWebSession webSession, ILog log, IServiceLocator services) : base(webSession, log)
        {
            _services = services;
        }

        public IEnumerable<ImpExpModuleInfo> GetModules()
        {
            var modules = _services.GetCollection<IImportExportModule>();

            foreach (var module in modules)
            {
                yield return new ImpExpModuleInfo { Title = module.Title, Description = module.Description, Uid = module.Uid };
            }
        }

        public FileResult Export(string moduleUid) 
        {
            var module = FindModule(moduleUid);

            var bytes = module.Export(out var filename);

            return new FileResult(filename, bytes);
        }

        public string Import(string moduleUid, Stream inputStream) 
        {
            var module = FindModule(moduleUid);
            return module.Import(inputStream);            
        }

        private IImportExportModule FindModule(string uid) 
        {
            return _services.GetCollection<IImportExportModule>().FirstOrDefault(m => m.Uid == uid) ?? throw new ArgumentException("Invalid module uid");
        }
    }
}
