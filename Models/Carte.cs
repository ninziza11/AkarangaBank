using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AkarangaBank.Web.Models
{
    [Table("Cartes")]
    public class Carte
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(16)]
        public string NumeroCarte { get; set; } = string.Empty;

        [Required]
        public int CompteId { get; set; }

        [ForeignKey("CompteId")]
        public virtual Compte? Compte { get; set; }

        [Required]
        public int ClientId { get; set; }

        [ForeignKey("ClientId")]
        public virtual Client? Client { get; set; }

        [Required]
        [StringLength(20)]
        // TypeCarte ∈ {Visa, MasterCard, Locale}
        public string TypeCarte { get; set; } = "Locale"; 

        [Required]
        public DateTime DateExpiration { get; set; }

        [Required]
        [StringLength(20)]
        // Statut ∈ {Active, Bloquee, Expiree}
        public string Statut { get; set; } = "Active"; 
    }
}
