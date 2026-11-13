using System.Collections.Generic;
using AkarangaBank.Web.Models;

namespace AkarangaBank.Web.ViewModels
{
    public class AuditJournalViewModel
    {
        public IEnumerable<EcritureComptable> Ecritures { get; set; } = new List<EcritureComptable>();
        
        // Total cumulé pour prouver l'équilibre en partie double
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        
        // Indicateur d'intégrité du grand livre
        public bool EstEquilibre => TotalDebit == TotalCredit;
    }
}
