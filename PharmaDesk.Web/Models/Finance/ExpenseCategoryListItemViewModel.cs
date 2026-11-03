namespace PharmaDesk.Web.Models.Finance;

public class ExpenseCategoryListItemViewModel
{
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; }
}
