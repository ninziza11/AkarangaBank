using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using AkarangaBank.Web.Data;
using AkarangaBank.Web.Models;

namespace AkarangaBank.Web.Services
{
    public class AuditService
    {
        private readonly AkarangaBankContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditService(AkarangaBankContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        // 🛡️ ENREGISTREMENT DE LA PISTE D'AUDIT BANCAIRE (Page 10)
        public async Task LogActionAsync(string action, string table, string enregistrementId, string avant, string apres)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            string ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            string userAgent = httpContext?.Request.Headers["User-Agent"].ToString() ?? "Système Automatique";

            var log = new AuditLog
            {
                DateEvenement = DateTime.Now,
                Action = action.ToUpper(),
                TableConcernee = table,
                EnregistrementId = enregistrementId,
                ValeurAvant = avant,
                ValeurApres = apres,
                AdresseIP = ipAddress,
                UserAgent = userAgent
            };

            try
            {
                _context.AuditLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                // Évite de bloquer l'opération métier principale si l'audit échoue en mode simulation [1f2]
            }
        }
    }
}
