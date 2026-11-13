using System.Collections.Generic;
using AkarangaBank.Web.Models;

namespace AkarangaBank.Web.ViewModels
{
    public class AlerteAmlViewModel
    {
        public IEnumerable<AlerteAML> AlertesEnAttente { get; set; } = new List<AlerteAML>();
        public IEnumerable<AlerteAML> AlertesTraitees { get; set; } = new List<AlerteAML>();
        
        public int TotalAlertesBloquantes => AlertesEnAttente.Count();
    }
}
