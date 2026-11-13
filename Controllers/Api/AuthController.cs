using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using AkarangaBank.Web.Models.Api;

namespace AkarangaBank.Web.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            // Simulation de validation d'identifiants (À coupler avec votre table Utilisateurs)
            if (request.Identifiant == "mobile_agent" && request.MotDePasse == "Akaranga2026!")
            {
                var secretKey = _configuration["Jwt:Secret"] ?? "CléSecrèteParDéfautAkarangaBank2026!";
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var claims = new[]
                {
                    new Claim(ClaimTypes.Name, request.Identifiant),
                    new Claim(ClaimTypes.Role, "AgentMobile"),
                    new Claim("SystemeOrigin", "AkarangaCore")
                };

                var token = new JwtSecurityToken(
                    issuer: _configuration["Jwt:Issuer"],
                    audience: _configuration["Jwt:Audience"],
                    claims: claims,
                    expires: DateTime.Now.AddMinutes(double.Parse(_configuration["Jwt:ExpiryInMinutes"] ?? "60")),
                    signingCredentials: creds
                );

                return Ok(new
                {
                    Token = new JwtSecurityTokenHandler().WriteToken(token),
                    Expiration = token.ValidTo,
                    Statut = "Authentifié"
                });
            }

            return Unauthorized(new { Message = "Identifiant ou mot de passe incorrect pour le canal API." });
        }
    }
}
