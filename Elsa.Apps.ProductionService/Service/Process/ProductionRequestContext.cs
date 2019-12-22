using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Apps.ProductionService.Models;
using Elsa.Common;
using Elsa.Core.Entities.Commerce.Inventory;
using Elsa.Core.Entities.Commerce.Inventory.Recipes;

namespace Elsa.Apps.ProductionService.Service.Process
{
    internal class ProductionRequestContext
    {
        public ProductionRequestContext(ISession session, ProductionRequest request)
        {
            Session = session;
            Request = request;

            request.IsValid = true;
        }

        public ISession Session { get; }

        public ProductionRequest Request { get; }

        public IRecipe Recipe { get; set; }

        public Amount RequestedAmount { get; set; }
        public Amount NominalRecipeAmount { get; set; }
        public decimal ComponentMultiplier { get; set; }

        public Amount MinimalAmount { get; set; }

        public IMaterial TargetMaterial { get; set; }
        
        public void InvalidateRequest(string message)
        {
            Request.IsValid = false;

            Request.Messages.Add(new RequestValidationMessage(true, message));
        }
    }
}
