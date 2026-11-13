using System.ComponentModel.DataAnnotations;

namespace AkarangaBank.Web.ViewModels;

public class UtilisateurCreateViewModel
{
    [Required(ErrorMessage = "Le nom complet est obligatoire.")]
    public string NomComplet { get; set; } = string.Empty;

    [Required(ErrorMessage = "L'e-mail est obligatoire.")]
    [EmailAddress(ErrorMessage = "E-mail invalide.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le rôle est obligatoire.")]
    public string Role { get; set; } = string.Empty;

    [Required(ErrorMessage = "L'agence est obligatoire.")]
    public int? AgenceId { get; set; }

    [Required(ErrorMessage = "Le mot de passe est obligatoire.")]
    [MinLength(8, ErrorMessage = "8 caractères minimum.")]
    public string Password { get; set; } = string.Empty;
}