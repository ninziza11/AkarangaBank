using AkarangaBank.Web.Data;
using AkarangaBank.Web.Data.Interceptors;
using AkarangaBank.Web.Models;
using AkarangaBank.Web.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;          // CHAPITRE 13
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;                       // CHAPITRE 13
using System.Text;
using System.Text.Json;


var builder = WebApplication.CreateBuilder(args);


// ======================================================
// 0. Localisation (CHAPITRE 13) : fr, en, rn, sw
// ======================================================


// Les fichiers .resx sont dans le dossier Resources
builder.Services.AddLocalization(options =>
    options.ResourcesPath = "Resources");


builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var cultures = new[]
    {
        new CultureInfo("fr"),   // français (par défaut)

        new CultureInfo("en"),   // anglais
        new CultureInfo("rn"),   // kirundi
        new CultureInfo("sw")    // swahili
    };

    options.DefaultRequestCulture = new RequestCulture("fr");
    options.SupportedCultures = cultures;
    options.SupportedUICultures = cultures;

    // La langue choisie est lue dans un cookie
    // (posé par CultureController)
    options.RequestCultureProviders = new List<IRequestCultureProvider>
    {
        new CookieRequestCultureProvider()
    };
});


// ======================================================
// 1. MVC et Razor Pages
// ======================================================


builder.Services.AddControllersWithViews()
    .AddViewLocalization()              // CHAPITRE 13 : traductions dans les vues
    .AddDataAnnotationsLocalization();  // CHAPITRE 13 : messages de validation traduits


builder.Services.AddRazorPages();


// ======================================================

// 2. Base de données SQL Server
// ======================================================


builder.Services.AddScoped<AuditInterceptor>();


builder.Services.AddDbContext<AkarangaBankContext>((serviceProvider, options) =>
{
    var auditInterceptor = serviceProvider
        .GetRequiredService<AuditInterceptor>();


    options.UseSqlServer(
            builder.Configuration.GetConnectionString("AkarangaBank"))
        .AddInterceptors(auditInterceptor);
});


// ======================================================
// 3. ASP.NET Core Identity
// ======================================================


builder.Services
    .AddIdentity<ApplicationUser, ApplicationRole>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;



        options.SignIn.RequireConfirmedAccount = false;
        options.SignIn.RequireConfirmedEmail = false;


        options.Lockout.MaxFailedAccessAttempts = 3;
        options.Lockout.DefaultLockoutTimeSpan =
            TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<AkarangaBankContext>()
    .AddDefaultTokenProviders();


// ======================================================
// 4. Cookie d'authentification
// ======================================================


builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";


    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
});


// ======================================================
// 5. JWT pour l'API mobile

// ======================================================


var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? "CleSecreteParDefautAkarangaBank2026!";


var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? "AkarangaBank";


var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? "AkarangaBankClient";


builder.Services
    .AddAuthentication()
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,


                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,



                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSecret)),


                ClockSkew = TimeSpan.Zero
            };
    });


// ======================================================
// 6. Politiques d'autorisation
// ======================================================


builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("admin");
    });


    options.AddPolicy("AdminOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("admin");
    });


    options.AddPolicy("RequireAuditeur", policy =>
    {

        policy.RequireAuthenticatedUser();
        policy.RequireRole("auditeur", "admin");
    });


    options.AddPolicy("RequireGestionnaire", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("gestionnaire", "admin");
    });


    options.AddPolicy("RequireGuichetier", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole(
            "guichetier",
            "gestionnaire",
            "admin");
    });


    options.AddPolicy("RequireClient", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("client");
    });


    options.AddPolicy("GuichetierOnly", policy =>
    {
        policy.RequireAuthenticatedUser();

        policy.RequireRole("guichetier");
    });
});


// ======================================================
// 7. Session et contexte HTTP
// ======================================================


builder.Services.AddDistributedMemoryCache();


builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});


builder.Services.AddHttpContextAccessor();


// ======================================================
// 8. Services métier
// ======================================================


builder.Services.AddScoped<AuditService>();



builder.Services.AddScoped<IAgenceService, AgenceService>();
builder.Services.AddScoped<IClientService, ClientService>();


builder.Services.AddScoped<
    IVerificationKYCService,
    VerificationKYCService>();


builder.Services.AddScoped<ICompteService, CompteService>();


builder.Services.AddScoped<
    IUtilisateurService,
    UtilisateurService>();


builder.Services.AddScoped<TransactionService>();
builder.Services.AddScoped<PretService>();


builder.Services.AddScoped<LocalisationService>();
builder.Services.AddScoped<ReportingService>();
builder.Services.AddScoped<NotificationService>();


builder.Services.AddSingleton<IQrCodeService, QrCodeService>();


builder.Services.AddScoped<
    ICurrentUserService,
    CurrentUserService>();



builder.Services.AddScoped<
    IConformiteService,
    ConformiteService>();


builder.Services.AddScoped<
    ISurveillanceService,
    SurveillanceService>();


builder.Services.AddScoped<
    IDeclarationService,
    DeclarationService>();


builder.Services.AddScoped<
    IComptabiliteService,
    ComptabiliteService>();


builder.Services.AddScoped<
    ICompensationService,
    CompensationService>();


builder.Services.AddScoped<
    IVirementInterAgenceService,
    VirementInterAgenceService>();



builder.Services.AddScoped<ICNRFService, CNRFService>();


builder.Services.AddScoped<
    ISeuilAMLService,
    SeuilAMLService>();


builder.Services.AddScoped<
    IDetectionAvanceeService,
    DetectionAvanceeService>();


// ======================================================
// 8bis. Health checks (Partie G)
// ======================================================


// Vérifie que SQL Server est joignable (CanConnectAsync).
// Le tag "ready" rattache ce check à l'endpoint /health/ready.
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AkarangaBankContext>(
        name: "database",
        tags: new[] { "ready" });


// ======================================================
// 9. Construction de l'application
// ======================================================


var app = builder.Build();



// ======================================================
// 10. Pipeline HTTP
// ======================================================


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}


app.UseHttpsRedirection();
app.UseStaticFiles();


app.UseRouting();


// CHAPITRE 13 : après UseRouting, avant la session,
// l'authentification et les contrôleurs
app.UseRequestLocalization();


app.UseSession();


app.UseAuthentication();
app.UseAuthorization();



// ======================================================
// 11. Routes
// ======================================================


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.MapRazorPages();


// ======================================================
// 11bis. Endpoints de santé (Partie G)
// ======================================================


// LIVENESS : "l'application est-elle vivante ?" — ne vérifie QUE
// le process. Ne dépend jamais de la base : si SQL Server est lent,
// Docker ne doit pas tuer le conteneur.
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => false                   // aucun check : simple ping HTTP
});


// READINESS : "l'application est-elle prête à recevoir du trafic ?"
// Vérifie SQL Server. Renvoie un JSON détaillé pour le débogage.
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{

    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var json = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            duration = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description
            })
        });
        await context.Response.WriteAsync(json);
    }
});


// ======================================================
// 12. Initialisation des données de départ
// ======================================================


using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;


    var context =
        services.GetRequiredService<AkarangaBankContext>();



    var userManager =
        services.GetRequiredService<UserManager<ApplicationUser>>();


    var roleManager =
        services.GetRequiredService<RoleManager<ApplicationRole>>();


    var logger =
        services.GetRequiredService<ILogger<Program>>();


    try
    {
        // Migration uniquement avec SQL Server.
        // Les tests utilisent InMemory et ne doivent pas migrer.
        if (context.Database.ProviderName ==
            "Microsoft.EntityFrameworkCore.SqlServer")
        {
            await context.Database.MigrateAsync();
        }


        // --------------------------------------------------
        // 12.1. Agences
        // --------------------------------------------------


        if (!await context.Agences.AnyAsync())
        {

            context.Agences.AddRange(
                new Agence
                {
                    Code = "AKR-BJM",
                    Nom = "Akaranga Bank Bujumbura",
                    Ville = "Bujumbura",
                    EstSiege = true
                },
                new Agence
                {
                    Code = "AKR-GIT",
                    Nom = "Akaranga Bank Gitega",
                    Ville = "Gitega",
                    EstSiege = false
                },
                new Agence
                {
                    Code = "AKR-NGO",
                    Nom = "Akaranga Bank Ngozi",
                    Ville = "Ngozi",
                    EstSiege = false
                },
                new Agence
                {
                    Code = "AKR-RUM",
                    Nom = "Akaranga Bank Rumonge",
                    Ville = "Rumonge",
                    EstSiege = false
                });


            await context.SaveChangesAsync();

        }


        // --------------------------------------------------
        // 12.2. Types de compte
        // --------------------------------------------------


        if (!await context.TypesCompte.AnyAsync())
        {
            context.TypesCompte.AddRange(
                new TypeCompte
                {
                    Code = "COURANT",
                    Libelle = "Compte Courant",
                    TauxInteret = 0m,
                    SoldeMinimum = 0m,
                    AutoriseDecouvert = false
                },
                new TypeCompte
                {
                    Code = "EPARGNE",
                    Libelle = "Compte Épargne",
                    TauxInteret = 3.5m,
                    SoldeMinimum = 10000m,
                    AutoriseDecouvert = false
                });


            await context.SaveChangesAsync();
        }



        // --------------------------------------------------
        // 12.3. Rôles
        // --------------------------------------------------


        string[] roles =
        {
            "admin",
            "gestionnaire",
            "guichetier",
            "auditeur",
            "client"
        };


        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var resultatRole = await roleManager.CreateAsync(
                    new ApplicationRole(roleName));


                if (!resultatRole.Succeeded)
                {
                    foreach (var erreur in resultatRole.Errors)
                    {
                        logger.LogError(
                            "Erreur lors de la création du rôle " +
                            "{Role} : {Description}",
                            roleName,

                            erreur.Description);
                    }
                }
            }
        }


        // --------------------------------------------------
        // 12.4. Utilisateurs de test
        // Réservés à l'environnement local de démonstration.
        // --------------------------------------------------


        var agenceId = await context.Agences
            .OrderBy(a => a.Id)
            .Select(a => a.Id)
            .FirstAsync();


        var utilisateursTest = new[]
        {
            new
            {
                UserName = "admin",
                Email = "admin@akarangabank.bi",
                Role = "admin",
                MotDePasse = "Admin@2026",
                Nom = "System",
                Prenom = "Admin"
            },
            new
            {

                UserName = "gestionnaire",
                Email = "gestionnaire@akarangabank.bi",
                Role = "gestionnaire",
                MotDePasse = "Akaranga@2026",
                Nom = "Agent",
                Prenom = "Gestionnaire"
            },
            new
            {
                UserName = "guichetier",
                Email = "guichetier@akarangabank.bi",
                Role = "guichetier",
                MotDePasse = "Akaranga@2026",
                Nom = "Agent",
                Prenom = "Guichetier"
            },
            new
            {
                UserName = "auditeur",
                Email = "auditeur@akarangabank.bi",
                Role = "auditeur",
                MotDePasse = "Akaranga@2026",
                Nom = "Agent",
                Prenom = "Auditeur"
            },
            new
            {
                UserName = "client",
                Email = "client@akarangabank.bi",
                Role = "client",
                MotDePasse = "Akaranga@2026",
                Nom = "Test",

                Prenom = "Client"
            }
        };


        foreach (var donnees in utilisateursTest)
        {
            var utilisateurExistant = await userManager
                .FindByNameAsync(donnees.UserName);


            if (utilisateurExistant is not null)
            {
                continue;
            }


            var utilisateur = new ApplicationUser
            {
                UserName = donnees.UserName,
                Email = donnees.Email,
                EmailConfirmed = true,


                Nom = donnees.Nom,
                Prenom = donnees.Prenom,


                Role = donnees.Role,
                AgenceId = agenceId,



                EstActif = true,
                DeuxFA_Active = false
            };


            var resultatCreation = await userManager.CreateAsync(
                utilisateur,
                donnees.MotDePasse);


            if (!resultatCreation.Succeeded)
            {
                foreach (var erreur in resultatCreation.Errors)
                {
                    logger.LogError(
                        "Erreur lors de la création de " +
                        "{UserName} : {Description}",
                        donnees.UserName,
                        erreur.Description);
                }


                continue;
            }


            var resultatAjoutRole = await userManager.AddToRoleAsync(
                utilisateur,
                donnees.Role);


            if (!resultatAjoutRole.Succeeded)

            {
                foreach (var erreur in resultatAjoutRole.Errors)
                {
                    logger.LogError(
                        "Erreur lors de l’attribution du rôle " +
                        "{Role} à {UserName} : {Description}",
                        donnees.Role,
                        donnees.UserName,
                        erreur.Description);
                }
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(
            ex,
            "Échec de l’initialisation des données de départ.");
    }
}


// ======================================================
// 13. Démarrage
// ======================================================


app.Run();


// Nécessaire pour les tests d'intégration WebApplicationFactory.
public partial class Program { }