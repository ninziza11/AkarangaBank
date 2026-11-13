using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AkarangaBank.Web.Models
{
    [Table("Chequiers")]
    public class Chequier
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CompteId { get; set; }

        [ForeignKey("CompteId")]
        public virtual Compte? Compte { get; set; } // 🛠️ FIXÉ : Relation demandée par l'index et les détails

        [Required]
        public string NumeroDebut { get; set; } = string.Empty; // 🛠️ FIXÉ

        [Required]
        public string NumeroFin { get; set; } = string.Empty; // 🛠️ FIXÉ

        public int NombreFeuillets { get; set; }

        [Required]
        public string Statut { get; set; } = "EnAttente"; // EnAttente, Actif, Opposition, Cloture

        public DateTime DateDemande { get; set; } = DateTime.Now; // 🛠️ FIXÉ

        public DateTime? DateRemise { get; set; } // 🛠️ FIXÉ
    }
}
