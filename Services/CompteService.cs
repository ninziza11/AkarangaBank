using Microsoft.EntityFrameworkCore;
using AkarangaBank.Web.Data;
using AkarangaBank.Web.Models;
using AkarangaBank.Web.ViewModels;
using System;
using System.Threading.Tasks;

namespace AkarangaBank.Web.Services;

public class CompteService : ICompteService
{
    private readonly AkarangaBankContext _context;

    public CompteService(AkarangaBankContext context)
    {
        _context = context;
    }

    public async Task<Compte> CreateAsync(CompteCreateViewModel vm)
    {
        // 1. Récupérer le solde minimum exigé pour ce type de compte
        var typeCompte = await _context.TypesCompte.FindAsync(vm.TypeCompteId);
        decimal soldeInitial = typeCompte?.SoldeMinimum ?? 0m;

        // 2. Générer le numéro de compte bancaire unique
        string numeroCompte = await GenererNumeroCompteUniqueAsync(vm.AgenceId);

        var compte = new Compte
        {
            NumeroCompte = numeroCompte,
            ClientId = vm.ClientId,
            TypeCompteId = vm.TypeCompteId,
            AgenceId = vm.AgenceId,
            Solde = soldeInitial, 
            Devise = string.IsNullOrWhiteSpace(vm.Devise) ? "BIF" : vm.Devise, // CORRECTION : Évite d'insérer une valeur vide ou nulle en BDD
            Statut = "Actif",
            DateOuverture = DateTime.UtcNow
        };

        _context.Comptes.Add(compte);
        await _context.SaveChangesAsync();
        return compte;
    }

    public async Task<bool> ChangerStatutAsync(int id, string nouveauStatut)
    {
        var compte = await _context.Comptes.FindAsync(id);
        if (compte is null) return false;

        if (nouveauStatut != "Actif" && nouveauStatut != "Bloque" && nouveauStatut != "Cloture")
            return false;

        compte.Statut = nouveauStatut;
        if (nouveauStatut == "Cloture")
        {
            compte.DateFermeture = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<string> GenererNumeroCompteUniqueAsync(int agenceId)
    {
        var agence = await _context.Agences.FindAsync(agenceId);
        
        // CORRECTION DE SÉCURITÉ : Si l'agence n'est pas chargée (Id invalide ou absent), 
        // évite un crash de type NullReferenceException sur agence.Code
        string codeVille = "BJM";
        if (agence != null && !string.IsNullOrEmpty(agence.Code))
        {
            codeVille = agence.Code.Replace("AKR-", "");
        }
        
        Random rnd = new Random();
        string uniqueCode;
        bool existe;

        do
        {
            int suffix = rnd.Next(10000, 99999);
            uniqueCode = $"1000{codeVille}{suffix}";
            existe = await _context.Comptes.AnyAsync(c => c.NumeroCompte == uniqueCode);
        } while (existe);

        return uniqueCode;
    }

    // AJOUT COMPLÉMENTAIRE : Implémentation obligatoire de DeleteAsync pour concorder avec l'ICompteService et le contrôleur
    public async Task<bool> DeleteAsync(int id)
    {
        var compte = await _context.Comptes.FindAsync(id);
        if (compte is null) return false;

        // Règle de sécurité : Vérifier si des écritures financières existent avant de supprimer
        var aDesEcritures = await _context.Ecritures.AnyAsync(e => e.CompteId == id);
        if (aDesEcritures)
        {
            throw new InvalidOperationException(
                "Impossible de supprimer ce compte : des écritures comptables y sont déjà rattachées. Veuillez utiliser l'option Clôturer.");
        }

        _context.Comptes.Remove(compte);
        await _context.SaveChangesAsync();
        return true;
    }
}
