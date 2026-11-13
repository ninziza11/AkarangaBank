using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AkarangaBank.Web.ViewModels
{
    public class ClientEditViewModel
    {
        public int Id { get; set; }
        public string? NumeroClient { get; set; }
        public string? Nom { get; set; }
        public string? Prenom { get; set; }
        public DateTime DateNaissance { get; set; }
        public string? NumeroCNI { get; set; }
        public string? Telephone { get; set; }
        public string? Email { get; set; }
        public string? Adresse { get; set; }
        
        // CORRECTION 1 : Changé de 'int?' à 'int' pour correspondre au service
        public int AgenceId { get; set; } 
        public string? Statut { get; set; }

        // CORRECTION 2 : Changé en 'SelectListItem' pour correspondre au contrôleur
        public IEnumerable<SelectListItem> AgencesDisponibles { get; set; } = new List<SelectListItem>();
    }
}
