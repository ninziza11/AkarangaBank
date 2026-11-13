using System.ComponentModel.DataAnnotations;

namespace AkarangaBank.Web.Models.Api
{
    public class LoginRequest
    {
        [Required(ErrorMessage = "L'identifiant est obligatoire.")]
        public string Identifiant { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le mot de passe est obligatoire.")]
        public string MotDePasse { get; set; } = string.Empty;
    }
}
