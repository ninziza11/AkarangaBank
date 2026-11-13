using System.ComponentModel.DataAnnotations;

namespace AkarangaBank.Web.ViewModels
{
    public class TwoFactorViewModel
    {
        public string SecretKey { get; set; } = string.Empty;

        // URL formatée pour être injectée dans l'image du QR Code
        public string QrCodeUrl { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le code de vérification à 6 chiffres est obligatoire.")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Le code doit comporter exactement 6 chiffres.")]
        [RegularExpression(@"^[0-9]*$", ErrorMessage = "Le code doit contenir uniquement des chiffres.")]
        [Display(Name = "Code de validation")]
        public string Code { get; set; } = string.Empty;
    }
}
