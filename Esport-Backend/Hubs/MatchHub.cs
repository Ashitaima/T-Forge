using Microsoft.AspNetCore.SignalR;

namespace TForge.Hubs
{
    /// <summary>
    /// Трансляція живого рахунку. Підписка відкрита для всіх — рахунок є публічною
    /// інформацією; змінювати його можна лише через захищений REST-ендпоінт.
    /// </summary>
    public class MatchHub : Hub
    {
        public static string GroupFor(int matchId) => $"match-{matchId}";

        public Task SubscribeToMatch(int matchId) =>
            Groups.AddToGroupAsync(Context.ConnectionId, GroupFor(matchId));

        public Task UnsubscribeFromMatch(int matchId) =>
            Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupFor(matchId));
    }

    /// <summary>Події, які надсилає сервер. Назви збігаються з обробниками на клієнті.</summary>
    public static class MatchHubEvents
    {
        public const string ScoreUpdated = "ScoreUpdated";
        public const string MatchStatusChanged = "MatchStatusChanged";
    }
}
