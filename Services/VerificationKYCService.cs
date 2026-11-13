using Microsoft.EntityFrameworkCore;
using AkarangaBank.Web.Data;
using AkarangaBank.Web.Models;
using AkarangaBank.Web.ViewModels;

namespace AkarangaBank.Web.Services;

public class VerificationKYCService : IVerificationKYCService
{
    private readonly AkarangaBankContext _context;

    public VerificationKYCService(AkarangaBankContext context)
    {
        _context = context;
    }

    public async Task<VerificationKYC> CreateAsync(VerificationKYCCreateViewModel vm, int verifieParId)
    {
        var kyc = new VerificationKYC
        {
            ClientId = vm.ClientId,
            TypeDocument = vm.TypeDocument,
            NumeroDocument = vm.NumeroDocument.Trim().ToUpperInvariant(),
            CheminDocument = vm.CheminDocument ?? "manquant.pdf",
            Statut = "EnAttente", // Statut initial réglementaire
            VerifieParId = verifieParId,
            DateVerification = DateTime.UtcNow
        };

        _context.VerificationsKYC.Add(kyc);
        
        // Optionnel : Mettre à jour le statut du client si nécessaire
        await _context.SaveChangesAsync();
        return kyc;
    }

    public async Task<bool> ValiderAsync(int id, int verifieParId)
    {
        var kyc = await _context.VerificationsKYC.FirstOrDefaultAsync(v => v.Id == id);
        if (kyc is null) return false;

        kyc.Statut = "Valide";
        kyc.VerifieParId = verifieParId;
        kyc.DateVerification = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RejeterAsync(int id, int verifieParId, string commentaire)
    {
        var kyc = await _context.VerificationsKYC.FirstOrDefaultAsync(v => v.Id == id);
        if (kyc is null) return false;

        kyc.Statut = "Rejete";
        kyc.VerifieParId = verifieParId;
        kyc.DateVerification = DateTime.UtcNow;
        kyc.Commentaire = commentaire; // Assurez-vous d'avoir cette colonne ou de la mapper dans vos logs

        await _context.SaveChangesAsync();
        return true;
    }
}
