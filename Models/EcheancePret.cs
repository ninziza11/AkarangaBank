using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AkarangaBank.Web.Models
{
    [Table("EcheancesPret")]
    public class EcheancePret
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PretId { get; set; }

        [ForeignKey("PretId")]
        public virtual Pret? Pret { get; set; }

        [Required]
        public int NumeroEcheance { get; set; } // De 1 à DureeMois

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Capital { get; set; } // Capital amorti pour le mois

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Interet { get; set; } // Intérêt dû pour le mois

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MontantTotal { get; set; } // Mensualité constante (Capital + Intérêt)

        [Required]
        public DateTime DateEcheance { get; set; }

        public DateTime? DatePaiement { get; set; }

        [Required]
        [StringLength(20)]
        public string Statut { get; set; } = "EnAttente"; // EnAttente, Payee, EnRetard, Annulee
    }
}
