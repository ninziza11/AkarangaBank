using System;
using System.Collections.Generic;
using AkarangaBank.Web.Models;

namespace AkarangaBank.Web.ViewModels
{
    public class ReleveCompteViewModel
    {
        public int CompteId { get; set; }
        public string NumeroCompte { get; set; } = string.Empty;
        public string ClientNomComplet { get; set; } = string.Empty;
        public decimal SoldeActuel { get; set; }
        public string Devise { get; set; } = "BIF";

        // Filtres optionnels pour la recherche d'historique
        public DateTime? DateDebut { get; set; }
        public DateTime? DateFin { get; set; }

        // Liste des écritures comptables (le grand livre du compte)
        public IEnumerable<EcritureComptable> Mouvements { get; set; } = new List<EcritureComptable>();
    }
}
