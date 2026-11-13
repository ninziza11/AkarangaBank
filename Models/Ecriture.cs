using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AkarangaBank.Web.Models
{
    [Table("Ecritures")]
    public class Ecriture
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OperationId { get; set; }

        [ForeignKey("OperationId")]
        public virtual Operation? Operation { get; set; }

        [Required]
        public int CompteId { get; set; }

        [ForeignKey("CompteId")]
        public virtual Compte? Compte { get; set; }

        [Required]
        [StringLength(10)]
        // Sens ∈ {Debit, Credit} (Page 13)
        public string Sens { get; set; } = string.Empty; 

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Montant { get; set; }

        [Required]
        [StringLength(3)]
        public string Devise { get; set; } = "BIF";

        [Required]
        public DateTime DateEcriture { get; set; } = DateTime.Now;

        [Required]
        [StringLength(250)]
        public string Libelle { get; set; } = string.Empty;
    }
}
