using TForge.Common;

namespace TForge.Tests;

/// <summary>
/// Кому адресовано рядок — це чиста функція, тож перевіряється без бази.
/// Загальне правило одне: поки рядок відкритий, він чекає на відповідача;
/// щойно закритий — повідомляє ініціатора, бо саме той питав.
/// </summary>
public class NotificationAddressingTests
{
    private const int Initiator = 11;
    private const int Responder = 22;
    private const int Stranger = 33;

    [Theory]
    [InlineData(NotificationAddressing.Sources.Membership, MembershipRequestDirection.Invite)]
    [InlineData(NotificationAddressing.Sources.Membership, MembershipRequestDirection.Application)]
    [InlineData(NotificationAddressing.Sources.Tournament, TournamentInvitationDirection.Invite)]
    [InlineData(NotificationAddressing.Sources.Tournament, TournamentInvitationDirection.Application)]
    [InlineData(NotificationAddressing.Sources.Challenge, null)]
    public void Pending_AwaitsTheResponder(string source, string? direction)
    {
        var audience = NotificationAddressing.For(
            source, direction, MembershipRequestStatus.Pending, Initiator, Responder);

        Assert.NotNull(audience);
        Assert.Equal(Responder, audience!.UserId);
        Assert.True(audience.IsActionable);
    }

    [Theory]
    [InlineData(MembershipRequestStatus.Accepted)]
    [InlineData(MembershipRequestStatus.Declined)]
    public void Resolved_InformsTheInitiator(string status)
    {
        var audience = NotificationAddressing.For(
            NotificationAddressing.Sources.Membership,
            MembershipRequestDirection.Invite, status, Initiator, Responder);

        Assert.NotNull(audience);
        Assert.Equal(Initiator, audience!.UserId);
        Assert.False(audience.IsActionable);
    }

    [Fact]
    public void Cancelled_TellsNobody()
    {
        // Скасував ініціатор — повідомляти його ж про власну дію безглуздо,
        // а відповідачеві більше нема на що відповідати.
        Assert.Null(NotificationAddressing.For(
            NotificationAddressing.Sources.Challenge, null,
            MatchChallengeStatus.Cancelled, Initiator, Responder));
    }

    [Fact]
    public void NobodyElseIsEverAddressed()
    {
        var pending = NotificationAddressing.For(
            NotificationAddressing.Sources.Challenge, null,
            MatchChallengeStatus.Pending, Initiator, Responder);
        var resolved = NotificationAddressing.For(
            NotificationAddressing.Sources.Challenge, null,
            MatchChallengeStatus.Accepted, Initiator, Responder);

        Assert.NotEqual(Stranger, pending!.UserId);
        Assert.NotEqual(Stranger, resolved!.UserId);
    }

    [Fact]
    public void SelfAnsweredRow_IsNotReportedBackToItsOwnInitiator()
    {
        // Адмін міг відповісти за ініціатора. Тоді ініціатор і відповідач —
        // одна людина, і сповіщення «вам відповіли» читалося б як шум.
        Assert.Null(NotificationAddressing.For(
            NotificationAddressing.Sources.Membership,
            MembershipRequestDirection.Invite,
            MembershipRequestStatus.Accepted, Initiator, Initiator));
    }

    /// <summary>
    /// Кожна пара «джерело + напрям» дає рівно два різні види: той, що чекає
    /// відповіді, і той, що повідомляє про неї. Разом — десять, і всі різні.
    /// </summary>
    [Fact]
    public void Kind_IsDistinctForEverySourceDirectionAndPhase()
    {
        var kinds = new[]
        {
            NotificationAddressing.Kind(NotificationAddressing.Sources.Membership, MembershipRequestDirection.Invite, true),
            NotificationAddressing.Kind(NotificationAddressing.Sources.Membership, MembershipRequestDirection.Invite, false),
            NotificationAddressing.Kind(NotificationAddressing.Sources.Membership, MembershipRequestDirection.Application, true),
            NotificationAddressing.Kind(NotificationAddressing.Sources.Membership, MembershipRequestDirection.Application, false),
            NotificationAddressing.Kind(NotificationAddressing.Sources.Challenge, null, true),
            NotificationAddressing.Kind(NotificationAddressing.Sources.Challenge, null, false),
            NotificationAddressing.Kind(NotificationAddressing.Sources.Tournament, TournamentInvitationDirection.Invite, true),
            NotificationAddressing.Kind(NotificationAddressing.Sources.Tournament, TournamentInvitationDirection.Invite, false),
            NotificationAddressing.Kind(NotificationAddressing.Sources.Tournament, TournamentInvitationDirection.Application, true),
            NotificationAddressing.Kind(NotificationAddressing.Sources.Tournament, TournamentInvitationDirection.Application, false),
            NotificationAddressing.Kind(NotificationAddressing.Sources.Duel, null, true),
            NotificationAddressing.Kind(NotificationAddressing.Sources.Duel, null, false)
        };

        Assert.Equal(12, kinds.Distinct().Count());
        Assert.All(kinds, kind => Assert.True(NotificationKinds.IsValid(kind)));
    }

    [Fact]
    public void Kinds_AreDistinctAndSelfValidating()
    {
        Assert.Equal(12, NotificationKinds.All.Length);
        Assert.Equal(NotificationKinds.All.Length, NotificationKinds.All.Distinct().Count());
        Assert.All(NotificationKinds.All, kind => Assert.True(NotificationKinds.IsValid(kind)));
        Assert.False(NotificationKinds.IsValid("Whatever"));
        Assert.False(NotificationKinds.IsValid(null));
    }

    // ---- Дуелі ----

    [Fact]
    public void Duel_Pending_AwaitsTheOpponent()
    {
        var audience = NotificationAddressing.For(
            NotificationAddressing.Sources.Duel, null, DuelStatuses.Pending, Initiator, Responder);

        Assert.NotNull(audience);
        Assert.Equal(Responder, audience!.UserId);
        Assert.True(audience.IsActionable);
        Assert.Equal(NotificationKinds.DuelReceived,
            NotificationAddressing.Kind(NotificationAddressing.Sources.Duel, null, true));
    }

    [Theory]
    [InlineData(DuelStatuses.Accepted)]
    [InlineData(DuelStatuses.Declined)]
    // Дуель після згоди живе далі — почалась і завершилась. Повідомлення про
    // відповідь не повинно зникати лише тому, що гра пішла своїм ходом.
    [InlineData(DuelStatuses.InProgress)]
    [InlineData(DuelStatuses.Completed)]
    public void Duel_Answered_InformsTheChallenger(string status)
    {
        var audience = NotificationAddressing.For(
            NotificationAddressing.Sources.Duel, null, status, Initiator, Responder);

        Assert.NotNull(audience);
        Assert.Equal(Initiator, audience!.UserId);
        Assert.False(audience.IsActionable);
        Assert.Equal(NotificationKinds.DuelAnswered,
            NotificationAddressing.Kind(NotificationAddressing.Sources.Duel, null, false));
    }

    [Fact]
    public void Duel_Cancelled_AddressesNobody() =>
        Assert.Null(NotificationAddressing.For(
            NotificationAddressing.Sources.Duel, null, DuelStatuses.Cancelled, Initiator, Responder));

    [Fact]
    public void Duel_Pending_IsNotShownToAStranger() =>
        Assert.NotEqual(Stranger, NotificationAddressing.For(
            NotificationAddressing.Sources.Duel, null, DuelStatuses.Pending, Initiator, Responder)!.UserId);
}
