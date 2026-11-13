using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AkarangaBank.Web.ViewModels
{
    public class CompteCreateViewModel
    {
        // Données d'identification du client rattaché
        public int ClientId { get; set; }
        public string ClientNomComplet { get; set; } = string.Empty;

        // Informations du formulaire de création
        public int TypeCompteId { get; set; }
        public int AgenceId { get; set; }
        public string Devise { get; set; } = "BIF"; // Lecture seule, configurée par défaut sur BIF

        // Listes de chargement pour les listes déroulantes (Select)
        public IEnumerable<SelectListItem> TypesCompteDisponibles { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> AgencesDisponibles { get; set; } = new List<SelectListItem>();
    }
}
