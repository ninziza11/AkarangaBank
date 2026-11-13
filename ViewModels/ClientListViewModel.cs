using System.Collections.Generic;
using AkarangaBank.Web.Models;

namespace AkarangaBank.Web.ViewModels
{
    public class ClientListViewModel
    {
        // 1. La liste des clients à afficher
        public IEnumerable<Client> Clients { get; set; } = new List<Client>();

        // 2. Les filtres et la recherche
        public string? Recherche { get; set; }
        public int? AgenceId { get; set; }
        public string? Statut { get; set; }

        // 3. La pagination
        public int Page { get; set; } = 1;
        public int TaillePage { get; set; } = 10;
        public int Total { get; set; }

        // CORRECTION ICI : Votre service envoie des objets 'Agence' directement !
        public IEnumerable<Agence> Agences { get; set; } = new List<Agence>();
    }
}
