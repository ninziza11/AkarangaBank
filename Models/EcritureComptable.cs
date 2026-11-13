using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AkarangaBank.Web.Models
{
    public class EcritureComptable
    {
        public int Id { get; set; }

        [Required]
        public string NumeroPiece { get; set; } = string.Empty; // Identifiant unique du virement/opération

        public int CompteId { get; set; }
        [ForeignKey("CompteId")]
        public virtual Compte? Compte { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Debit { get; set; } // Augmente un compte d'actif / Diminue un compte de passif

        [Column(TypeName = "decimal(18,2)")]
        public decimal Credit { get; set; }

        [Required]
        public string LibelleOperation { get; set; } = string.Empty;

        public DateTime DateComptable { get; set; } = DateTime.UtcNow;
    }
}
