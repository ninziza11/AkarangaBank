using System;
using System.ComponentModel.DataAnnotations;

namespace AkarangaBank.Web.ViewModels
{
    public class ClientKycUpdateViewModel
    {
        public int ClientId { get; set; }

        [Display(Name = "Numéro de Client")]
        public string NumeroClient { get; set; } = string.Empty;

        [Display(Name = "Nom complet du Client")]
        public string NomComplet { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le type de pièce d'identité est obligatoire.")]
        [Display(Name = "Type de pièce d'identité")]
        public string TypePieceId { get; set; } = "CNI"; // CNI, Passeport, Carte de Résident

        [Required(ErrorMessage = "Le numéro de la pièce est obligatoire.")]
        [StringLength(50)]
        [Display(Name = "Numéro de la pièce (CNI / Passeport)")]
        public string NumeroCNI { get; set; } = string.Empty;

        [Required(ErrorMessage = "La date d'expiration de la pièce est obligatoire.")]
        [DataType(DataType.Date)]
        [Display(Name = "Date d'expiration de la pièce")]
        public DateTime DateExpirationPiece { get; set; } = DateTime.Today.AddYears(5);

        [Required(ErrorMessage = "Le statut de conformité est obligatoire.")]
        [Display(Name = "Statut du dossier KYC")]
        public string StatutKyc { get; set; } = "Conforme"; // Conforme, NonConforme, Suspendu
    }
}
