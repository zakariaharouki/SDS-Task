using SchemeTracker.Api.Models;

namespace SchemeTracker.Api.Services;

public static class SchemeCalculator
{
    public static decimal? CalculateAffordablePercent(
        int affordableHomes,
        int totalHomes)
    {
        if (totalHomes == 0)
        {
            return null;
        }

        decimal percentage = (decimal)affordableHomes / totalHomes * 100;

        return Math.Round(percentage, 1, MidpointRounding.AwayFromZero);
    }

    public static SchemeTotals CalculateTotals(TenureMix mix)
    {
        var totals = new SchemeTotals();

        totals.TotalHomes =
            mix.SocialRent.Homes +
            mix.AffordableRent.Homes +
            mix.SharedOwnership.Homes +
            mix.MarketSale.Homes;

        totals.AffordableHomes =
            mix.SocialRent.Homes +
            mix.AffordableRent.Homes +
            mix.SharedOwnership.Homes;

        totals.TotalValue =
            mix.SocialRent.Homes * mix.SocialRent.AverageValue +
            mix.AffordableRent.Homes * mix.AffordableRent.AverageValue +
            mix.SharedOwnership.Homes * mix.SharedOwnership.AverageValue +
            mix.MarketSale.Homes * mix.MarketSale.AverageValue;

        return totals;
    }
}
