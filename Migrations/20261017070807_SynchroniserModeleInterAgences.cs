using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AkarangaBank.Web.Migrations
{
    /// <inheritdoc />
    public partial class SynchroniserModeleInterAgences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CompensationsAgences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Reference = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    AgenceId = table.Column<int>(type: "int", nullable: false),
                    CompteCompensationId = table.Column<int>(type: "int", nullable: false),
                    TotalDebits = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalCredits = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Solde = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DateCompensation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Statut = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "EnCours")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompensationsAgences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompensationsAgences_Agences_AgenceId",
                        column: x => x.AgenceId,
                        principalTable: "Agences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompensationsAgences_Comptes_CompteCompensationId",
                        column: x => x.CompteCompensationId,
                        principalTable: "Comptes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DeclarationsSoupcon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Reference = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    AlerteId = table.Column<int>(type: "int", nullable: false),
                    ClientId = table.Column<int>(type: "int", nullable: false),
                    TypeDeclaration = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Contenu = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MotifSuspicion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Statut = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Brouillon"),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateTransmission = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateAccuseReception = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RedigeParId = table.Column<int>(type: "int", nullable: true),
                    ReponseCNRF = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeclarationsSoupcon", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeclarationsSoupcon_AlertesAML_AlerteId",
                        column: x => x.AlerteId,
                        principalTable: "AlertesAML",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeclarationsSoupcon_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeclarationsSoupcon_Utilisateurs_RedigeParId",
                        column: x => x.RedigeParId,
                        principalTable: "Utilisateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SeuilsAML",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Libelle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Montant = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Devise = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false, defaultValue: "BIF"),
                    TypeOperation = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Toutes"),
                    Actif = table.Column<bool>(type: "bit", nullable: false),
                    DateModification = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeuilsAML", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VirementsInterAgences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Reference = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    VirementId = table.Column<int>(type: "int", nullable: false),
                    AgenceSourceId = table.Column<int>(type: "int", nullable: false),
                    AgenceDestinationId = table.Column<int>(type: "int", nullable: false),
                    Montant = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Devise = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false, defaultValue: "BIF"),
                    Statut = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "EnAttente"),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateCompensation = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateReglement = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MotifRejet = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ValideParId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VirementsInterAgences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VirementsInterAgences_Agences_AgenceDestinationId",
                        column: x => x.AgenceDestinationId,
                        principalTable: "Agences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VirementsInterAgences_Agences_AgenceSourceId",
                        column: x => x.AgenceSourceId,
                        principalTable: "Agences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VirementsInterAgences_Utilisateurs_ValideParId",
                        column: x => x.ValideParId,
                        principalTable: "Utilisateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VirementsInterAgences_Virements_VirementId",
                        column: x => x.VirementId,
                        principalTable: "Virements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompensationsAgences_AgenceId",
                table: "CompensationsAgences",
                column: "AgenceId");

            migrationBuilder.CreateIndex(
                name: "IX_CompensationsAgences_CompteCompensationId",
                table: "CompensationsAgences",
                column: "CompteCompensationId");

            migrationBuilder.CreateIndex(
                name: "IX_CompensationsAgences_DateCompensation",
                table: "CompensationsAgences",
                column: "DateCompensation");

            migrationBuilder.CreateIndex(
                name: "IX_DeclarationsSoupcon_AlerteId",
                table: "DeclarationsSoupcon",
                column: "AlerteId");

            migrationBuilder.CreateIndex(
                name: "IX_DeclarationsSoupcon_ClientId",
                table: "DeclarationsSoupcon",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_DeclarationsSoupcon_RedigeParId",
                table: "DeclarationsSoupcon",
                column: "RedigeParId");

            migrationBuilder.CreateIndex(
                name: "IX_DeclarationsSoupcon_Reference",
                table: "DeclarationsSoupcon",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SeuilsAML_Code",
                table: "SeuilsAML",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_VirementsInterAgences_AgenceDestinationId",
                table: "VirementsInterAgences",
                column: "AgenceDestinationId");

            migrationBuilder.CreateIndex(
                name: "IX_VirementsInterAgences_AgenceSourceId",
                table: "VirementsInterAgences",
                column: "AgenceSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_VirementsInterAgences_Reference",
                table: "VirementsInterAgences",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VirementsInterAgences_Statut",
                table: "VirementsInterAgences",
                column: "Statut");

            migrationBuilder.CreateIndex(
                name: "IX_VirementsInterAgences_ValideParId",
                table: "VirementsInterAgences",
                column: "ValideParId");

            migrationBuilder.CreateIndex(
                name: "IX_VirementsInterAgences_VirementId",
                table: "VirementsInterAgences",
                column: "VirementId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompensationsAgences");

            migrationBuilder.DropTable(
                name: "DeclarationsSoupcon");

            migrationBuilder.DropTable(
                name: "SeuilsAML");

            migrationBuilder.DropTable(
                name: "VirementsInterAgences");
        }
    }
}
