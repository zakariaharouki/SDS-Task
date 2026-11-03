namespace SchemeTracker.Api.Dtos;

public record TenureLineDto
{
    public required int Homes { get; init; }

    public required decimal AverageValue { get; init; }
}

public record TenureMixDto
{
    public required TenureLineDto SocialRent { get; init; }

    public required TenureLineDto AffordableRent { get; init; }

    public required TenureLineDto SharedOwnership { get; init; }

    public required TenureLineDto MarketSale { get; init; }
}
