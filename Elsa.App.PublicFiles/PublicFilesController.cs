using Elsa.Common;
using Elsa.Common.Caching;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Robowire.RoboApi;
using System;
using System.Text;

namespace Elsa.App.PublicFiles
{
    [Controller("PublicFile")]
    public class PublicFilesController : ElsaControllerBase
    {
        private readonly ILog _log;
        private readonly ICache _cache;
        private readonly IPublicFilesHelper _publicFilesHelper;

        public PublicFilesController(IWebSession webSession, ILog log, ICache cache, IPublicFilesHelper publicFilesHelper) : base(webSession, log)
        {
            _log = log;
            _cache = cache;
            _publicFilesHelper = publicFilesHelper;
        }

        [AllowAnonymous]
        public FileResult GetFile(string cid, string ftype)
        {
            // check params for nulls
            if (string.IsNullOrWhiteSpace(cid) || string.IsNullOrWhiteSpace(ftype))
                return new FileResult("error.txt", Encoding.ASCII.GetBytes("500"));

            return _cache.ReadThrough<FileResult>($"PublicFiles/{cid.GetHashCode()}/{ftype.GetHashCode()}",
                TimeSpan.FromMinutes(1),
                () => _publicFilesHelper.GetFile(cid, ftype));
        }        
    }
}
