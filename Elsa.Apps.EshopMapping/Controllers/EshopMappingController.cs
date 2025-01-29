using Elsa.Apps.EshopMapping.Internal;
using Elsa.Apps.EshopMapping.Model;
using Elsa.Commerce.Core;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Common.Utils;
using Robowire.RoboApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Elsa.Apps.EshopMapping.Controllers
{
    [Controller("eshopMapping")]
    public class EshopMappingController : ElsaControllerBase
    {
        private readonly IErpRepository _erpRepository;
        private readonly IEshopMappingFacade _facade;
        private readonly IMaterialRepository _materialRepository;

        public EshopMappingController(IWebSession webSession, ILog log, IErpRepository erpRepository, IEshopMappingFacade facade, IMaterialRepository materialRepository) : base(webSession, log)
        {
            _erpRepository = erpRepository;
            _facade = facade;
            _materialRepository = materialRepository;
        }

        private int GetErpId()
        {
            // it's cached on repo level
            return _erpRepository.GetAllErps().First().Id;
        }

        public List<EshopItemMappingRecord> GetMappings(bool reloadErpProducts)
        {
            // TODO not ready for multiple erps
            return _facade.GetMappings(GetErpId(), reloadErpProducts);
        }

        public List<EshopItemMappingRecord> Map(string elsaMaterialName, string eshopProductName)
        {            
            _facade.Map(GetErpId(), elsaMaterialName, eshopProductName, false);
            return GetMappings(false);
        }

        public List<EshopItemMappingRecord> Unmap(string elsaMaterialName, string eshopProductName)
        {
            _facade.Unmap(GetErpId(), elsaMaterialName, eshopProductName);
            return GetMappings(false);
        }

        public List<EshopItemMappingRecord> HideMaterial(string elsaMaterialName)
        {
            var material = _materialRepository.GetMaterialByName(elsaMaterialName).Ensure();

            _materialRepository.SetMaterialHidden(material.Id, true, false);

            return GetMappings(false);
        }

        public string PeekOrders(bool kit, string placedName)
        {
            return string.Join(
                Environment.NewLine, 
                _facade.PeekOrders(placedName, GetErpId(), kit).Select(o => $"{o.OrderDt}  {o.OrderNumber}  {o.CustomerName}  {o.Status}")
                );
        }
    }
}
