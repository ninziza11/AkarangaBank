using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AkarangaBank.Web.Data;
using AkarangaBank.Web.Models;

namespace AkarangaBank.Web.Services;

public class PretService
{
    private readonly AkarangaBankContext _context;

    public PretService(AkarangaBankContext context)
    {
        _context = context;
    }

    public List<object> GenererTableauAmortissement(decimal principal, decimal tauxAnnuel, int dureeMois, int pretId)
    {
        var echeances = new List<object>();
        double tauxMensuel = (double)(tauxAnnuel / 12m / 100m);
        
        // Calcul de la mensualité constante (Formule standard PMT d'amortissement constant)
        decimal mensualite = principal / (decimal)dureeMois;
        if (tauxMensuel > 0)
        {
            mensualite = principal * (decimal)(tauxMensuel / (1 - Math.Pow(1 + tauxMensuel, -dureeMois)));
        }

        decimal soldeRestantDu = principal;

        for (int i = 1; i <= dureeMois; i++)
        {
            decimal interet = soldeRestantDu * (tauxAnnuel / 12m / 100m);
            decimal capitalAmorti = mensualite - interet;

            // Ajustement de la dernière ligne pour éviter les écarts d'arrondis centésimaux réglementaires
            if (i == dureeMois)
            {
                capitalAmorti = soldeRestantDu;
                mensualite = capitalAmorti + interet;
            }

            soldeRestantDu -= capitalAmorti;

            // Utilisation d'un objet anonyme dynamique pour garantir la compilation immédiate
            echeances.Add(new
            {
                PretId = pretId,
                NumeroEcheance = i,
                DateEcheance = DateTime.Now.AddMonths(i),
                Capital = Math.Round(capitalAmorti, 2),
                Interet = Math.Round(interet, 2),
                MontantTotal = Math.Round(mensualite, 2),
                EstPaye = false
            });
        }

        return echeances;
    }
}
