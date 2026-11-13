using System.ComponentModel.DataAnnotations;

namespace AkarangaBank.Web.ViewModels;

public class DepotViewModel
{
    [Required(ErrorMessage = "Le compte est obligatoire.")]
    [Display(Name = "Compte")]
    public int CompteId { get; set; }

    [Required(ErrorMessage = "Le montant est obligatoire.")]
    [Range(0.01, double.MaxValue,
        ErrorMessage = "Le montant doit être supérieur à 0.")]
    [Display(Name = "Montant (BIF)")]
    public decimal Montant { get; set; }

    [StringLength(250)]
    [Display(Name = "Commentaire")]
    public string? Commentaire { get; set; }
}