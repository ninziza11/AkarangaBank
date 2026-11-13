using System.Linq;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AkarangaBank.Web.Models;

namespace AkarangaBank.Web.Data;

public class AkarangaBankContext
    : IdentityDbContext<ApplicationUser, ApplicationRole, int>
{
    public AkarangaBankContext(
        DbContextOptions<AkarangaBankContext> options)
        : base(options)
    {
    }

    // Users et Roles sont déjà fournis par IdentityDbContext.
    // Cet alias permet de conserver context.Utilisateurs.
    public DbSet<ApplicationUser> Utilisateurs => Set<ApplicationUser>();

    // Bloc 1 - Référentiel
    public DbSet<Agence> Agences => Set<Agence>();
    public DbSet<Parametre> Parametres => Set<Parametre>();

    // Bloc 2 - Clients et KYC
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<VerificationKYC> VerificationsKYC => Set<VerificationKYC>();

    // Bloc 3 - Comptes et cartes
    public DbSet<TypeCompte> TypesCompte => Set<TypeCompte>();
    public DbSet<Compte> Comptes => Set<Compte>();
    public DbSet<Carte> Cartes => Set<Carte>();

    // Bloc 4 - Prêts
    public DbSet<Pret> Prets => Set<Pret>();
    public DbSet<EcheancePret> EcheancesPret => Set<EcheancePret>();

    // Bloc 5 - Virements, opérations et écritures
    public DbSet<Virement> Virements => Set<Virement>();
    public DbSet<Operation> Operations => Set<Operation>();
    public DbSet<Ecriture> Ecritures => Set<Ecriture>();

    // Bloc 6 - Audit, AML et sessions
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<AlerteAML> AlertesAML => Set<AlerteAML>();
    public DbSet<SessionUtilisateur> SessionsUtilisateur =>
        Set<SessionUtilisateur>();

    // Virements inter-agences et conformité avancée
    public DbSet<VirementInterAgence> VirementsInterAgences =>
        Set<VirementInterAgence>();

    public DbSet<CompensationAgence> CompensationsAgences =>
        Set<CompensationAgence>();

    public DbSet<DeclarationSoupcon> DeclarationsSoupcon =>
        Set<DeclarationSoupcon>();

    public DbSet<SeuilAML> SeuilsAML => Set<SeuilAML>();

    // Chéquiers
    public DbSet<Chequier> Chequiers => Set<Chequier>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // =========================================================
        // TABLES IDENTITY
        // =========================================================

        modelBuilder.Entity<ApplicationUser>()
            .ToTable("Utilisateurs");

        modelBuilder.Entity<ApplicationRole>()
            .ToTable("Roles");

        modelBuilder.Entity<IdentityUserRole<int>>()
            .ToTable("UtilisateurRoles");

        modelBuilder.Entity<IdentityUserClaim<int>>()
            .ToTable("UtilisateurClaims");

        modelBuilder.Entity<IdentityUserLogin<int>>()
            .ToTable("UtilisateurLogins");

        modelBuilder.Entity<IdentityRoleClaim<int>>()
            .ToTable("RoleClaims");

        modelBuilder.Entity<IdentityUserToken<int>>()
            .ToTable("UtilisateurTokens");

        // =========================================================
        // RELATIONS DES UTILISATEURS
        // =========================================================

        modelBuilder.Entity<ApplicationUser>()
            .HasOne(u => u.Agence)
            .WithMany()
            .HasForeignKey(u => u.AgenceId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ApplicationUser>()
            .HasOne(u => u.Client)
            .WithMany()
            .HasForeignKey(u => u.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        // =========================================================
        // PRÉCISION DES MONTANTS ET TAUX
        // =========================================================

        modelBuilder.Entity<Compte>()
            .Property(c => c.Solde)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Operation>()
            .Property(o => o.Montant)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Virement>()
            .Property(v => v.Montant)
            .HasPrecision(18, 2);

        modelBuilder.Entity<TypeCompte>()
            .Property(t => t.SoldeMinimum)
            .HasPrecision(18, 2);

        modelBuilder.Entity<TypeCompte>()
            .Property(t => t.TauxInteret)
            .HasPrecision(5, 2);

        modelBuilder.Entity<Ecriture>()
            .Property(e => e.Montant)
            .HasPrecision(18, 2);

        modelBuilder.Entity<EcheancePret>()
            .Property(e => e.Capital)
            .HasPrecision(18, 2);

        modelBuilder.Entity<EcheancePret>()
            .Property(e => e.Interet)
            .HasPrecision(18, 2);

        modelBuilder.Entity<EcheancePret>()
            .Property(e => e.MontantTotal)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Pret>()
            .Property(p => p.MontantPrincipal)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Pret>()
            .Property(p => p.Mensualite)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Pret>()
            .Property(p => p.TauxInteret)
            .HasPrecision(5, 2);

        // =========================================================
        // VIREMENTS INTER-AGENCES
        // =========================================================

        modelBuilder.Entity<VirementInterAgence>(b =>
        {
            b.ToTable("VirementsInterAgences");

            b.HasKey(v => v.Id);

            b.Property(v => v.Reference)
                .HasMaxLength(30)
                .IsRequired();

            b.Property(v => v.Montant)
                .HasColumnType("decimal(18,2)");

            b.Property(v => v.Devise)
                .HasMaxLength(3)
                .HasDefaultValue("BIF");

            b.Property(v => v.Statut)
                .HasMaxLength(20)
                .HasDefaultValue("EnAttente");

            b.Property(v => v.MotifRejet)
                .HasMaxLength(500);

            b.HasIndex(v => v.Reference)
                .IsUnique();

            b.HasIndex(v => v.Statut);

            b.HasOne(v => v.Virement)
                .WithMany()
                .HasForeignKey(v => v.VirementId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(v => v.AgenceSource)
                .WithMany()
                .HasForeignKey(v => v.AgenceSourceId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(v => v.AgenceDestination)
                .WithMany()
                .HasForeignKey(v => v.AgenceDestinationId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(v => v.ValidePar)
                .WithMany()
                .HasForeignKey(v => v.ValideParId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // =========================================================
        // COMPENSATIONS ENTRE AGENCES
        // =========================================================

        modelBuilder.Entity<CompensationAgence>(b =>
        {
            b.ToTable("CompensationsAgences");

            b.HasKey(c => c.Id);

            b.Property(c => c.Reference)
                .HasMaxLength(30)
                .IsRequired();

            b.Property(c => c.TotalDebits)
                .HasColumnType("decimal(18,2)");

            b.Property(c => c.TotalCredits)
                .HasColumnType("decimal(18,2)");

            b.Property(c => c.Solde)
                .HasColumnType("decimal(18,2)");

            b.Property(c => c.Statut)
                .HasMaxLength(20)
                .HasDefaultValue("EnCours");

            b.HasIndex(c => c.DateCompensation);

            b.HasOne(c => c.Agence)
                .WithMany()
                .HasForeignKey(c => c.AgenceId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(c => c.CompteCompensation)
                .WithMany()
                .HasForeignKey(c => c.CompteCompensationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // =========================================================
        // DÉCLARATIONS DE SOUPÇON
        // =========================================================

        modelBuilder.Entity<DeclarationSoupcon>(b =>
        {
            b.ToTable("DeclarationsSoupcon");

            b.HasKey(d => d.Id);

            b.Property(d => d.Reference)
                .HasMaxLength(30)
                .IsRequired();

            b.Property(d => d.TypeDeclaration)
                .HasMaxLength(10)
                .IsRequired();

            b.Property(d => d.Contenu)
                .HasColumnType("nvarchar(max)");

            b.Property(d => d.MotifSuspicion)
                .HasMaxLength(1000);

            b.Property(d => d.Statut)
                .HasMaxLength(20)
                .HasDefaultValue("Brouillon");

            b.Property(d => d.ReponseCNRF)
                .HasMaxLength(1000);

            b.HasIndex(d => d.Reference)
                .IsUnique();

            b.HasOne(d => d.Alerte)
                .WithMany()
                .HasForeignKey(d => d.AlerteId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(d => d.Client)
                .WithMany()
                .HasForeignKey(d => d.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(d => d.RedigePar)
                .WithMany()
                .HasForeignKey(d => d.RedigeParId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // =========================================================
        // SEUILS AML
        // =========================================================

        modelBuilder.Entity<SeuilAML>(b =>
        {
            b.ToTable("SeuilsAML");

            b.HasKey(s => s.Id);

            b.Property(s => s.Code)
                .HasMaxLength(20)
                .IsRequired();

            b.Property(s => s.Libelle)
                .HasMaxLength(100)
                .IsRequired();

            b.Property(s => s.Montant)
                .HasColumnType("decimal(18,2)");

            b.Property(s => s.Devise)
                .HasMaxLength(3)
                .HasDefaultValue("BIF");

            b.Property(s => s.TypeOperation)
                .HasMaxLength(30)
                .HasDefaultValue("Toutes");

            b.HasIndex(s => s.Code);
        });

        // Conservation de ta règle globale :
        // aucune suppression automatique des entités liées.
        foreach (var foreignKey in modelBuilder.Model
            .GetEntityTypes()
            .SelectMany(entityType => entityType.GetForeignKeys())
            .Where(foreignKey => !foreignKey.IsOwnership))
        {
            foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }
}