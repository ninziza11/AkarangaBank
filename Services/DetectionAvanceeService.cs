using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AkarangaBank.Web.Data;
using AkarangaBank.Web.Models;

namespace AkarangaBank.Web.Services;

public class DetectionAvanceeService : IDetectionAvanceeService
{
    private readonly AkarangaBankContext _context;
    private readonly ISeuilAMLService _seuils;

    public DetectionAvanceeService(
        AkarangaBankContext context,
        ISeuilAMLService seuils)
    {
        _context = context;
        _seuils = seuils;
    }

    // Plusieurs opérations dont les montants sont proches du seuil.
    public async Task<ResultatSurveillance> DetecterStructuringAsync(
        int clientId,
        DateTime date)
    {
        var alertes = new List<AlerteSurveillance>();

        var seuil = await _seuils.GetSeuilVigilanceAsync();
        var debut = date.AddDays(-30);

        var nombreOperations = await _context.Operations
            .AsNoTracking()
            .Where(o =>
                o.DateOperation >= debut &&
                o.DateOperation <= date)
            .Where(o => o.Compte.ClientId == clientId)
            .Where(o =>
                o.Montant >= seuil * 0.8m &&
                o.Montant < seuil)
            .CountAsync();

        if (nombreOperations >= 3)
        {
            alertes.Add(new AlerteSurveillance
            {
                Regle = "Structuring",
                Description =
                    $"{nombreOperations} opérations entre " +
                    $"{seuil * 0.8m:N0} et {seuil:N0} BIF sur 30 jours. " +
                    "Suspicion de fractionnement structuré.",
                Niveau = NiveauAlerte.Eleve
            });
        }

        return new ResultatSurveillance
        {
            CompteId = 0,
            DateAnalyse = DateTime.UtcNow,
            Alertes = alertes,
            NecessiteDeclaration = alertes.Any()
        };
    }

    // Cumul d'opérations sous le seuil sur les comptes du client.
    public async Task<ResultatSurveillance> DetecterSmurfingAsync(
        int clientId,
        DateTime date)
    {
        var alertes = new List<AlerteSurveillance>();

        var seuil = await _seuils.GetSeuilVigilanceAsync();
        var debut = date.AddDays(-7);

        var comptes = await _context.Comptes
            .AsNoTracking()
            .Where(c => c.ClientId == clientId)
            .Select(c => c.Id)
            .ToListAsync();

        if (comptes.Count < 2)
        {
            return new ResultatSurveillance
            {
                CompteId = 0,
                DateAnalyse = DateTime.UtcNow,
                Alertes = alertes,
                NecessiteDeclaration = false
            };
        }

        var total = await _context.Operations
            .AsNoTracking()
            .Where(o =>
                o.DateOperation >= debut &&
                o.DateOperation <= date)
            .Where(o => comptes.Contains(o.CompteId))
            .Where(o => o.Montant < seuil)
            .SumAsync(o => (decimal?)o.Montant) ?? 0m;

        if (total >= seuil * 3)
        {
            alertes.Add(new AlerteSurveillance
            {
                Regle = "Smurfing",
                Description =
                    $"Total cumulé de {total:N0} BIF sur " +
                    $"{comptes.Count} comptes du client " +
                    "en 7 jours, chaque opération sous le seuil AML.",
                Niveau = NiveauAlerte.Critique
            });
        }

        return new ResultatSurveillance
        {
            CompteId = 0,
            DateAnalyse = DateTime.UtcNow,
            Alertes = alertes,
            NecessiteDeclaration = alertes.Any()
        };
    }

    // Nombre de virements inter-agences émis par le client.
    public async Task<ResultatSurveillance> DetecterLayeringAsync(
        int clientId,
        DateTime date)
    {
        var alertes = new List<AlerteSurveillance>();
        var debut = date.AddDays(-14);

        var nombreVirements = await _context.VirementsInterAgences
            .AsNoTracking()
            .Where(v =>
                v.DateCreation >= debut &&
                v.DateCreation <= date)
            .Where(v =>
                v.Virement.CompteSource.ClientId == clientId)
            .CountAsync();

        if (nombreVirements >= 5)
        {
            alertes.Add(new AlerteSurveillance
            {
                Regle = "Layering",
                Description =
                    $"{nombreVirements} virements inter-agences en 14 jours. " +
                    "Schéma possible de superposition.",
                Niveau = NiveauAlerte.Eleve
            });
        }

        return new ResultatSurveillance
        {
            CompteId = 0,
            DateAnalyse = DateTime.UtcNow,
            Alertes = alertes,
            NecessiteDeclaration = alertes.Any()
        };
    }

    // Nombre élevé d'opérations sur une période d'une heure.
    public async Task<ResultatSurveillance> DetecterVelociteAnormaleAsync(
        int clientId,
        DateTime date)
    {
        var alertes = new List<AlerteSurveillance>();
        var debut = date.AddHours(-1);

        var nombreOperations = await _context.Operations
            .AsNoTracking()
            .Where(o =>
                o.DateOperation >= debut &&
                o.DateOperation <= date)
            .Where(o => o.Compte.ClientId == clientId)
            .CountAsync();

        if (nombreOperations >= 10)
        {
            alertes.Add(new AlerteSurveillance
            {
                Regle = "VelociteAnormale",
                Description =
                    $"{nombreOperations} opérations en 1 heure. " +
                    "Comportement inhabituel.",
                Niveau = NiveauAlerte.Eleve
            });
        }

        return new ResultatSurveillance
        {
            CompteId = 0,
            DateAnalyse = DateTime.UtcNow,
            Alertes = alertes,
            NecessiteDeclaration = alertes.Any()
        };
    }
}