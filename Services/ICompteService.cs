using AkarangaBank.Web.Models;
using AkarangaBank.Web.ViewModels;

namespace AkarangaBank.Web.Services;

public interface ICompteService
{
    Task<Compte> CreateAsync(CompteCreateViewModel vm);
    Task<bool> ChangerStatutAsync(int id, string nouveauStatut);
    Task<string> GenererNumeroCompteUniqueAsync(int agenceId);
}
