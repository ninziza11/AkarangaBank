using System;
using System.ComponentModel.DataAnnotations;

namespace AkarangaBank.Web.Models;

public class AuditLog
{
    public int Id { get; set; }

    [Required]
    public string Utilisateur { get; set; } = "Système"; // Identifiant de l'agent ou du client

    [Required]
    public string Action { get; set; } = string.Empty; // Ex: "MiseAJour_KYC", "Ouverture_Compte", "Connexion"

    [Required]
    public string TableConcernee { get; set; } = string.Empty; // Ex: "Clients", "Comptes"

    public string? EnregistrementId { get; set; } // ID de la ligne modifiée

    public string? ValeurAvant { get; set; } // JSON des données avant modification

    public string? ValeurApres { get; set; } // JSON des données après modification

    public DateTime DateEvenement { get; set; } = DateTime.UtcNow;

    public string? AdresseIP { get; set; }

    public string? UserAgent { get; set; }
}
