using AkarangaBank.Web.Models;

namespace AkarangaBank.Web.ViewModels;

public class ReleveViewModel
{
    public Compte Compte { get; set; } = null!;

    public decimal SoldeInitial { get; set; }

    public decimal SoldeFinal { get; set; }

    public ICollection<Ecriture> Ecritures { get; set; } =
        new List<Ecriture>();
}