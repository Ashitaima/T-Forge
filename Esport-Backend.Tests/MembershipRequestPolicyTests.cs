using TForge.Common;
using Xunit;

namespace TForge.Tests;

public class MembershipRequestPolicyTests
{
    private const int Captain = 10;
    private const int PlayerUser = 20;
    private const int Stranger = 30;

    private static MembershipRequestPolicy.Context Invite(
        string status = MembershipRequestStatus.Pending) =>
        new(MembershipRequestDirection.Invite, status, Captain, Captain, PlayerUser);

    private static MembershipRequestPolicy.Context Application(
        string status = MembershipRequestStatus.Pending) =>
        new(MembershipRequestDirection.Application, status, PlayerUser, Captain, PlayerUser);

    // ---- Хто відповідає ----

    [Fact]
    public void ResponderUserId_Invite_IsThePlayer()
    {
        Assert.Equal(PlayerUser, MembershipRequestPolicy.ResponderUserId(Invite()));
    }

    [Fact]
    public void ResponderUserId_Application_IsTheCaptain()
    {
        Assert.Equal(Captain, MembershipRequestPolicy.ResponderUserId(Application()));
    }

    // ---- CanRespond ----

    [Fact]
    public void CanRespond_Invite_PlayerMay()
    {
        Assert.True(MembershipRequestPolicy.CanRespond(Invite(), PlayerUser, isAdmin: false));
    }

    [Fact]
    public void CanRespond_Invite_CaptainMayNot_EvenThoughTheyInitiatedIt()
    {
        Assert.False(MembershipRequestPolicy.CanRespond(Invite(), Captain, isAdmin: false));
    }

    [Fact]
    public void CanRespond_Application_CaptainMay()
    {
        Assert.True(MembershipRequestPolicy.CanRespond(Application(), Captain, isAdmin: false));
    }

    [Fact]
    public void CanRespond_Application_PlayerMayNot_EvenThoughTheyInitiatedIt()
    {
        Assert.False(MembershipRequestPolicy.CanRespond(Application(), PlayerUser, isAdmin: false));
    }

    [Fact]
    public void CanRespond_StrangerMayNot()
    {
        Assert.False(MembershipRequestPolicy.CanRespond(Invite(), Stranger, isAdmin: false));
        Assert.False(MembershipRequestPolicy.CanRespond(Application(), Stranger, isAdmin: false));
    }

    [Fact]
    public void CanRespond_AdminMay_InBothDirections()
    {
        Assert.True(MembershipRequestPolicy.CanRespond(Invite(), Stranger, isAdmin: true));
        Assert.True(MembershipRequestPolicy.CanRespond(Application(), Stranger, isAdmin: true));
    }

    [Theory]
    [InlineData(MembershipRequestStatus.Accepted)]
    [InlineData(MembershipRequestStatus.Declined)]
    [InlineData(MembershipRequestStatus.Cancelled)]
    public void CanRespond_TerminalStatus_RejectsEveryone(string status)
    {
        Assert.False(MembershipRequestPolicy.CanRespond(Invite(status), PlayerUser, isAdmin: false));
        Assert.False(MembershipRequestPolicy.CanRespond(Invite(status), PlayerUser, isAdmin: true));
    }

    // ---- CanCancel ----

    [Fact]
    public void CanCancel_InitiatorMay()
    {
        Assert.True(MembershipRequestPolicy.CanCancel(Invite(), Captain, isAdmin: false));
        Assert.True(MembershipRequestPolicy.CanCancel(Application(), PlayerUser, isAdmin: false));
    }

    [Fact]
    public void CanCancel_ResponderMayNot()
    {
        Assert.False(MembershipRequestPolicy.CanCancel(Invite(), PlayerUser, isAdmin: false));
        Assert.False(MembershipRequestPolicy.CanCancel(Application(), Captain, isAdmin: false));
    }

    [Fact]
    public void CanCancel_AdminMay()
    {
        Assert.True(MembershipRequestPolicy.CanCancel(Invite(), Stranger, isAdmin: true));
    }

    [Theory]
    [InlineData(MembershipRequestStatus.Accepted)]
    [InlineData(MembershipRequestStatus.Declined)]
    [InlineData(MembershipRequestStatus.Cancelled)]
    public void CanCancel_TerminalStatus_RejectsEveryone(string status)
    {
        Assert.False(MembershipRequestPolicy.CanCancel(Invite(status), Captain, isAdmin: false));
        Assert.False(MembershipRequestPolicy.CanCancel(Invite(status), Captain, isAdmin: true));
    }

    // ---- IsPending ----

    [Fact]
    public void IsPending_TrueOnlyForPending()
    {
        Assert.True(MembershipRequestPolicy.IsPending(Invite()));
        Assert.False(MembershipRequestPolicy.IsPending(Invite(MembershipRequestStatus.Accepted)));
    }
}
