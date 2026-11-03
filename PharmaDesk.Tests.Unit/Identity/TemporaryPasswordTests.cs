using PharmaDesk.Infrastructure.Identity;

namespace PharmaDesk.Tests.Unit.Identity;

public class TemporaryPasswordTests
{
    [Fact]
    public void GeneratedPassword_MeetsIdentityPasswordPolicy()
    {
        for (var i = 0; i < 50; i++)
        {
            var password = IdentitySeeder.GenerateTemporaryPassword();

            Assert.Equal(16, password.Length);
            Assert.Contains(password, c => char.IsUpper(c));
            Assert.Contains(password, c => char.IsLower(c));
            Assert.Contains(password, c => char.IsDigit(c));
            Assert.Contains(password, c => !char.IsLetterOrDigit(c));
        }
    }

    [Fact]
    public void GeneratedPassword_IsNotTheSameEveryTime()
    {
        var first = IdentitySeeder.GenerateTemporaryPassword();
        var second = IdentitySeeder.GenerateTemporaryPassword();

        Assert.NotEqual(first, second);
    }
}
