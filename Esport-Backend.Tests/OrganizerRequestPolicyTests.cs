using TForge.Common;
using Xunit;

namespace TForge.Tests;

public class OrganizerRequestPolicyTests
{
    private const int Applicant = 7;
    private const int Someone = 8;

    private static OrganizerRequestPolicy.Context Pending() =>
        new(OrganizerRequestStatus.Pending, Applicant);

    // ---- Розгляд ----

    [Fact]
    public void Admin_CanRespond_ToPending()
    {
        Assert.True(OrganizerRequestPolicy.CanRespond(Pending(), isAdmin: true));
    }

    // Саме заради цього роль і не видається реєстрацією.
    [Fact]
    public void NonAdmin_CannotRespond()
    {
        Assert.False(OrganizerRequestPolicy.CanRespond(Pending(), isAdmin: false));
    }

    [Theory]
    [InlineData(OrganizerRequestStatus.Approved)]
    [InlineData(OrganizerRequestStatus.Declined)]
    [InlineData(OrganizerRequestStatus.Cancelled)]
    public void Admin_CannotRespond_Twice(string status)
    {
        var context = new OrganizerRequestPolicy.Context(status, Applicant);

        Assert.False(OrganizerRequestPolicy.CanRespond(context, isAdmin: true));
    }

    // ---- Відкликання ----

    [Fact]
    public void Applicant_CanCancel_OwnPending()
    {
        Assert.True(OrganizerRequestPolicy.CanCancel(Pending(), Applicant));
    }

    [Fact]
    public void Other_CannotCancel()
    {
        Assert.False(OrganizerRequestPolicy.CanCancel(Pending(), Someone));
    }

    [Fact]
    public void Applicant_CannotCancel_AfterDecision()
    {
        var context = new OrganizerRequestPolicy.Context(OrganizerRequestStatus.Approved, Applicant);

        Assert.False(OrganizerRequestPolicy.CanCancel(context, Applicant));
    }

    // ---- Подання ----

    [Fact]
    public void Player_CanApply()
    {
        Assert.True(OrganizerRequestPolicy.CanApply(UserRoles.Player));
    }

    // Обидві ролі вже мають це право, тож заявка нічого не змінила б.
    [Theory]
    [InlineData(UserRoles.Organizer)]
    [InlineData(UserRoles.Admin)]
    public void AlreadyPrivileged_CannotApply(string role)
    {
        Assert.False(OrganizerRequestPolicy.CanApply(role));
    }
}
