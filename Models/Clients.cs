using System;
using System.Collections.Generic;

namespace AkarangaBank.Web.Models;

public class Client
{
    public int Id { get; set; }
    
    public string NumeroClient { get; set; } = string.Empty;
    
    public string Nom { get; set; } = string.Empty;
    
    public string Prenom { get; set; } = string.Empty;
    
    public DateTime DateNaissance { get; set; }
    
    public string NumeroCNI { get; set; } = string.Empty;
    
    public string? Telephone { get; set; }
    
    public string? Email { get; set; }
    
    public string? Adresse { get; set; }
    
    public int AgenceId { get; set; }
    
    // Propriété de navigation vers l'Agence de rattachement
    public virtual Agence? Agence { get; set; }
    
    public string Statut { get; set; } = "Actif"; // Actif, Suspendu, Cloture
    
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
    
    public DateTime? DateModification { get; set; }

    // ─────────────────────────────────────────────────────────────────────────
    // FIX ESSENTIEL POUR .ANY() : Initialisation des collections par défaut
    // ─────────────────────────────────────────────────────────────────────────
    
    // Un client peut posséder plusieurs comptes bancaires
    public virtual ICollection<Compte> Comptes { get; set; } = new List<Compte>();
    
    // Un client peut avoir plusieurs documents de vérification KYC historique
    public virtual ICollection<VerificationKYC> VerificationsKYC { get; set; } = new List<VerificationKYC>();
}
