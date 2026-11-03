namespace PharmaDesk.Web.Models.Branches;

public class BranchListItemViewModel
{
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Address { get; set; }

    public string? Phone { get; set; }

    public bool IsMainBranch { get; set; }

    public bool IsActive { get; set; }
}
