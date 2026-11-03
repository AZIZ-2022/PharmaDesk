namespace PharmaDesk.Web.Models.Payments;

public class StatementViewModel
{
    public long PartyId { get; set; }

    public string PartyName { get; set; } = string.Empty;

    public decimal OpeningBalance { get; set; }

    public decimal CurrentBalance { get; set; }

    public List<StatementEntryViewModel> Entries { get; set; } = new();
}

public class StatementEntryViewModel
{
    public DateTime Date { get; set; }

    public string Description { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public decimal RunningBalance { get; set; }
}
