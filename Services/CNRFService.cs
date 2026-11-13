using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AkarangaBank.Web.Data;
using AkarangaBank.Web.Models;

namespace AkarangaBank.Web.Services;

public class CNRFService : ICNRFService
{
    private readonly AkarangaBankContext _context;

    public CNRFService(AkarangaBankContext context)
    {
        _context = context;
    }

    public async Task<DeclarationSoupcon> GenererDOSAsync(
        int alerteId,
        int redigeParId)
    {
        var alerte = await _context.AlertesAML
            .Include(a => a.Client)
            .Include(a => a.Operation)
            .FirstOrDefaultAsync(a => a.Id == alerteId)
            ?? throw new InvalidOperationException("Alerte introuvable.");

        var reference = GenererReference("DOS");
        var contenu = ConstruireContenuDOS(alerte, reference);

        var declaration = new DeclarationSoupcon
        {
            Reference = reference,
            AlerteId = alerteId,
            ClientId = alerte.ClientId,
            TypeDeclaration = "DOS",
            Contenu = contenu,
            MotifSuspicion = alerte.Description,
            Statut = "Brouillon",
            DateCreation = DateTime.UtcNow,
            RedigeParId = redigeParId
        };

        _context.DeclarationsSoupcon.Add(declaration);
        await _context.SaveChangesAsync();

        return declaration;
    }

    public async Task<bool> TransmettreAsync(int declarationId)
    {
        var declaration = await _context.DeclarationsSoupcon
            .FindAsync(declarationId);

        if (declaration is null)
        {
            return false;
        }

        if (declaration.Statut != "Validee" &&
            declaration.Statut != "Brouillon")
        {
            return false;
        }

        // Cette méthode enregistre uniquement un changement d'état local.
        // Elle n'effectue aucun envoi vers un service externe.
        declaration.Statut = "Transmise";
        declaration.DateTransmission = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> EnregistrerAccuseAsync(
        int declarationId,
        string reponse)
    {
        var declaration = await _context.DeclarationsSoupcon
            .FindAsync(declarationId);

        if (declaration is null)
        {
            return false;
        }

        declaration.Statut = "Accusee";
        declaration.DateAccuseReception = DateTime.UtcNow;
        declaration.ReponseCNRF = reponse;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<IEnumerable<DeclarationSoupcon>> GetDeclarationsAsync(
        string? statut)
    {
        var query = _context.DeclarationsSoupcon
            .Include(d => d.Client)
            .Include(d => d.Alerte)
            .Include(d => d.RedigePar)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(statut))
        {
            query = query.Where(d => d.Statut == statut);
        }

        return await query
            .OrderByDescending(d => d.DateCreation)
            .ToListAsync();
    }

    public Task<string> GenererContenuDOSAsync(AlerteAML alerte)
    {
        var contenu = ConstruireContenuDOS(
            alerte,
            GenererReference("DOS"));

        return Task.FromResult(contenu);
    }

    private static string ConstruireContenuDOS(
        AlerteAML alerte,
        string reference)
    {
        ArgumentNullException.ThrowIfNull(alerte);

        if (alerte.Client is null)
        {
            throw new InvalidOperationException(
                "Les informations du client doivent être chargées " +
                "avant de générer la déclaration.");
        }

        var sb = new StringBuilder();

        sb.AppendLine("=====================================================");
        sb.AppendLine("DÉCLARATION D'OPÉRATION SUSPECTE (DOS)");
        sb.AppendLine("Akaranga Bank — Conformité LBC/FT");
        sb.AppendLine("=====================================================");
        sb.AppendLine();

        sb.AppendLine($"Référence déclaration : {reference}");
        sb.AppendLine(
            $"Date de génération : {DateTime.UtcNow:dd/MM/yyyy HH:mm} UTC");

        sb.AppendLine();
        sb.AppendLine("--- IDENTIFICATION DU CLIENT ---");

        sb.AppendLine(
            $"Nom complet : {alerte.Client.Nom} {alerte.Client.Prenom}");

        sb.AppendLine($"Numéro client : {alerte.Client.NumeroClient}");
        sb.AppendLine($"Numéro CNI : {alerte.Client.NumeroCNI}");
        sb.AppendLine($"Téléphone : {alerte.Client.Telephone}");

        sb.AppendLine();
        sb.AppendLine("--- OPÉRATION CONCERNÉE ---");

        if (alerte.Operation is not null)
        {
            sb.AppendLine(
                $"Référence opération : {alerte.Operation.Reference}");

            sb.AppendLine(
                $"Type : {alerte.Operation.TypeOperation}");

            sb.AppendLine(
                $"Montant : {alerte.Operation.Montant:N2} " +
                $"{alerte.Operation.Devise}");

            sb.AppendLine(
                $"Date : {alerte.Operation.DateOperation:dd/MM/yyyy HH:mm} UTC");
        }
        else
        {
            sb.AppendLine("Aucune opération associée.");
        }

        sb.AppendLine();
        sb.AppendLine("--- MOTIF DE LA DÉCLARATION ---");

        sb.AppendLine($"Type d'alerte : {alerte.TypeAlerte}");
        sb.AppendLine($"Niveau : {alerte.Niveau}");
        sb.AppendLine($"Description : {alerte.Description}");

        sb.AppendLine();
        sb.AppendLine("--- AUTORITÉ DESTINATAIRE ---");

        sb.AppendLine(
            "Cellule Nationale de Renseignement Financier (CNRF)");

        sb.AppendLine("Bujumbura, Burundi");

        sb.AppendLine();
        sb.AppendLine("--- MENTIONS LÉGALES ---");

        // Texte repris du projet ; références juridiques à vérifier
        // avant toute utilisation réelle.
        sb.AppendLine(
            "Déclaration établie conformément à la loi n° 1/02 du 4 février 2008");

        sb.AppendLine(
            "relative à la lutte contre le blanchiment de capitaux et le financement");

        sb.AppendLine(
            "du terrorisme, modifiée par la loi n° 1/08 du 27 mars 2025.");

        sb.AppendLine("=====================================================");

        return sb.ToString();
    }

    private static string GenererReference(string prefix)
    {
        var date = DateTime.UtcNow.ToString("yyyyMMdd");
        var random = Guid.NewGuid()
            .ToString("N")[..6]
            .ToUpperInvariant();

        return $"{prefix}-{date}-{random}";
    }
}