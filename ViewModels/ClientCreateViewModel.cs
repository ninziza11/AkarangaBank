using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AkarangaBank.Web.ViewModels;

public class ClientCreateViewModel
{
    [Required(ErrorMessage = "Le numéro de client est obligatoire.")]
    [RegularExpression(@"^CLI-\d{6}$", ErrorMessage = "Le format doit être CLI-XXXXXX (ex: CLI-000001).")]
    public string NumeroClient { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le nom est obligatoire.")]
    [StringLength(50, ErrorMessage = "Le nom ne doit pas dépasser 50 caractères.")]
    public string Nom { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le prénom est obligatoire.")]
    [StringLength(50, ErrorMessage = "Le prénom ne doit pas dépasser 50 caractères.")]
    public string Prenom { get; set; } = string.Empty;

    [Required(ErrorMessage = "La date de naissance est obligatoire.")]
    [DataType(DataType.Date)]
    public DateTime DateNaissance { get; set; }

    [Required(ErrorMessage = "Le numéro de CNI est obligatoire.")]
    public string NumeroCNI { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Numéro de téléphone invalide.")]
    public string? Telephone { get; set; }

    [EmailAddress(ErrorMessage = "Adresse email invalide.")]
    public string? Email { get; set; }

    public string? Adresse { get; set; }

    [Required(ErrorMessage = "L'agence de rattachement est obligatoire.")]
    public int AgenceId { get; set; }

    // Utilisé pour remplir le <select> HTML dans la vue
    public List<SelectListItem> AgencesDisponibles { get; set; } = new();
}
