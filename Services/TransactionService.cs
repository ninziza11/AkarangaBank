using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AkarangaBank.Web.Data;
using AkarangaBank.Web.Models;

namespace AkarangaBank.Web.Services
{
    // 🛠️ DÉCLARATION DU TYPE PUBLIC AU SOMMET POUR RÉSOUDRE L'ERREUR CS0246 [1f2]
    public class TransactionResult
    {
        public bool Succeeded { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class TransactionService
    {
        private readonly AkarangaBankContext _context;
        private readonly AuditService _auditService;

        public TransactionService(AkarangaBankContext context, AuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        // CORRECTIF INTÉGRI-SÉCURITÉ : Surcharge d'inversion automatique des types pour stabiliser GuichetController
        public async Task<TransactionResult> ExecuterOperationGuichetAsync(string numeroCompte, string typeOperation, decimal montant, string libelleManuel, object? argumentOptionnel)
        {
            return await ExecuterOperationGuichetAsync(numeroCompte, montant, typeOperation, libelleManuel);
        }

        // SURCHARGE À 5 ARGUMENTS STANDARD
        public async Task<TransactionResult> ExecuterOperationGuichetAsync(string numeroCompte, decimal montant, string typeOperation, string libelleManuel, object? argumentOptionnel)
        {
            return await ExecuterOperationGuichetAsync(numeroCompte, montant, typeOperation, libelleManuel);
        }

        // ⚡ MOTEUR TRANSACTIONNEL ATOMIQUE (ACID) POUR LES OPÉRATIONS DE GUICHET (Dépôts / Retraits)
        public async Task<TransactionResult> ExecuterOperationGuichetAsync(string numeroCompte, decimal montant, string typeOperation, string libelleManuel)
        {
            if (montant <= 0) 
                return new TransactionResult { Succeeded = false, Message = "Le montant doit être supérieur à 0 BIF." };

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var compte = await _context.Comptes.FirstOrDefaultAsync(c => c.NumeroCompte == numeroCompte.Trim());
                if (compte == null)
                {
                    await transaction.RollbackAsync();
                    return new TransactionResult { Succeeded = false, Message = "Compte d'agence introuvable." };
                }

                string soldeAvant = compte.Solde.ToString();
                string sensComptable = "";

                if (typeOperation.Equals("Depot", StringComparison.OrdinalIgnoreCase) || typeOperation.Equals("Versement", StringComparison.OrdinalIgnoreCase))
                {
                    compte.Solde += montant;
                    sensComptable = "Credit"; 
                }
                else if (typeOperation.Equals("Retrait", StringComparison.OrdinalIgnoreCase))
                {
                    if (compte.Solde < montant)
                    {
                        await transaction.RollbackAsync();
                        return new TransactionResult { Succeeded = false, Message = "Provision insuffisante pour effectuer ce retrait d'espèces." };
                    }
                    compte.Solde -= montant;
                    sensComptable = "Debit"; 
                }
                else
                {
                    await transaction.RollbackAsync();
                    return new TransactionResult { Succeeded = false, Message = "Nature d'opération de guichet non prise en charge." };
                }

                _context.Comptes.Update(compte);

                var operation = new Operation
                {
                    Reference = $"GCH-{DateTime.Now.Year}-{new Random().Next(10000, 99999)}",
                    TypeOperation = typeOperation,
                    CompteId = compte.Id,
                    Montant = montant,
                    Devise = "BIF",
                    Statut = "Valide",
                    DateCreation = DateTime.Now
                };
                _context.Operations.Add(operation);
                await _context.SaveChangesAsync(); 

                var ecriture = new Ecriture
                {
                    OperationId = operation.Id,
                    CompteId = compte.Id,
                    Sens = sensComptable,
                    Montant = montant,
                    Devise = "BIF",
                    DateEcriture = DateTime.Now,
                    Libelle = string.IsNullOrEmpty(libelleManuel) ? $"{typeOperation} d'espèces au guichet" : libelleManuel
                };
                _context.Ecritures.Add(ecriture);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                await _auditService.LogActionAsync(typeOperation, "Comptes", compte.Id.ToString(), soldeAvant, compte.Solde.ToString());

                return new TransactionResult 
                { 
                    Succeeded = true, 
                    Message = $"Opération de {typeOperation} de {string.Format("{0:N2}", montant)} BIF exécutée avec succès sous contrôle ACID." 
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new TransactionResult { Succeeded = false, Message = $"Échec technique de l'opération de guichet : {ex.Message}" };
            }
        }

        // ⚡ MOTEUR TRANSACTIONNEL ATOMIQUE (ACID) POUR LES VIREMENTS
        public async Task<TransactionResult> ValiderEtExecuterVirementAsync(int compteSourceId, int compteDestinationId, decimal montant)
        {
            if (montant <= 0) return new TransactionResult { Succeeded = false, Message = "Le montant doit être supérieur à 0 BIF." };

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var source = await _context.Comptes.FindAsync(compteSourceId);
                var destination = await _context.Comptes.FindAsync(compteDestinationId);

                if (source == null || destination == null)
                {
                    await transaction.RollbackAsync();
                    return new TransactionResult { Succeeded = false, Message = "Compte source ou destination introuvable." };
                }

                if (source.Solde < montant)
                {
                    await transaction.RollbackAsync();
                    return new TransactionResult { Succeeded = false, Message = "Provision insuffisante sur le compte émetteur." };
                }

                string soldeAvantSource = source.Solde.ToString();

                source.Solde -= montant;
                destination.Solde += montant;

                _context.Comptes.Update(source);
                _context.Comptes.Update(destination);

                var operation = new Operation
                {
                    Reference = $"VIR-{DateTime.Now.Year}-{new Random().Next(10000, 99999)}",
                    TypeOperation = "Virement",
                    CompteId = source.Id,
                    Montant = montant,
                    Devise = "BIF",
                    Statut = "Valide",
                    DateCreation = DateTime.Now
                };
                _context.Operations.Add(operation);
                await _context.SaveChangesAsync();

                var ecritureDebit = new Ecriture
                {
                    OperationId = operation.Id,
                    CompteId = source.Id,
                    Sens = "Debit",
                    Montant = montant,
                    Devise = "BIF",
                    DateEcriture = DateTime.Now,
                    Libelle = $"Virement émis vers le compte N° {destination.NumeroCompte}"
                };

                var ecritureCredit = new Ecriture
                {
                    OperationId = operation.Id,
                    CompteId = destination.Id,
                    Sens = "Credit",
                    Montant = montant,
                    Devise = "BIF",
                    DateEcriture = DateTime.Now,
                    Libelle = $"Virement reçu depuis le compte N° {source.NumeroCompte}"
                };

                _context.Ecritures.Add(ecritureDebit);
                _context.Ecritures.Add(ecritureCredit);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                await _auditService.LogActionAsync("VIREMENT", "Comptes", source.Id.ToString(), soldeAvantSource, source.Solde.ToString());

                return new TransactionResult 
                { 
                    Succeeded = true, 
                    Message = $"Virement de {string.Format("{0:N2}", montant)} BIF exécuté avec succès." 
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new TransactionResult { Succeeded = false, Message = $"Échec technique du virement : {ex.Message}" };
            }
        }
    }
}
