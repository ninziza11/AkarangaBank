using Microsoft.EntityFrameworkCore;
using AkarangaBank.Web.Data;
using AkarangaBank.Web.Models;
using AkarangaBank.Web.ViewModels;

namespace AkarangaBank.Web.Services;

public class ComptabiliteService : IComptabiliteService
{
    private readonly AkarangaBankContext _context;
    private readonly IConformiteService _conformite;
    private readonly ISurveillanceService _surveillance;
    private readonly IDeclarationService _declaration;

    public ComptabiliteService(
        AkarangaBankContext context,
        IConformiteService conformite,
        ISurveillanceService surveillance,
        IDeclarationService declaration)
    {
        _context = context;
        _conformite = conformite;
        _surveillance = surveillance;
        _declaration = declaration;
    }

    public async Task<Operation> DeposerAsync(
        DepotViewModel vm,
        int utilisateurId)
    {
        var compte = await _context.Comptes.FindAsync(vm.CompteId)
            ?? throw new InvalidOperationException("Compte introuvable.");

        if (compte.Statut != "Actif")
        {
            throw new InvalidOperationException(
                $"Compte {compte.Statut}. Opération impossible.");
        }

        var analyse = await _conformite.AnalyserTransactionAsync(
            vm.CompteId,
            vm.Montant,
            "Depot");

        if (analyse.EstBloquee)
        {
            throw new InvalidOperationException(analyse.Motif);
        }

        var fractionnement = await _conformite.DetecterFractionnementAsync(
            vm.CompteId,
            vm.Montant,
            DateTime.UtcNow);

        var surveillance = await _surveillance.AnalyserTransactionAsync(
            vm.CompteId,
            vm.Montant,
            "Depot",
            DateTime.UtcNow);

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            var operation = new Operation
            {
                Reference = GenererReference("AKR"),
                TypeOperation = "Depot",
                CompteId = vm.CompteId,
                UtilisateurId = utilisateurId,
                AgenceId = compte.AgenceId,
                Montant = vm.Montant,
                Devise = compte.Devise,
                Statut = "Valide",
                DateOperation = DateTime.UtcNow,
                DateValidation = DateTime.UtcNow,
                Commentaire = vm.Commentaire?.Trim()
            };

            _context.Operations.Add(operation);
            await _context.SaveChangesAsync();

            var ecriture = new Ecriture
            {
                OperationId = operation.Id,
                CompteId = vm.CompteId,
                Sens = "Credit",
                Montant = vm.Montant,
                Devise = compte.Devise,
                DateEcriture = DateTime.UtcNow,
                Libelle = $"Dépôt sur compte {compte.NumeroCompte}"
            };

            _context.Ecritures.Add(ecriture);

            compte.Solde += vm.Montant;
            compte.DateModification = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            await CreerAlertesSiNecessaireAsync(
                compte.ClientId,
                operation.Id,
                analyse,
                fractionnement,
                surveillance);

            return operation;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<Operation> RetirerAsync(
        RetraitViewModel vm,
        int utilisateurId)
    {
        var compte = await _context.Comptes.FindAsync(vm.CompteId)
            ?? throw new InvalidOperationException("Compte introuvable.");

        if (compte.Statut != "Actif")
        {
            throw new InvalidOperationException(
                $"Compte {compte.Statut}. Opération impossible.");
        }

        if (compte.Solde < vm.Montant)
        {
            throw new InvalidOperationException("Solde insuffisant.");
        }

        var analyse = await _conformite.AnalyserTransactionAsync(
            vm.CompteId,
            vm.Montant,
            "Retrait");

        if (analyse.EstBloquee)
        {
            throw new InvalidOperationException(analyse.Motif);
        }

        var fractionnement = await _conformite.DetecterFractionnementAsync(
            vm.CompteId,
            vm.Montant,
            DateTime.UtcNow);

        var surveillance = await _surveillance.AnalyserTransactionAsync(
            vm.CompteId,
            vm.Montant,
            "Retrait",
            DateTime.UtcNow);

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            var operation = new Operation
            {
                Reference = GenererReference("AKR"),
                TypeOperation = "Retrait",
                CompteId = vm.CompteId,
                UtilisateurId = utilisateurId,
                AgenceId = compte.AgenceId,
                Montant = vm.Montant,
                Devise = compte.Devise,
                Statut = "Valide",
                DateOperation = DateTime.UtcNow,
                DateValidation = DateTime.UtcNow,
                Commentaire = vm.Commentaire?.Trim()
            };

            _context.Operations.Add(operation);
            await _context.SaveChangesAsync();

            var ecriture = new Ecriture
            {
                OperationId = operation.Id,
                CompteId = vm.CompteId,
                Sens = "Debit",
                Montant = vm.Montant,
                Devise = compte.Devise,
                DateEcriture = DateTime.UtcNow,
                Libelle = $"Retrait du compte {compte.NumeroCompte}"
            };

            _context.Ecritures.Add(ecriture);

            compte.Solde -= vm.Montant;
            compte.DateModification = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            await CreerAlertesSiNecessaireAsync(
                compte.ClientId,
                operation.Id,
                analyse,
                fractionnement,
                surveillance);

            return operation;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<Operation> VirerAsync(
        VirementViewModel vm,
        int utilisateurId)
    {
        if (vm.CompteSourceId == vm.CompteDestinationId)
        {
            throw new InvalidOperationException(
                "Les comptes source et destination doivent être différents.");
        }

        var source = await _context.Comptes.FindAsync(vm.CompteSourceId)
            ?? throw new InvalidOperationException("Compte source introuvable.");

        var destination = await _context.Comptes.FindAsync(vm.CompteDestinationId)
            ?? throw new InvalidOperationException("Compte destination introuvable.");

        if (source.Statut != "Actif" || destination.Statut != "Actif")
        {
            throw new InvalidOperationException(
                "Les deux comptes doivent être actifs.");
        }

        if (source.Devise != destination.Devise)
        {
            throw new InvalidOperationException(
                "Les devises des deux comptes doivent être identiques.");
        }

        if (source.Solde < vm.Montant)
        {
            throw new InvalidOperationException(
                "Solde insuffisant sur le compte source.");
        }

        var analyse = await _conformite.AnalyserTransactionAsync(
            vm.CompteSourceId,
            vm.Montant,
            "Virement");

        if (analyse.EstBloquee)
        {
            throw new InvalidOperationException(analyse.Motif);
        }

        var fractionnement = await _conformite.DetecterFractionnementAsync(
            vm.CompteSourceId,
            vm.Montant,
            DateTime.UtcNow);

        var surveillance = await _surveillance.AnalyserTransactionAsync(
            vm.CompteSourceId,
            vm.Montant,
            "Virement",
            DateTime.UtcNow);

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            var virement = new Virement
            {
                Reference = GenererReference("VIR"),
                CompteSourceId = vm.CompteSourceId,
                CompteDestinationId = vm.CompteDestinationId,
                UtilisateurId = utilisateurId,
                Montant = vm.Montant,
                Devise = source.Devise,
                Statut = "Valide",
                Motif = vm.Motif?.Trim(),
                ControleAML = analyse.NecessiteVigilanceRenforcee || fractionnement,
                DateVirement = DateTime.UtcNow,
                DateValidation = DateTime.UtcNow
            };

            _context.Virements.Add(virement);
            await _context.SaveChangesAsync();

            var operation = new Operation
            {
                Reference = virement.Reference,
                TypeOperation = "Virement",
                CompteId = vm.CompteSourceId,
                UtilisateurId = utilisateurId,
                AgenceId = source.AgenceId,
                VirementId = virement.Id,
                Montant = vm.Montant,
                Devise = source.Devise,
                Statut = "Valide",
                DateOperation = DateTime.UtcNow,
                DateValidation = DateTime.UtcNow,
                Commentaire = vm.Motif?.Trim()
            };

            _context.Operations.Add(operation);
            await _context.SaveChangesAsync();

            var debit = new Ecriture
            {
                OperationId = operation.Id,
                CompteId = vm.CompteSourceId,
                Sens = "Debit",
                Montant = vm.Montant,
                Devise = source.Devise,
                DateEcriture = DateTime.UtcNow,
                Libelle = $"Virement sortant vers {destination.NumeroCompte}"
            };

            var credit = new Ecriture
            {
                OperationId = operation.Id,
                CompteId = vm.CompteDestinationId,
                Sens = "Credit",
                Montant = vm.Montant,
                Devise = destination.Devise,
                DateEcriture = DateTime.UtcNow,
                Libelle = $"Virement entrant depuis {source.NumeroCompte}"
            };

            _context.Ecritures.Add(debit);
            _context.Ecritures.Add(credit);

            VerifierEquilibre(new[] { debit, credit });

            source.Solde -= vm.Montant;
            destination.Solde += vm.Montant;

            source.DateModification = DateTime.UtcNow;
            destination.DateModification = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            await CreerAlertesSiNecessaireAsync(
                source.ClientId,
                operation.Id,
                analyse,
                fractionnement,
                surveillance);

            return operation;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<JournalViewModel> GetJournalAsync(
        DateTime? du,
        DateTime? au,
        string? typeOperation,
        int page,
        int taille)
    {
        var query = _context.Operations
            .AsNoTracking()
            .Include(o => o.Compte)
            .Include(o => o.Utilisateur)
            .Include(o => o.Ecritures)
            .AsQueryable();

        if (du.HasValue)
        {
            query = query.Where(o => o.DateOperation >= du.Value);
        }

        if (au.HasValue)
        {
            query = query.Where(o => o.DateOperation <= au.Value);
        }

        if (!string.IsNullOrWhiteSpace(typeOperation))
        {
            query = query.Where(o => o.TypeOperation == typeOperation);
        }

        var total = await query.CountAsync();

        var operations = await query
            .OrderByDescending(o => o.DateOperation)
            .Skip((page - 1) * taille)
            .Take(taille)
            .ToListAsync();

        return new JournalViewModel
        {
            Operations = operations,
            Du = du,
            Au = au,
            TypeOperation = typeOperation,
            Page = page,
            TaillePage = taille,
            Total = total
        };
    }

    public async Task<ReleveViewModel?> GetReleveAsync(int compteId)
    {
        var compte = await _context.Comptes
            .AsNoTracking()
            .Include(c => c.Client)
            .FirstOrDefaultAsync(c => c.Id == compteId);

        if (compte == null)
        {
            return null;
        }

        var ecritures = await _context.Ecritures
            .AsNoTracking()
            .Include(e => e.Operation)
            .Where(e => e.CompteId == compteId)
            .OrderBy(e => e.DateEcriture)
            .ToListAsync();

        var totalCredits = ecritures
            .Where(e => e.Sens == "Credit")
            .Sum(e => e.Montant);

        var totalDebits = ecritures
            .Where(e => e.Sens == "Debit")
            .Sum(e => e.Montant);

        var soldeInitial = compte.Solde - totalCredits + totalDebits;

        return new ReleveViewModel
        {
            Compte = compte,
            SoldeInitial = soldeInitial,
            SoldeFinal = compte.Solde,
            Ecritures = ecritures
        };
    }

    public async Task<decimal> GetSoldeAsync(int compteId)
    {
        var compte = await _context.Comptes
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == compteId)
            ?? throw new InvalidOperationException("Compte introuvable.");

        return compte.Solde;
    }

    public async Task<bool> VerifierEquilibreGlobalAsync()
    {
        var desequilibres = await _context.Ecritures
            .GroupBy(e => e.OperationId)
            .Select(g => new
            {
                OperationId = g.Key,
                Diff = g.Sum(e =>
                    e.Sens == "Debit" ? e.Montant : -e.Montant)
            })
            .Where(x => x.Diff != 0)
            .ToListAsync();

        return desequilibres.Count == 0;
    }

    private async Task CreerAlertesSiNecessaireAsync(
        int clientId,
        int operationId,
        ResultatConformite analyse,
        bool fractionnement,
        ResultatSurveillance surveillance)
    {
        if (analyse.NecessiteDeclaration)
        {
            await _declaration.CreerAlerteAsync(
                clientId,
                operationId,
                "DepassementSeuil",
                analyse.Niveau.ToString(),
                analyse.Motif ?? "Dépassement du seuil AML.");
        }

        if (fractionnement)
        {
            await _declaration.CreerAlerteAsync(
                clientId,
                operationId,
                "Fractionnement",
                "Eleve",
                "Suspicion de fractionnement : plusieurs opérations juste en dessous du seuil sur une courte période.");
        }

        foreach (var alerte in surveillance.Alertes)
        {
            await _declaration.CreerAlerteAsync(
                clientId,
                operationId,
                alerte.Regle,
                alerte.Niveau.ToString(),
                alerte.Description);
        }
    }

    private static string GenererReference(string prefix)
    {
        var date = DateTime.UtcNow.ToString("yyyyMMdd");
        var random = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();

        return $"{prefix}-{date}-{random}";
    }

    private static void VerifierEquilibre(IEnumerable<Ecriture> ecritures)
    {
        var totalDebit = ecritures
            .Where(e => e.Sens == "Debit")
            .Sum(e => e.Montant);

        var totalCredit = ecritures
            .Where(e => e.Sens == "Credit")
            .Sum(e => e.Montant);

        if (totalDebit != totalCredit)
        {
            throw new InvalidOperationException(
                $"Écriture déséquilibrée : débit={totalDebit:N2}, crédit={totalCredit:N2}.");
        }
    }
}