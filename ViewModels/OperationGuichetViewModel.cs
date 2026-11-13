using System.ComponentModel.DataAnnotations;

namespace AkarangaBank.Web.ViewModels
{
    public class OperationGuichetViewModel
    {
        [Required(ErrorMessage = "Le numéro de compte est obligatoire.")]
        [Display(Name = "Numéro de compte cible")]
        public string NumeroCompte { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le type d'opération est obligatoire.")]
        [Display(Name = "Nature de l'opération")]
        public string TypeOperation { get; set; } = "Depot"; // Depot ou Retrait

        [Required(ErrorMessage = "Le montant est obligatoire.")]
        [Range(500, double.MaxValue, ErrorMessage = "Le montant minimum pour une opération au guichet est de 500 BIF.")]
        [Display(Name = "Montant de l'opération (BIF)")]
        public decimal Montant { get; set; }

        [Required(ErrorMessage = "Le nom du déposant ou du bénéficiaire est obligatoire.")]
        [StringLength(100)]
        [Display(Name = "Nom de la personne au guichet")]
        public string NomDonneur { get; set; } = string.Empty;

        [StringLength(150)]
        [Display(Name = "Remarques / Motif")]
        public string Remarque { get; set; } = string.Empty;
    }
}
