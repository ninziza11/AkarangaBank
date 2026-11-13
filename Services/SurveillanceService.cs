using Microsoft.EntityFrameworkCore;
using AkarangaBank.Web.Data;
using AkarangaBank.Web.Models;
namespace AkarangaBank.Web.Services;
public class SurveillanceService : ISurveillanceService
{
 private readonly AkarangaBankContext _context;
 private readonly IConformiteService _conformite;
 public SurveillanceService(
 AkarangaBankContext context,
 IConformiteService conformite)
 {
 _context = context;
 _conformite = conformite;
 }
 public async Task<ResultatSurveillance> AnalyserTransactionAsync(
 int compteId, decimal montant, string typeOperation, DateTime date)
 {
 var alertes = new List<AlerteSurveillance>();
 var compte = await _context.Comptes
 .Include(c => c.Client)
 .FirstOrDefaultAsync(c => c.Id == compteId);
 if (compte is null)
 return new ResultatSurveillance { Alertes = alertes };
 var seuilAML = await _conformite.GetSeuilAMLAsync();
 // Règle 1 : montant élevé
 if (montant >= seuilAML)
 {
 alertes.Add(new AlerteSurveillance
 {
 Regle = "MontantEleve",
 Description = $"Transaction de {montant:N0} BIF dépasse le seuil AML.",
 Niveau = NiveauAlerte.Moyen
 });
 }
 // Règle 2 : fréquence inhabituelle
 var debut = date.AddDays(-7);
 var nbTransactions = await _context.Operations
 .Where(o => o.DateOperation >= debut && o.DateOperation <= date)
 .Where(o => o.CompteId == compteId)
 .CountAsync();
 if (nbTransactions > 20)
 {
 alertes.Add(new AlerteSurveillance
 {
 Regle = "FrequenceElevee",
 Description = $"{nbTransactions} transactions sur ce compte en 7 jours.",
 Niveau = NiveauAlerte.Eleve
 });
 }
 // Règle 3 : client à risque
 var risqueClient = await _conformite.EvaluerRisqueClientAsync(compte.ClientId);
 if (risqueClient == NiveauRisqueClient.Eleve)
 {
 alertes.Add(new AlerteSurveillance
 {
 Regle = "ClientARisque",
 Description = "Le client associé est classé à risque élevé.",
 Niveau = NiveauAlerte.Eleve
 });
 }
 // Règle 4 : montant inhabituel
 var moyenne = await GetMoyenneTransactionsAsync(compteId);
 if (moyenne > 0 && montant > moyenne * 5)
 {
 alertes.Add(new AlerteSurveillance
 {
 Regle = "MontantInhabituel",
 Description = $"Montant {montant:N0} BIF > 5x la moyenne ({moyenne
:N0} BIF).",
 Niveau = NiveauAlerte.Moyen
 });
 }
 return new ResultatSurveillance
 {
 CompteId = compteId,
 DateAnalyse = DateTime.UtcNow,
 Alertes = alertes,
 NecessiteDeclaration = alertes.Any(a => a.Niveau >= NiveauAlerte.Eleve
)
 };
 }
 public async Task<NiveauRisqueClient> EvaluerRisqueClientAsync(int clientId) {
 return await _conformite.EvaluerRisqueClientAsync(clientId);
 }
 private async Task<decimal> GetMoyenneTransactionsAsync(int compteId) {
 var montants = await _context.Operations
 .Where(o => o.CompteId == compteId)
 .Select(o => o.Montant)
 .ToListAsync();
 return montants.Count > 0 ? montants.Average() : 0m;
 }
}