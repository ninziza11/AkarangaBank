using System.Collections.Generic;
using AkarangaBank.Web.Models;

namespace AkarangaBank.Web.ViewModels
{
    public class ChequierSuiviViewModel
    {
        public IEnumerable<Chequier> Chequiers { get; set; } = new List<Chequier>();
        public string? FiltreStatut { get; set; }
    }
}
