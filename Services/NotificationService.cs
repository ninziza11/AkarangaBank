using System;
using System.Diagnostics;

namespace AkarangaBank.Web.Services;

public class NotificationService
{
    public void EnvoyerAlerteSecurite(string destinataire, string message, string canal = "SMS")
    {
        // Simulation d'envoi via passerelle télécom locale (Econet/Onatel)
        string horodatage = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        Debug.WriteLine($"[{canal.ToUpper()}] [{horodatage}] Vers {destinataire} : {message}");
        Console.WriteLine($"\n⚡ [NOTIFICATION {canal.ToUpper()}] Envoyé à {destinataire} -> {message}\n");
    }
}
