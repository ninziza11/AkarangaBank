using AkarangaBank.Web.Data;
using AkarangaBank.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AkarangaBank.Web.Data.Interceptors;

public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditInterceptor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            AjouterEntreesAudit(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
        {
            AjouterEntreesAudit(eventData.Context);
        }

        return base.SavingChanges(eventData, result);
    }

    private void AjouterEntreesAudit(DbContext context)
    {
        var utilisateur = ObtenirUtilisateur();
        var adresseIp = ObtenirAdresseIp();
        var userAgent = ObtenirUserAgent();

        var entrees = context.ChangeTracker
            .Entries()
            .Where(e =>
                e.State == EntityState.Added ||
                e.State == EntityState.Modified ||
                e.State == EntityState.Deleted)
            .Where(e => e.Entity is not AuditLog)
            .ToList();

        foreach (var entree in entrees)
        {
            var auditLog = new AuditLog
            {
                Utilisateur = utilisateur,
                Action = ObtenirAction(entree.State),
                TableConcernee = entree.Entity.GetType().Name,
                EnregistrementId = ObtenirEnregistrementId(entree),
                DateEvenement = DateTime.UtcNow,
                AdresseIP = adresseIp,
                UserAgent = userAgent
            };

            context.Set<AuditLog>().Add(auditLog);
        }
    }

    private string ObtenirUtilisateur()
    {
        var utilisateur = _httpContextAccessor.HttpContext?.User;

        if (utilisateur is null ||
            utilisateur.Identity is null ||
            !utilisateur.Identity.IsAuthenticated)
        {
            return "Système";
        }

        return utilisateur.Identity.Name ?? "Système";
    }

    private string? ObtenirAdresseIp()
    {
        return _httpContextAccessor.HttpContext?
            .Connection
            .RemoteIpAddress?
            .ToString();
    }

    private string? ObtenirUserAgent()
    {
        return _httpContextAccessor.HttpContext?
            .Request
            .Headers["User-Agent"]
            .ToString();
    }

    private static string? ObtenirEnregistrementId(
        EntityEntry entree)
    {
        var proprieteId = entree.Properties
            .FirstOrDefault(p =>
                p.Metadata.Name == "Id" ||
                p.Metadata.Name.EndsWith("Id"));

        if (proprieteId?.CurrentValue is null)
        {
            return null;
        }

        return proprieteId.CurrentValue.ToString();
    }

    private static string ObtenirAction(EntityState etat)
    {
        return etat switch
        {
            EntityState.Added => "Creation",
            EntityState.Modified => "Modification",
            EntityState.Deleted => "Suppression",
            _ => "Inconnue"
        };
    }
}