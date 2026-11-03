namespace PharmaDesk.Web.Models.Users;

public class UserListItemViewModel
{
    public long Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public IList<string> Roles { get; set; } = new List<string>();
}
