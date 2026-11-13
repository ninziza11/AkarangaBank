using System.Collections.Generic;
using AkarangaBank.Web.Models;

namespace AkarangaBank.Web.ViewModels
{
    public class AuditIndexViewModel
    {
        public IEnumerable<AuditLog> Logs { get; set; } = new List<AuditLog>();
        public string? RechercheUtilisateur { get; set; }
        public string? FiltreTable { get; set; }
    }
}
