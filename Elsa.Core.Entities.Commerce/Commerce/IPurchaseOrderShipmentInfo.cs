using Elsa.Core.Entities.Commerce.Common;
using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Core.Entities.Commerce.Commerce
{
    [Entity]
    public interface IPurchaseOrderShipmentInfo : IOrderRelatedEntity, IIntIdEntity
    {
        [NVarchar(100, false)]
        string ShipmentProviderSymbol { get; set; }

        [NVarchar(100, true)]
        string ExternalTrackingNumber { get; set; }

        DateTime? TrackingMailSendDt { get; set; }

        /// <summary>
        /// Vytvoren zaznam u prepravce (bud z A|PI, nebo prvni ziskani)
        /// </summary>
        DateTime? ParcelRegistered { get; set; }

        /// <summary>
        /// Datum prvni detekce statusu, z jehoz vyznamu plyne, ze zasilka je jiz u dopravce, nebo datum prevzeti, pokud API dopravce poskytuje
        /// </summary>
        DateTime? ParcelTransportStarted { get; set; }

        /// <summary>
        /// Datum prvni detekce stavu Doruceno, nebo datum doruceni, pokud API dopravce poskytuje
        /// </summary>
        DateTime? Delivered { get; set; }

        /// <summary>
        /// Prestali jsme sledovat, protoze:
        /// 1. objednavka ma aspon jednu zasilku ve stavu doruceno
        /// 2. teto zasilce nezacal transport a objednavka ma jinou zasilku se zapocatym transportem
        /// 3. ubehlo > 30 dnu od ParcelRegistered
        /// </summary>        
        DateTime? Unwatched { get; set; }        
    }
}
