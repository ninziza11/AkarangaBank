using System.ComponentModel.DataAnnotations;

namespace AkarangaBank.Web.ViewModels
{
    public class UserCreateViewModel
    {
        [Required(ErrorMessage = "L'identifiant est obligatoire.")]
        [StringLength(50, ErrorMessage = "L'identifiant ne doit pas dépasser 50 caractères.")]
        [Display(Name = "Identifiant / Code Utilisateur")]
        public string Identifiant { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le nom complet est obligatoire.")]
        [Display(Name = "Nom Complet")]
        public string NomComplet { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le rôle est obligatoire.")]
        [Display(Name = "Rôle de l'acteur")]
        public string Role { get; set; } = "client"; // admin, guichetier, gestionnaire, auditeur, client

        [Required(ErrorMessage = "Le mot de passe initial est obligatoire.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Le mot de passe doit contenir au moins 6 caractères.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mot de passe initial")]
        public string MotDePasse { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirmer le mot de passe")]
        [Compare("MotDePasse", ErrorMessage = "Les deux mots de passe ne correspondent pas.")]
        public string ConfirmerMotDePasse { get; set; } = string.Empty;
    }
}
