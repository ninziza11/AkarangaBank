using System.Collections.Generic;

namespace AkarangaBank.Web.ViewModels
{
    public class CalculInteretsViewModel
    {
        public int NombreComptesTraites { get; set; }
        public decimal TotalInteretsVerses { get; set; }
        public List<LigneApercuInteret> DetailsComptes { get; set; } = new();
    }

    public class LigneApercuInteret
    {
        public string NumeroCompte { get; set; } = string.Empty;
        public string ClientNom { get; set; } = string.Empty;
        public decimal SoldeActuel { get; set; }
        public decimal Taux { get; set; }
        public decimal MontantInteret { get; set; }
    }
}
