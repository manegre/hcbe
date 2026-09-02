using HcbeApi.Helpers;

namespace HcbeApi.Tests.Helpers;

public class PasswordPolicyTests
{
    [Fact]
    public void GenerateTemporaryPassword_AlwaysMeetsTheStrongPasswordPolicy()
    {
        var generatedPasswords = Enumerable.Range(0, 100)
            .Select(_ => PasswordPolicy.GenerateTemporaryPassword())
            .ToList();

        Assert.All(generatedPasswords, password =>
        {
            Assert.Equal(16, password.Length);
            Assert.True(PasswordPolicy.IsStrong(password));
        });
        Assert.True(generatedPasswords.Distinct().Count() > 95);
    }
}
