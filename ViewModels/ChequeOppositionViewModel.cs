using System.ComponentModel.DataAnnotations;

namespace AkarangaBank.Web.ViewModels
{
    public class ChequeOppositionViewModel
    {
        [Required]
        public int ChequierId { get; set; }

        [Display(Name = "Numéro de compte")]
        public string NumeroCompte { get; set; } = string.Empty;

        [Display(Name = "Titulaire du compte")]
        public string ClientNomComplet { get; set; } = string.Empty;

        [Display(Name = "Série du chéquier original")]
        public string PlageChequierOrigine { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le numéro de début d'opposition est obligatoire.")]
        [Display(Name = "Bloquer à partir du numéro")]
        public string NumeroDebutOpposition { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le numéro de fin d'opposition est obligatoire.")]
        [Display(Name = "Jusqu'au numéro inclus")]
        public string NumeroFinOpposition { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le motif légal est obligatoire pour enregistrer l'opposition.")]
        [Display(Name = "Motif de l'opposition")]
        public string Motif { get; set; } = "Perte"; // Perte, Vol, UtilisationFrauduleuse
    }
}
