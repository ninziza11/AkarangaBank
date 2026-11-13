using Microsoft.AspNetCore.Identity;

namespace AkarangaBank.Web.Models;

public class ApplicationRole : IdentityRole<int>
{
    public string? Description { get; set; }

    public ApplicationRole() { }

    public ApplicationRole(string nom, string? description = null) : base(nom)
    {
        Description = description;
    }
}