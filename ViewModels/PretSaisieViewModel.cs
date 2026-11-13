using System.ComponentModel.DataAnnotations;

namespace AkarangaBank.Web.ViewModels
{
    public class PretSaisieViewModel
    {
        [Required(ErrorMessage = "Le choix du compte bénéficiaire est obligatoire.")]
        [Display(Name = "Numéro de Compte")]
        public string NumeroCompte { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le montant principal du crédit est obligatoire.")]
        [Range(100000, 500000000, ErrorMessage = "Le montant doit être compris entre 100 000 et 500 000 000 BIF.")]
        [Display(Name = "Montant du Principal (BIF)")]
        public decimal MontantPrincipal { get; set; }

        [Required(ErrorMessage = "Le taux d'intérêt annuel est requis.")]
        [Range(1, 35, ErrorMessage = "Le taux doit être compris entre 1% et 35%.")]
        [Display(Name = "Taux d'intérêt annuel (%)")]
        public decimal TauxInteret { get; set; } = 14.5m;

        [Required(ErrorMessage = "La durée de remboursement est obligatoire.")]
        [Range(3, 120, ErrorMessage = "La durée doit être comprise entre 3 et 120 mois.")]
        [Display(Name = "Durée de remboursement (Mois)")]
        public int DureeMois { get; set; } = 24;

        [Display(Name = "Remarques / Objet du crédit")]
        public string Remarque { get; set; } = string.Empty;
    }
}
