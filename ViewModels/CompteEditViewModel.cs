using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AkarangaBank.Web.ViewModels;

public class CompteEditViewModel
{
    public int Id { get; set; }

    [Display(Name = "Numéro de compte")]
    public string NumeroCompte { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le type de compte est obligatoire.")]
    [Display(Name = "Type de compte")]
    public int TypeCompteId { get; set; }

    [Required(ErrorMessage = "L'agence est obligatoire.")]
    [Display(Name = "Agence")]
    public int AgenceId { get; set; }

    [Required]
    [StringLength(20)]
    [Display(Name = "Statut")]
    public string Statut { get; set; } = "Actif";

    // Affichage seul
    public string? ClientNom { get; set; }
    public decimal Solde { get; set; }
    public string Devise { get; set; } = "BIF";

    public List<SelectListItem> TypesCompteDisponibles { get; set; } = new();
    public List<SelectListItem> AgencesDisponibles { get; set; } = new();
}
