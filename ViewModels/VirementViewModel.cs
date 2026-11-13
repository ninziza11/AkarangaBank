using System.ComponentModel.DataAnnotations;

namespace AkarangaBank.Web.ViewModels;

public class VirementViewModel
{
    [Required(ErrorMessage = "Le compte source est obligatoire.")]
    [Display(Name = "Compte source")]
    public int CompteSourceId { get; set; }

    [Required(ErrorMessage = "Le compte destination est obligatoire.")]
    [Display(Name = "Compte destination")]
    public int CompteDestinationId { get; set; }

    [Required(ErrorMessage = "Le montant est obligatoire.")]
    [Range(0.01, double.MaxValue,
        ErrorMessage = "Le montant doit être supérieur à 0.")]
    [Display(Name = "Montant (BIF)")]
    public decimal Montant { get; set; }

    [StringLength(250)]
    [Display(Name = "Motif du virement")]
    public string? Motif { get; set; }

    [StringLength(250)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Numéro du compte source")]
    public string? NumeroCompteSource { get; set; }

    [Display(Name = "Numéro du compte destination")]
    public string? NumeroCompteDestination { get; set; }

    [Display(Name = "Vérification supplémentaire requise")]
    public bool NecessiteVerification { get; set; }

    [StringLength(6)]
    [Display(Name = "Code OTP")]
    public string? CodeOTP { get; set; }
}