namespace PharmaDesk.Application.Security;

public static class PermissionCodes
{
    public static class Users
    {
        public const string View = "Users.View";
        public const string Create = "Users.Create";
        public const string Edit = "Users.Edit";
        public const string Deactivate = "Users.Deactivate";
    }

    public static class Roles
    {
        public const string View = "Roles.View";
        public const string Manage = "Roles.Manage";
    }

    public static class Tenants
    {
        public const string Manage = "Tenant.Manage";
    }

    public static class Branches
    {
        public const string View = "Branches.View";
        public const string Manage = "Branches.Manage";
    }

    public static class Medicines
    {
        public const string View = "Medicines.View";
        public const string Manage = "Medicines.Manage";
    }

    public static class Suppliers
    {
        public const string View = "Suppliers.View";
        public const string Manage = "Suppliers.Manage";
    }

    public static class Customers
    {
        public const string View = "Customers.View";
        public const string Manage = "Customers.Manage";
    }

    public static class Purchases
    {
        public const string View = "Purchases.View";
        public const string Manage = "Purchases.Manage";
    }

    public static class Inventory
    {
        public const string View = "Inventory.View";
        public const string Adjust = "Inventory.Adjust";
    }

    public static class Sales
    {
        public const string View = "Sales.View";
        public const string Manage = "Sales.Manage";
    }

    public static class Returns
    {
        public const string View = "Returns.View";
        public const string Manage = "Returns.Manage";
    }

    public static class Payments
    {
        public const string View = "Payments.View";
        public const string Manage = "Payments.Manage";
    }

    public static class Expenses
    {
        public const string View = "Expenses.View";
        public const string Manage = "Expenses.Manage";
    }

    public static class CashDrawer
    {
        public const string View = "CashDrawer.View";
        public const string Manage = "CashDrawer.Manage";
    }

    public static class Reports
    {
        public const string View = "Reports.View";
    }

    public static class Backup
    {
        public const string View = "Backup.View";
        public const string Manage = "Backup.Manage";
    }
}
