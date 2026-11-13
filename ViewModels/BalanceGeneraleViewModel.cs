using System.Collections.Generic;

namespace AkarangaBank.Web.ViewModels
{
    public class BalanceGeneraleViewModel
    {
        public List<LigneBalanceViewModel> Lignes { get; set; } = new();
        
        // Totaux de contrôle de la balance
        public decimal TotalMouvementsDebit { get; set; }
        public decimal TotalMouvementsCredit { get; set; }
        public decimal TotalSoldesDebiteurs { get; set; }
        public decimal TotalSoldesCrediteurs { get; set; }

        // Indicateur d'égalité arithmétique stricte
        public bool EstEquilibree => 
            TotalMouvementsDebit == TotalMouvementsCredit && 
            TotalSoldesDebiteurs == TotalSoldesCrediteurs;
    }

    public class LigneBalanceViewModel
    {
        public string NumeroCompte { get; set; } = string.Empty;
        public string IntituleCompte { get; set; } = string.Empty; // Nom du client ou libellé du produit
        public decimal CumulDebit { get; set; }
        public decimal CumulCredit { get; set; }
        public decimal SoldeDebiteur { get; set; }
        public decimal SoldeCrediteur { get; set; }
    }
}
