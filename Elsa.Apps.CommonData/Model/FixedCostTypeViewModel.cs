using System.ComponentModel.DataAnnotations;

using Elsa.Core.Entities.Commerce.Accounting;

namespace Elsa.Apps.CommonData.Model
{
    public class FixedCostTypeViewModel
    {
        public FixedCostTypeViewModel() { }

        public FixedCostTypeViewModel(IFixedCostType e)
        {
            Id = e.Id;
            Name = e.Name;
            Percent = e.PercentToDistributeAmongProducts;
        }

        public int? Id { get; set; }

        [Display(Name = "Název")]
        [Required]
        public string Name { get; set; }

        [Display(Name ="Procento ceny k rozpočítání")]
        [Required]
        [Range(typeof(int), "0", "100")]
        public int Percent { get; set; } 
    }
}
