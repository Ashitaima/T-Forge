using TForge.Common;
using Xunit;

namespace TForge.Tests;

public class TournamentInvitationPolicyTests
{
    private const int Organizer = 10;
    private const int Captain = 20;
    private const int Stranger = 30;

    private static TournamentInvitationPolicy.Context Invite(
        string status = TournamentInvitationStatus.Pending) =>
        new(TournamentInvitationDirection.Invite, status, Organizer, Organizer, Captain);

    private static TournamentInvitationPolicy.Context Application(
        string status = TournamentInvitationStatus.Pending) =>
        new(TournamentInvitationDirection.Application, status, Captain, Organizer, Captain);

    // ---- Хто відповідає ----

    [Fact]
    public void ResponderUserId_Invite_IsTheCaptain()
    {
        Assert.Equal(Captain, TournamentInvitationPolicy.ResponderUserId(Invite()));
    }

    [Fact]
    public void ResponderUserId_Application_IsTheOrganizer()
    {
        Assert.Equal(Organizer, TournamentInvitationPolicy.ResponderUserId(Application()));
    }

    // ---- CanRespond ----

    [Fact]
    public void CanRespond_Invite_CaptainMay_OrganizerMayNot()
    {
        Assert.True(TournamentInvitationPolicy.CanRespond(Invite(), Captain, isAdmin: false));
        Assert.False(TournamentInvitationPolicy.CanRespond(Invite(), Organizer, isAdmin: false));
    }

    [Fact]
    public void CanRespond_Application_OrganizerMay_CaptainMayNot()
    {
        Assert.True(TournamentInvitationPolicy.CanRespond(Application(), Organizer, isAdmin: false));
        Assert.False(TournamentInvitationPolicy.CanRespond(Application(), Captain, isAdmin: false));
    }

    [Fact]
    public void CanRespond_StrangerMayNot()
    {
        Assert.False(TournamentInvitationPolicy.CanRespond(Invite(), Stranger, isAdmin: false));
        Assert.False(TournamentInvitationPolicy.CanRespond(Application(), Stranger, isAdmin: false));
    }

    [Fact]
    public void CanRespond_AdminMay_InBothDirections()
    {
        Assert.True(TournamentInvitationPolicy.CanRespond(Invite(), Stranger, isAdmin: true));
        Assert.True(TournamentInvitationPolicy.CanRespond(Application(), Stranger, isAdmin: true));
    }

    [Theory]
    [InlineData(TournamentInvitationStatus.Accepted)]
    [InlineData(TournamentInvitationStatus.Declined)]
    [InlineData(TournamentInvitationStatus.Cancelled)]
    public void CanRespond_TerminalStatus_RejectsEveryone(string status)
    {
        Assert.False(TournamentInvitationPolicy.CanRespond(Invite(status), Captain, isAdmin: false));
        Assert.False(TournamentInvitationPolicy.CanRespond(Invite(status), Captain, isAdmin: true));
    }

    // ---- CanCancel ----

    [Fact]
    public void CanCancel_InitiatorMay_ResponderMayNot()
    {
        Assert.True(TournamentInvitationPolicy.CanCancel(Invite(), Organizer, isAdmin: false));
        Assert.False(TournamentInvitationPolicy.CanCancel(Invite(), Captain, isAdmin: false));

        Assert.True(TournamentInvitationPolicy.CanCancel(Application(), Captain, isAdmin: false));
        Assert.False(TournamentInvitationPolicy.CanCancel(Application(), Organizer, isAdmin: false));
    }

    [Theory]
    [InlineData(TournamentInvitationStatus.Accepted)]
    [InlineData(TournamentInvitationStatus.Declined)]
    [InlineData(TournamentInvitationStatus.Cancelled)]
    public void CanCancel_TerminalStatus_RejectsEveryone(string status)
    {
        Assert.False(TournamentInvitationPolicy.CanCancel(Invite(status), Organizer, isAdmin: false));
        Assert.False(TournamentInvitationPolicy.CanCancel(Invite(status), Organizer, isAdmin: true));
    }

    // ---- Хто ініціює ----

    [Fact]
    public void CanInvite_OnlyTheOrganizer()
    {
        Assert.True(TournamentInvitationPolicy.CanInvite(Organizer, Organizer, isAdmin: false));
        Assert.False(TournamentInvitationPolicy.CanInvite(Captain, Organizer, isAdmin: false));
        Assert.True(TournamentInvitationPolicy.CanInvite(Stranger, Organizer, isAdmin: true));
    }

    [Fact]
    public void CanApply_OnlyTheCaptain()
    {
        Assert.True(TournamentInvitationPolicy.CanApply(Captain, Captain, isAdmin: false));
        Assert.False(TournamentInvitationPolicy.CanApply(Organizer, Captain, isAdmin: false));
        Assert.True(TournamentInvitationPolicy.CanApply(Stranger, Captain, isAdmin: true));
    }

    // ---- Пряма реєстрація ----

    [Fact]
    public void CanRegisterDirectly_OpenTournament_CaptainMay()
    {
        Assert.True(TournamentInvitationPolicy.CanRegisterDirectly(
            isInviteOnly: false, Captain, Organizer, Captain, isAdmin: false));
    }

    [Fact]
    public void CanRegisterDirectly_InviteOnly_CaptainMayNot()
    {
        // Саме в цьому суть перемикача: склад визначає організатор, а не
        // швидкість кліку капітана.
        Assert.False(TournamentInvitationPolicy.CanRegisterDirectly(
            isInviteOnly: true, Captain, Organizer, Captain, isAdmin: false));
    }

    [Fact]
    public void CanRegisterDirectly_InviteOnly_OrganizerAndAdminStillMay()
    {
        Assert.True(TournamentInvitationPolicy.CanRegisterDirectly(
            isInviteOnly: true, Organizer, Organizer, Captain, isAdmin: false));
        Assert.True(TournamentInvitationPolicy.CanRegisterDirectly(
            isInviteOnly: true, Stranger, Organizer, Captain, isAdmin: true));
    }

    [Fact]
    public void CanRegisterDirectly_StrangerNeverMay()
    {
        Assert.False(TournamentInvitationPolicy.CanRegisterDirectly(
            isInviteOnly: false, Stranger, Organizer, Captain, isAdmin: false));
        Assert.False(TournamentInvitationPolicy.CanRegisterDirectly(
            isInviteOnly: true, Stranger, Organizer, Captain, isAdmin: false));
    }

    // ---- Константи ----

    [Fact]
    public void Constants_KnowTheirOwnValues()
    {
        Assert.All(TournamentInvitationStatus.All,
            status => Assert.True(TournamentInvitationStatus.IsValid(status)));
        Assert.All(TournamentInvitationDirection.All,
            direction => Assert.True(TournamentInvitationDirection.IsValid(direction)));

        Assert.False(TournamentInvitationStatus.IsValid("Expired"));
        Assert.False(TournamentInvitationDirection.IsValid("Request"));
        Assert.False(TournamentInvitationStatus.IsValid(null));
    }
}
