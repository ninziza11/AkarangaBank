using AkarangaBank.Web.Models;
using AkarangaBank.Web.ViewModels;

namespace AkarangaBank.Web.Services;

public interface IAgenceService
{
    Task<AgenceListViewModel> GetPagedAsync(string? recherche, int page, int taille);
    Task<AgenceDetailsViewModel?> GetDetailsAsync(int id);
    Task<Agence?> GetByIdAsync(int id);
    Task<Agence> CreateAsync(AgenceCreateViewModel vm);
    Task<bool> UpdateAsync(AgenceEditViewModel vm);
    Task<bool> DeleteAsync(int id);
    Task<bool> CodeExisteAsync(string code, int? excludeId = null);
    Task<bool> APeuEtreSupprimeeAsync(int id);
}