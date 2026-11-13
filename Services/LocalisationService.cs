using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace AkarangaBank.Web.Services
{
    public class LocalisationService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LocalisationService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private readonly Dictionary<string, Dictionary<string, string>> _dictionnaire = new()
        {
            { "fr", new() {
                { "Bienvenutb", "Tableau de Bord Principal" },
                { "Clients", "Gestion des Clients" },
                { "Comptes", "Comptes Bancaires" },
                { "Virements", "Virements" },
                { "Deconnexion", "Déconnexion" },
                { "AlerteAml", "Vigilance LAB/CFT" },
                { "SessionNonConnecte", "Session Non Connectée" }
            }},
            { "rn", new() {
                { "Bienvenutb", "Ibiharuro Bikuru vy'Ibanki" },
                { "Clients", "Kugenzura Abanywanyi" },
                { "Comptes", "Amakonti y'Ibanki" },
                { "Virements", "Kurungika Mahera" },
                { "Deconnexion", "Gusohoka" },
                { "AlerteAml", "Umutekano w'Ibanki" },
                { "SessionNonConnecte", "Ntiwinjiye mu muhora" }
            }}
        };

        public string T(string cle)
        {
            var context = _httpContextAccessor.HttpContext;
            string langue = context?.Request.Cookies["Akaranga_Culture"] ?? "fr";
            
            if (_dictionnaire.ContainsKey(langue) && _dictionnaire[langue].ContainsKey(cle))
            {
                return _dictionnaire[langue][cle];
            }
            return cle;
        }
    }
}
