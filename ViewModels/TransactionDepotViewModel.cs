using System.ComponentModel.DataAnnotations;

namespace AkarangaBank.Web.ViewModels
{
    public class TransactionDepotViewModel
    {
        [Required(ErrorMessage = "Le numéro de compte d'agence est obligatoire.")]
        [Display(Name = "Numéro de Compte")]
        public string NumeroCompte { get; set; } = string.Empty;

        [Display(Name = "Titulaire du Compte")]
        public string? ClientNomComplet { get; set; }

        [Display(Name = "Solde Actuel (BIF)")]
        public decimal SoldeActuel { get; set; }

        [Required(ErrorMessage = "Le montant du dépôt d'espèces est obligatoire.")]
        [Range(1000, 1000000000, ErrorMessage = "Le versement minimum autorisé au guichet est de 1 000 BIF.")]
        [Display(Name = "Montant du Dépôt (BIF)")]
        public decimal Montant { get; set; }

        [StringLength(250, ErrorMessage = "La remarque ne doit pas dépasser 250 caractères.")]
        [Display(Name = "Remarques / Motif du dépôt")]
        public string? Remarque { get; set; }
    }
}
