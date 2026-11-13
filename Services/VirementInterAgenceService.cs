using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AkarangaBank.Web.Data;
using AkarangaBank.Web.Models;
using AkarangaBank.Web.ViewModels;

namespace AkarangaBank.Web.Services;

public class VirementInterAgenceService : IVirementInterAgenceService
{
    private readonly AkarangaBankContext _context;
    private readonly IConformiteService _conformite;

    public VirementInterAgenceService(
        AkarangaBankContext context,
        IConformiteService conformite)
    {
        _context = context;
        _conformite = conformite;
    }

    // ======================================================
    // 1. Initier un virement inter-agences
    // ======================================================

    public async Task<VirementInterAgence> InitierAsync(
        VirementInterAgenceViewModel vm,
        int utilisateurId)
    {
        if (vm.Montant <= 0)
        {
            throw new InvalidOperationException(
                "Le montant doit être strictement positif.");
        }

        var source = await _context.Comptes
            .Include(c => c.Agence)
            .FirstOrDefaultAsync(c => c.Id == vm.CompteSourceId)
            ?? throw new InvalidOperationException(
                "Compte source introuvable.");

        var destination = await _context.Comptes
            .Include(c => c.Agence)
            .FirstOrDefaultAsync(c => c.Id == vm.CompteDestinationId)
            ?? throw new InvalidOperationException(
                "Compte destination introuvable.");

        if (source.AgenceId == destination.AgenceId)
        {
            throw new InvalidOperationException(
                "Pour un virement intra-agence, " +
                "utilisez le virement standard.");
        }

        VerifierComptes(source, destination, vm.Montant);

        var analyse = await _conformite.AnalyserTransactionAsync(
            vm.CompteSourceId,
            vm.Montant,
            "VirementInterAgence");

        if (analyse.EstBloquee)
        {
            throw new InvalidOperationException(
                analyse.Motif
                ?? "Virement bloqué par le contrôle de conformité.");
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            var maintenant = DateTime.UtcNow;

            var virement = new Virement
            {
                Reference = GenererReference("VIR"),
                CompteSourceId = source.Id,
                CompteDestinationId = destination.Id,
                UtilisateurId = utilisateurId,
                Montant = vm.Montant,
                Devise = source.Devise,
                Statut = "EnAttente",
                Motif = vm.Motif?.Trim(),
                ControleAML = analyse.NecessiteVigilanceRenforcee,
                DateVirement = maintenant
            };

            _context.Virements.Add(virement);
            await _context.SaveChangesAsync();

            var virementInter = new VirementInterAgence
            {
                Reference = virement.Reference,
                VirementId = virement.Id,
                AgenceSourceId = source.AgenceId,
                AgenceDestinationId = destination.AgenceId,
                Montant = vm.Montant,
                Devise = source.Devise,
                Statut = "EnAttente",
                DateCreation = maintenant
            };

            _context.VirementsInterAgences.Add(virementInter);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return virementInter;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // ======================================================
    // 2. Valider et comptabiliser le virement
    // ======================================================

    public async Task<bool> ValiderAsync(
        int virementInterAgenceId,
        int valideParId)
    {
        var virementInter = await _context.VirementsInterAgences
            .Include(v => v.Virement)
                .ThenInclude(v => v.CompteSource)
                    .ThenInclude(c => c.Agence)
            .Include(v => v.Virement)
                .ThenInclude(v => v.CompteDestination)
                    .ThenInclude(c => c.Agence)
            .FirstOrDefaultAsync(
                v => v.Id == virementInterAgenceId);

        if (virementInter is null)
        {
            return false;
        }

        if (virementInter.Statut != "EnAttente")
        {
            return false;
        }

        var virement = virementInter.Virement
            ?? throw new InvalidOperationException(
                "Le virement associé est introuvable.");

        var source = virement.CompteSource
            ?? throw new InvalidOperationException(
                "Compte source introuvable.");

        var destination = virement.CompteDestination
            ?? throw new InvalidOperationException(
                "Compte destination introuvable.");

        VerifierComptes(
            source,
            destination,
            virementInter.Montant);

        if (source.Agence is null || destination.Agence is null)
        {
            throw new InvalidOperationException(
                "Les agences des comptes sont introuvables.");
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            var compteCompensationSourceId =
                await GetCompteCompensationIdAsync(source.AgenceId);

            var compteCompensationDestinationId =
                await GetCompteCompensationIdAsync(destination.AgenceId);

            var maintenant = DateTime.UtcNow;

            var operation = new Operation
            {
                Reference = virementInter.Reference,
                TypeOperation = "VirementInterAgence",
                CompteId = source.Id,
                UtilisateurId = valideParId,
                AgenceId = source.AgenceId,
                VirementId = virementInter.VirementId,
                Montant = virementInter.Montant,
                Devise = virementInter.Devise,
                Statut = "Valide",
                DateOperation = maintenant,
                DateValidation = maintenant
            };

            _context.Operations.Add(operation);
            await _context.SaveChangesAsync();

            // Écriture 1 : débit du compte client source.
            var debitClient = new Ecriture
            {
                OperationId = operation.Id,
                CompteId = source.Id,
                Sens = "Debit",
                Montant = virementInter.Montant,
                Devise = source.Devise,
                DateEcriture = maintenant,
                Libelle =
                    $"Virement inter-agence vers " +
                    $"{destination.NumeroCompte}"
            };

            // Écriture 2 : crédit de la compensation source.
            var creditCompSource = new Ecriture
            {
                OperationId = operation.Id,
                CompteId = compteCompensationSourceId,
                Sens = "Credit",
                Montant = virementInter.Montant,
                Devise = source.Devise,
                DateEcriture = maintenant,
                Libelle =
                    $"Compensation agence source " +
                    $"{source.Agence.Code}"
            };

            // Écriture 3 : débit de la compensation destination.
            var debitCompDestination = new Ecriture
            {
                OperationId = operation.Id,
                CompteId = compteCompensationDestinationId,
                Sens = "Debit",
                Montant = virementInter.Montant,
                Devise = destination.Devise,
                DateEcriture = maintenant,
                Libelle =
                    $"Compensation agence destination " +
                    $"{destination.Agence.Code}"
            };

            // Écriture 4 : crédit du compte client destination.
            var creditClient = new Ecriture
            {
                OperationId = operation.Id,
                CompteId = destination.Id,
                Sens = "Credit",
                Montant = virementInter.Montant,
                Devise = destination.Devise,
                DateEcriture = maintenant,
                Libelle =
                    $"Virement inter-agence depuis " +
                    $"{source.NumeroCompte}"
            };

            _context.Ecritures.AddRange(
                debitClient,
                creditCompSource,
                debitCompDestination,
                creditClient);

            source.Solde -= virementInter.Montant;
            destination.Solde += virementInter.Montant;

            source.DateModification = maintenant;
            destination.DateModification = maintenant;

            virementInter.Statut = "EnCompensation";
            virementInter.ValideParId = valideParId;
            virementInter.DateCompensation = maintenant;

            virement.Statut = "Valide";
            virement.DateValidation = maintenant;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // ======================================================
    // 3. Rejeter un virement en attente
    // ======================================================

    public async Task<bool> RejeterAsync(
        int virementInterAgenceId,
        int valideParId,
        string motif)
    {
        if (string.IsNullOrWhiteSpace(motif))
        {
            throw new InvalidOperationException(
                "Le motif de rejet est obligatoire.");
        }

        var virementInter = await _context.VirementsInterAgences
            .Include(v => v.Virement)
            .FirstOrDefaultAsync(
                v => v.Id == virementInterAgenceId);

        if (virementInter is null)
        {
            return false;
        }

        if (virementInter.Statut != "EnAttente")
        {
            return false;
        }

        if (virementInter.Virement is null)
        {
            throw new InvalidOperationException(
                "Le virement associé est introuvable.");
        }

        virementInter.Statut = "Rejete";
        virementInter.MotifRejet = motif.Trim();
        virementInter.ValideParId = valideParId;

        virementInter.Virement.Statut = "Rejete";

        await _context.SaveChangesAsync();

        return true;
    }

    // ======================================================
    // 4. Virements en attente de l'agence connectée
    // Affiche les virements entrants ET sortants.
    // ======================================================

    public async Task<IEnumerable<VirementInterAgence>> GetEnAttenteAsync(
        int agenceId)
    {
        return await _context.VirementsInterAgences
            .AsNoTracking()
            .Include(v => v.AgenceSource)
            .Include(v => v.AgenceDestination)
            .Where(v => v.Statut == "EnAttente")
            .Where(v =>
                v.AgenceSourceId == agenceId ||
                v.AgenceDestinationId == agenceId)
            .OrderByDescending(v => v.DateCreation)
            .ToListAsync();
    }

    // ======================================================
    // 5. Historique des virements de l'agence connectée
    // ======================================================

    public async Task<IEnumerable<VirementInterAgence>> GetHistoriqueAsync(
        int agenceId,
        DateTime? du,
        DateTime? au)
    {
        var query = _context.VirementsInterAgences
            .AsNoTracking()
            .Include(v => v.AgenceSource)
            .Include(v => v.AgenceDestination)
            .AsQueryable();

        if (du.HasValue)
        {
            query = query.Where(
                v => v.DateCreation >= du.Value);
        }

        if (au.HasValue)
        {
            query = query.Where(
                v => v.DateCreation <= au.Value);
        }

        return await query
            .Where(v =>
                v.AgenceSourceId == agenceId ||
                v.AgenceDestinationId == agenceId)
            .OrderByDescending(v => v.DateCreation)
            .ToListAsync();
    }

    // ======================================================
    // 6. Recherche d'un compte de compensation
    // ======================================================

    private async Task<int> GetCompteCompensationIdAsync(int agenceId)
    {
        var numeroCompte = $"9000-{agenceId:D4}";

        var compte = await _context.Comptes
            .FirstOrDefaultAsync(
                c => c.NumeroCompte == numeroCompte);

        if (compte is null)
        {
            throw new InvalidOperationException(
                $"Compte de compensation {numeroCompte} introuvable. " +
                "Créez-le avec un client et un type de compte valides " +
                "avant de valider le virement.");
        }

        if (compte.AgenceId != agenceId ||
            compte.Statut != "Actif")
        {
            throw new InvalidOperationException(
                $"Le compte de compensation {numeroCompte} " +
                "doit être actif et rattaché à l'agence concernée.");
        }

        return compte.Id;
    }

    // ======================================================
    // 7. Vérification des comptes et du montant
    // ======================================================

    private static void VerifierComptes(
        Compte source,
        Compte destination,
        decimal montant)
    {
        if (montant <= 0)
        {
            throw new InvalidOperationException(
                "Le montant doit être strictement positif.");
        }

        if (source.Statut != "Actif" ||
            destination.Statut != "Actif")
        {
            throw new InvalidOperationException(
                "Les deux comptes doivent être actifs.");
        }

        if (source.Devise != destination.Devise)
        {
            throw new InvalidOperationException(
                "Les devises des deux comptes doivent être identiques.");
        }

        if (source.Solde < montant)
        {
            throw new InvalidOperationException(
                "Solde insuffisant sur le compte source.");
        }
    }

    // ======================================================
    // 8. Génération d'une référence
    // ======================================================

    private static string GenererReference(string prefix)
    {
        var date = DateTime.UtcNow.ToString("yyyyMMdd");

        var random = Guid.NewGuid()
            .ToString("N")[..6]
            .ToUpperInvariant();

        return $"{prefix}-{date}-{random}";
    }
}