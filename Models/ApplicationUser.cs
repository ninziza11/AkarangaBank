using Microsoft.AspNetCore.Identity;

namespace AkarangaBank.Web.Models;

public class ApplicationUser : IdentityUser<int>
{
    public string Nom { get; set; } = string.Empty;

    public string Prenom { get; set; } = string.Empty;

    public string NomComplet => $"{Prenom} {Nom}".Trim();

    public string Role { get; set; } = "client";

    public int AgenceId { get; set; }

    public Agence? Agence { get; set; }

    public int? ClientId { get; set; }

    public Client? Client { get; set; }

    public bool DeuxFA_Active { get; set; }

    public string? DeuxFA_Secret { get; set; }

    public string? DeuxFA_CodesRecuperation { get; set; }

    public DateTime? DerniereConnexion { get; set; }

    public bool EstActif { get; set; } = true;

    public DateTime DateCreation { get; set; } = DateTime.UtcNow;

    public DateTime? DateModification { get; set; }
}