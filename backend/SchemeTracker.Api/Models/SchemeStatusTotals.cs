namespace SchemeTracker.Api.Models;

public class SchemeStatusTotals
{
    public SchemeStatus Status { get; set; }

    public int SchemeCount { get; set; }

    public int TotalHomes { get; set; }

    public int AffordableHomes { get; set; }

    public decimal TotalValue { get; set; }
}
