﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Common.Utils;

namespace Elsa.Apps.ProductionService.Service.Process.Steps
{
    internal class ValidateAmountAndPrice : IProductionRequestProcessingStep
    {
        private readonly IMaterialRepository m_materialRepository;

        public ValidateAmountAndPrice(IMaterialRepository materialRepository)
        {
            m_materialRepository = materialRepository;
        }

        public void Process(ProductionRequestContext context)
        {
            if ((context.Request.ProducingAmount ?? 0m) < 0.00001m)
            {
                context.Request.ProducingAmount = 0;
                context.InvalidateRequest("Je třeba zadat množství");
            }
            else
            {
                if (context.Request.PriceCalcAmount != (context.Request.ProducingAmount ?? 0m))
                {
                    if (context.Recipe.ProductionPricePerUnit != null)
                    {
                        context.Request.ProducingPrice =
                            context.Recipe.ProductionPricePerUnit * (context.Request.ProducingAmount ?? 0);
                    }

                    context.Request.PriceCalcAmount = (context.Request.ProducingAmount ?? 0m);
                }
            }
            
            if ((context.Request.ProducingPrice ?? 0m) < 0.001m)
            {
                if (context.TargetMaterial.RequiresProductionPrice != true)
                {                    
                    context.Request.ProducingPrice = 0m;
                }
                else
                {
                    context.InvalidateRequest("Je třeba zadat cenu práce");
                }
            }

            context.Request.DisableProductionPriceInput = (context.TargetMaterial.RequiresProductionPrice != true) && ((context.Recipe.ProductionPricePerUnit ?? 0M) == 0);
        }
    }
}
