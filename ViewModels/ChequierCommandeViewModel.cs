using System.ComponentModel.DataAnnotations;

namespace AkarangaBank.Web.ViewModels
{
    public class ChequierCommandeViewModel
    {
        [Required]
        public int CompteId { get; set; }

        [Display(Name = "Numéro de compte")]
        public string NumeroCompte { get; set; } = string.Empty;

        [Display(Name = "Titulaire du compte")]
        public string ClientNomComplet { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le nombre de feuillets est obligatoire.")]
        [Display(Name = "Format du chéquier (Nombre de feuillets)")]
        public int NombreFeuillets { get; set; } = 25; // Choix : 25 ou 50

        [Required(ErrorMessage = "Le numéro de premier chèque est requis pour la traçabilité.")]
        [RegularExpression(@"^\d+$", ErrorMessage = "Le numéro de début doit contenir uniquement des chiffres.")]
        [Display(Name = "Premier numéro de la série")]
        public string NumeroDebut { get; set; } = string.Empty;
    }
}
