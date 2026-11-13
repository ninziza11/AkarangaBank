using System;
using System.Threading.Tasks;
using AkarangaBank.Web.Data;

namespace AkarangaBank.Web.Services
{
    public class AdminInitializer
    {
        private readonly AkarangaBankContext _context;

        public AdminInitializer(AkarangaBankContext context)
        {
            _context = context;
        }

        // 🛠️ INITIALISATION SÉCURISÉE ALLÉGÉE (La gestion réelle est opérée par le Program.cs)
        public async Task InitializeAsync()
        {
            await Task.CompletedTask;
        }
    }
}
