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
using System.Web;

namespace Elsa.Apps.EshopMapping.Controllers
{
    [Controller("eshopMapping")]
    public class EshopMappingController : ElsaControllerBase
    {
        private readonly IErpRepository _erpRepository;
        private readonly IEshopMappingFacade _facade;
        private readonly IMaterialRepository _materialRepository;
        private readonly IKitProductRepository _kitProductRepository;

        public EshopMappingController(IWebSession webSession, ILog log, IErpRepository erpRepository, IEshopMappingFacade facade, IMaterialRepository materialRepository, IKitProductRepository kitProductRepository) : base(webSession, log)
        {
            _erpRepository = erpRepository;
            _facade = facade;
            _materialRepository = materialRepository;
            _kitProductRepository = kitProductRepository;
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

        public List<EshopItemMappingRecord> UpdateKitItem(int kitItemId, string newItemName)
        {
            _kitProductRepository.UpdateKitItemMapping(kitItemId, newItemName);

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

        public string GetKitCode(int kitDefinitionId)
        {
            var kit = _kitProductRepository.GetKitDefinition(kitDefinitionId).Ensure();

            /*
             <p style="display: none;">
            ##SADA{ 
                "Mega deodorant (80 g v plastu)": [ "Citronová meduňka (bez sody)", "V cukrárně (bez sody)", "V lese najdeš se (bez sody)", "Citronová meduňka (se sodou)", "Růžová zahrada (se sodou)", "Levandulové pole (se sodou)", "Pačuli, máta, rozmarýn (se sodou)"], 
                "Mega pleťový krém (60 ml)": ["Citronová meduňka (mastná pleť)", "Arganový olej, levandule (smíšená, normální pleť)", "Anti-pupínek (akné, problematická pleť)", "Smyslná a věrná sama sobě (suchá, zralá pleť)", "Kakaové máslo, vanilka (suchá, citlivá pleť)", "Olej z granátového jablka (citlivá pleť)"] 
	            }
            </p>
             */

            var sb = new StringBuilder();
            sb.AppendLine("<p style=\"display: none;\">");
            sb.AppendLine("##SADA{");

            bool firstGroup = true;
            foreach (var group in kit.SelectionGroups)
            {
                if (!firstGroup)
                {
                    sb.AppendLine(",");                    
                }

                firstGroup = false;
                
                sb.Append("  ")
                   .Append(HttpUtility.JavaScriptStringEncode(group.InTextMarker, true))
                   .Append(": [")
                   .Append(string.Join(", ", group.Items.Select(i => HttpUtility.JavaScriptStringEncode(i.InTextMarker, true))))
                   .Append("]");
            }

            sb.AppendLine().AppendLine("}");
            sb.AppendLine("</p>");

            return sb.ToString();
        }

    }
}
