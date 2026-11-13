using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AkarangaBank.Web.Data;
using AkarangaBank.Web.Models;
using AkarangaBank.Web.ViewModels;

namespace AkarangaBank.Web.Services;

public class UtilisateurService : IUtilisateurService
{
    private const int LongueurMinMotDePasse = 8;

    private static readonly string[] RolesAutorises =
    {
        "client",
        "guichetier",
        "gestionnaire",
        "auditeur",
        "admin"
    };

    private readonly AkarangaBankContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<UtilisateurService> _logger;

    public UtilisateurService(
        AkarangaBankContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<UtilisateurService> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<IEnumerable<ApplicationUser>> GetAllAsync()
    {
        return await _context.Users
            .Include(u => u.Agence)
            .OrderBy(u => u.Nom)
            .ThenBy(u => u.Prenom)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<ApplicationUser?> GetByIdAsync(int id)
    {
        return await _context.Users
            .Include(u => u.Agence)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<(bool Success, string? Error)> CreateAsync(
        UtilisateurCreateViewModel vm)
    {
        try
        {
            var email = vm.Email?.Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            {
                return (false, "L'adresse e-mail est obligatoire et doit être valide.");
            }

            if (string.IsNullOrWhiteSpace(vm.NomComplet))
            {
                return (false, "Le nom complet est obligatoire.");
            }

            if (!EstMotDePasseValide(vm.Password))
            {
                return (false,
                    "Le mot de passe doit contenir au moins 8 caractères, " +
                    "une majuscule, une minuscule et un chiffre.");
            }

            var role = NormaliserRole(vm.Role);

            if (role == null)
            {
                return (false, "Rôle invalide.");
            }

            if (vm.AgenceId is not int agenceId ||
                !await _context.Agences.AnyAsync(a => a.Id == agenceId))
            {
                return (false, "Agence introuvable.");
            }

            var emailExiste = await _context.Users
                .AnyAsync(u => u.Email == email);

            if (emailExiste)
            {
                return (false, "Cette adresse e-mail professionnelle est déjà attribuée.");
            }

            var partiesNom = vm.NomComplet.Trim().Split(' ', 2);

            var utilisateur = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                Nom = partiesNom.Length > 1 ? partiesNom[1] : partiesNom[0],
                Prenom = partiesNom[0],
                Role = role,
                AgenceId = agenceId,
                DeuxFA_Active = false,
                EstActif = true,
                DateCreation = DateTime.UtcNow
            };

            var resultat = await _userManager.CreateAsync(
                utilisateur,
                vm.Password);

            if (!resultat.Succeeded)
            {
                var erreur = string.Join(
                    " ",
                    resultat.Errors.Select(e => e.Description));

                return (false, erreur);
            }

            await _userManager.AddToRoleAsync(utilisateur, role);

            _logger.LogInformation(
                "Utilisateur {Email} créé avec le rôle {Role}.",
                utilisateur.Email,
                utilisateur.Role);

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Échec de la création de l'utilisateur {Email}.",
                vm.Email);

            return (false,
                "Erreur : l'utilisateur n'a pas pu être créé. Veuillez réessayer.");
        }
    }

    public async Task<(bool Success, string? Error)> ToggleActifAsync(int id)
    {
        try
        {
            var utilisateur = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id);

            if (utilisateur == null)
            {
                return (false, "Utilisateur introuvable.");
            }

            if (utilisateur.EstActif &&
                utilisateur.Role == "admin" &&
                !await ExisteAutreAdminActifAsync(utilisateur.Id))
            {
                return (false, "Impossible de désactiver le dernier administrateur actif.");
            }

            utilisateur.EstActif = !utilisateur.EstActif;
            utilisateur.DateModification = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Échec du changement d'état de l'utilisateur {UtilisateurId}.",
                id);

            return (false,
                "Erreur : le changement d'état n'a pas pu être enregistré.");
        }
    }

    public async Task<(bool Success, string? Error)> ResetPasswordAsync(
        int id,
        string newPassword)
    {
        try
        {
            if (!EstMotDePasseValide(newPassword))
            {
                return (false,
                    "Le mot de passe doit contenir au moins 8 caractères, " +
                    "une majuscule, une minuscule et un chiffre.");
            }

            var utilisateur = await _userManager.FindByIdAsync(
                id.ToString());

            if (utilisateur == null)
            {
                return (false, "Utilisateur introuvable.");
            }

            var token = await _userManager
                .GeneratePasswordResetTokenAsync(utilisateur);

            var resultat = await _userManager.ResetPasswordAsync(
                utilisateur,
                token,
                newPassword);

            if (!resultat.Succeeded)
            {
                var erreur = string.Join(
                    " ",
                    resultat.Errors.Select(e => e.Description));

                return (false, erreur);
            }

            utilisateur.DateModification = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Échec de la réinitialisation du mot de passe de l'utilisateur {UtilisateurId}.",
                id);

            return (false,
                "Erreur : le mot de passe n'a pas pu être réinitialisé.");
        }
    }

    public async Task<(bool Success, string? Error)> AssignRoleAsync(
        int id,
        string role)
    {
        try
        {
            var roleNormalise = NormaliserRole(role);

            if (roleNormalise == null)
            {
                return (false, "Rôle invalide.");
            }

            var utilisateur = await _userManager.FindByIdAsync(
                id.ToString());

            if (utilisateur == null)
            {
                return (false, "Utilisateur introuvable.");
            }

            if (utilisateur.Role == "admin" &&
                roleNormalise != "admin" &&
                utilisateur.EstActif &&
                !await ExisteAutreAdminActifAsync(utilisateur.Id))
            {
                return (false, "Impossible de retirer le rôle au dernier administrateur actif.");
            }

            var rolesActuels = await _userManager.GetRolesAsync(utilisateur);

            if (rolesActuels.Any())
            {
                await _userManager.RemoveFromRolesAsync(
                    utilisateur,
                    rolesActuels);
            }

            utilisateur.Role = roleNormalise;
            utilisateur.DateModification = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _userManager.AddToRoleAsync(
                utilisateur,
                roleNormalise);

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Échec du changement de rôle de l'utilisateur {UtilisateurId}.",
                id);

            return (false, "Erreur : le rôle n'a pas pu être modifié.");
        }
    }

    public async Task<(bool Success, string? Error)> AuthentifierAsync(
        string identifiant,
        string password)
    {
        try
        {
            var identifiantNormalise = identifiant?.Trim();

            if (string.IsNullOrWhiteSpace(identifiantNormalise) ||
                string.IsNullOrWhiteSpace(password))
            {
                return (false, "L'identifiant et le mot de passe sont obligatoires.");
            }

            var utilisateur = await TrouverUtilisateurAsync(identifiantNormalise);

            if (utilisateur == null || !utilisateur.EstActif)
            {
                return (false, "Identifiants incorrects ou compte inactif.");
            }

            var motDePasseValide = await _userManager
                .CheckPasswordAsync(utilisateur, password);

            if (!motDePasseValide)
            {
                return (false, "Identifiants incorrects.");
            }

            utilisateur.DerniereConnexion = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Échec de l'authentification pour {Identifiant}.",
                identifiant);

            return (false, "Erreur lors de la connexion. Veuillez réessayer.");
        }
    }

    private async Task<ApplicationUser?> TrouverUtilisateurAsync(
        string identifiant)
    {
        if (identifiant.Contains('@'))
        {
            return await _userManager.FindByEmailAsync(
                identifiant.ToLowerInvariant());
        }

        return await _userManager.FindByNameAsync(identifiant);
    }

    private static bool EstMotDePasseValide(string? motDePasse)
    {
        if (string.IsNullOrWhiteSpace(motDePasse))
        {
            return false;
        }

        if (motDePasse.Length < LongueurMinMotDePasse)
        {
            return false;
        }

        return motDePasse.Any(char.IsUpper) &&
               motDePasse.Any(char.IsLower) &&
               motDePasse.Any(char.IsDigit);
    }

    private static string? NormaliserRole(string? role)
    {
        var roleNormalise = role?.Trim().ToLowerInvariant();

        return RolesAutorises.Contains(roleNormalise)
            ? roleNormalise
            : null;
    }

    private async Task<bool> ExisteAutreAdminActifAsync(int idExclu)
    {
        return await _context.Users
            .AnyAsync(u =>
                u.Id != idExclu &&
                u.Role == "admin" &&
                u.EstActif);
    }
}