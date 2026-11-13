using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AkarangaBank.Web.Models;

[Table("Prets")]
public class Pret
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string Reference { get; set; } = string.Empty; // 🛠️ REQUIS (Bloc 4)

    [Required]
    public int ClientId { get; set; } // 🛠️ REQUIS (Bloc 4)

    [Required]
    public int CompteDebitId { get; set; } // 🛠️ REQUIS : Le compte à prélever (Bloc 4)

    public int? ValideParId { get; set; } // 🛠️ REQUIS : ID du Gestionnaire (Bloc 4)

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal MontantPrincipal { get; set; }

    [Required]
    [Column(TypeName = "decimal(5,2)")]
    public decimal TauxInteret { get; set; }

    [Required]
    public int DureeMois { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Mensualite { get; set; }

    [Required]
    [StringLength(20)]
    // ⚠️ Valeurs autorisées par la contrainte CHECK de la BRB : EnAttente, Valide, Rejete, EnCours, Solde, Impaye
    public string Statut { get; set; } = "EnAttente"; 

    [Required]
    public DateTime DateCreation { get; set; } = DateTime.Now;

    [StringLength(500)]
    public string Remarques { get; set; } = string.Empty;
}
