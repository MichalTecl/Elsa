using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Commerce.Core.Production.Recipes;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Common.Data;
using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Common.Security;
using Elsa.Core.Entities.Commerce.Inventory;
using Elsa.Core.Entities.Commerce.Inventory.Batches;
using Elsa.Core.Entities.Commerce.Inventory.Recipes;
using Robowire;

namespace Elsa.Commerce.Core.Warehouse.Impl.Model
{
    internal class MaterialBatchAdapter : AdapterBase<IMaterialBatch>, IMaterialBatch
    {
        private readonly IMaterialBatch m_adaptee;

        public MaterialBatchAdapter(IMaterialBatch adaptee, IServiceLocator sl) : base(sl, adaptee)
        {
            m_adaptee = adaptee;
        }
        public int Id { get => m_adaptee.Id; }
        public int ProjectId { get => m_adaptee.ProjectId; set => m_adaptee.ProjectId = value; }
        public decimal Volume { get => m_adaptee.Volume; set => m_adaptee.Volume = value; }
        public int UnitId { get => m_adaptee.UnitId; set => m_adaptee.UnitId = value; }
        public DateTime Created { get => m_adaptee.Created; set => m_adaptee.Created = value; }
        public string BatchNumber { get => m_adaptee.BatchNumber; set => m_adaptee.BatchNumber = value; }
        public string Note { get => m_adaptee.Note; set => m_adaptee.Note = value; }
        public DateTime? Expiration { get => m_adaptee.Expiration; set => m_adaptee.Expiration = value; }
        public decimal Price { get => m_adaptee.Price; set => m_adaptee.Price = value; }
        public decimal? ProductionWorkPrice { get => m_adaptee.ProductionWorkPrice; set => m_adaptee.ProductionWorkPrice = value; }
        public string InvoiceNr { get => m_adaptee.InvoiceNr; set => m_adaptee.InvoiceNr = value; }
        public string InvoiceVarSymbol { get => m_adaptee.InvoiceVarSymbol; set => m_adaptee.InvoiceVarSymbol = value; }
        public int? SupplierId { get => m_adaptee.SupplierId; set => m_adaptee.SupplierId = value; }
        public DateTime? FinalAccountingDate { get => m_adaptee.FinalAccountingDate; set => m_adaptee.FinalAccountingDate = value; }
        public int MaterialId { get => m_adaptee.MaterialId; set => m_adaptee.MaterialId = value; }
        public DateTime? CloseDt { get => m_adaptee.CloseDt; set => m_adaptee.CloseDt = value; }
        public DateTime? LockDt { get => m_adaptee.LockDt; set => m_adaptee.LockDt = value; }
        public int? LockUserId { get => m_adaptee.LockUserId; set => m_adaptee.LockUserId = value; }
        public int? PriceConversionId { get => m_adaptee.PriceConversionId; set => m_adaptee.PriceConversionId = value; }
        public int AuthorId { get => m_adaptee.AuthorId; set => m_adaptee.AuthorId = value; }
        public string LockReason { get => m_adaptee.LockReason; set => m_adaptee.LockReason = value; }
        public bool IsAvailable { get => m_adaptee.IsAvailable; set => m_adaptee.IsAvailable = value; }
        public DateTime? Produced { get => m_adaptee.Produced; set => m_adaptee.Produced = value; }
        public bool? IsHiddenForAccounting { get => m_adaptee.IsHiddenForAccounting; set => m_adaptee.IsHiddenForAccounting = value; }
        public int? RecipeId
        {
            get => Adaptee.RecipeId;
            set => Adaptee.RecipeId = value;
        }

        public IRecipe Recipe => Get<IRecipeRepository, IRecipe>("Recipe",r => RecipeId == null ? null : r.GetRecipe(RecipeId.Value));

        public ISupplier Supplier => Get<ISupplierRepository, ISupplier>("Supplier", r => SupplierId == null ? null : r.GetSupplier(SupplierId.Value));

        public IMaterial Material => Get<IMaterialRepository, IMaterial>("Material", r => r.GetMaterialById(MaterialId)?.Adaptee);

        public IUser Author => Get<IUserRepository, IUser>("Author", r => r.GetUser(AuthorId));

        public ICurrencyConversion PriceConversion => Get<ICurrencyRepository, ICurrencyConversion>("PriceConversion",
            r => PriceConversionId == null ? null : r.GetCurrencyConversion(PriceConversionId.Value));

        public IMaterialUnit Unit => Get<IUnitRepository, IMaterialUnit>("Unit", r => r.GetUnit(UnitId));

        public IUser LockUser =>
            Get<IUserRepository, IUser>("LockUser", r => LockUserId == null ? null : r.GetUser(LockUserId.Value));

        public IProject Project { get { throw new NotImplementedException(); } }

        public IEnumerable<IMaterialBatchComposition> Components =>
            Get<IMaterialBatchRepository, IEnumerable<IMaterialBatchComposition>>("Components",
                r => r.GetBatchComponents(Id));
    }
}
