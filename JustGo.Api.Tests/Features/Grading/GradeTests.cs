using JustGo.Api.Features.Grading;
using JustGo.Api.Features.Members;

namespace JustGo.Api.Tests.Features.Grading;

public sealed class GradeTests
{
    [Fact]
    public void FromCredentials_WithoutCredentials_StartsAtTenthGup()
    {
        Assert.Same(Grade.TenthGup, Grade.FromCredentials(null));
        Assert.Same(Grade.TenthGup, Grade.FromCredentials([]));
        Assert.Same(Grade.NinthGup, Grade.FromCredentials([]).Next);
    }

    [Fact]
    public void FromCredentials_IgnoresInactiveAndUnrelatedCredentials()
    {
        MemberCredentialDtoV2_2[] credentials =
        [
            new() { Name = "1st Dan", Status = "Expired" },
            new() { Name = "Membership", Status = "Active" },
        ];

        Assert.Same(Grade.TenthGup, Grade.FromCredentials(credentials));
    }

    [Fact]
    public void FromCredentials_UsesHighestActiveGradeWhenPresent()
    {
        MemberCredentialDtoV2_2[] credentials =
        [
            new() { Name = "10th Gup", Status = "Active" },
            new() { Name = "7th Gup", Status = "Active" },
        ];

        Assert.Same(Grade.SeventhGup, Grade.FromCredentials(credentials));
    }
}
