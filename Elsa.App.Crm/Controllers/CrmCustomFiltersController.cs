using Elsa.App.Crm.Model;
using Elsa.App.Crm.Repositories;
using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Common.Utils;
using Newtonsoft.Json;
using Robowire.RoboApi;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Elsa.App.Crm.Controllers
{
    [Controller("CrmCustomFilters")]
    public class CrmCustomFiltersController : ElsaControllerBase
    {
        private readonly DistributorFiltersRepository _filtersRepository;

        public CrmCustomFiltersController(IWebSession webSession, ILog log, DistributorFiltersRepository filtersRepository) : base(webSession, log)
        {
            _filtersRepository = filtersRepository;
        }

        public List<CustomFilterInfo> GetSavedFilters()
        {
            return _filtersRepository
                .GetCustomFilters()
                .Select(f => new CustomFilterInfo { Id = f.Id, Name = f.Name })
                .ToList();
        }

        public List<CustomFilterInfo> SaveFilter(string name, DistributorGridFilter filter)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException("Filtr musí mít název");

            var f = _filtersRepository
                .GetCustomFilters()
                .FirstOrDefault(i => i.Name.Equals(name, System.StringComparison.InvariantCultureIgnoreCase));
        
            var json = JsonConvert.SerializeObject(filter);

            _filtersRepository.SaveCustomFilter(f?.Id, name, json);

            return GetSavedFilters();
        }

        public DistributorGridFilter LoadFilter(int id)
        {
            var record = _filtersRepository.GetCustomFilters().FirstOrDefault(f => f.Id == id).Ensure();

            return JsonConvert.DeserializeObject<DistributorGridFilter>(record.JsonData);
        }


    }
}
