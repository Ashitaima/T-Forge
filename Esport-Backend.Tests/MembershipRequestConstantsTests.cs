using TForge.Common;
using Xunit;

namespace TForge.Tests;

public class MembershipRequestConstantsTests
{
    [Fact]
    public void Direction_IsValid_AcceptsBothDirections()
    {
        Assert.True(MembershipRequestDirection.IsValid(MembershipRequestDirection.Invite));
        Assert.True(MembershipRequestDirection.IsValid(MembershipRequestDirection.Application));
    }

    [Fact]
    public void Direction_IsValid_RejectsUnknownAndNull()
    {
        Assert.False(MembershipRequestDirection.IsValid("Sideways"));
        Assert.False(MembershipRequestDirection.IsValid(null));
    }

    [Fact]
    public void Status_IsValid_AcceptsAllFourStatuses()
    {
        Assert.True(MembershipRequestStatus.IsValid(MembershipRequestStatus.Pending));
        Assert.True(MembershipRequestStatus.IsValid(MembershipRequestStatus.Accepted));
        Assert.True(MembershipRequestStatus.IsValid(MembershipRequestStatus.Declined));
        Assert.True(MembershipRequestStatus.IsValid(MembershipRequestStatus.Cancelled));
    }

    [Fact]
    public void Status_All_ContainsExactlyFourEntries()
    {
        Assert.Equal(4, MembershipRequestStatus.All.Length);
    }
}
