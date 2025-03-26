using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;

namespace Crossword.Models
{
    public class GameActionsHub : Hub
    {
        private MoveQueueService m_queueService;
        private Storage.BoardStorageService m_boardStorageService;
        private static readonly ConcurrentDictionary<string, string> m_connectionGroups = new ConcurrentDictionary<string, string>();

        public GameActionsHub(MoveQueueService queueService, Storage.BoardStorageService boardStorageService)
        {
            m_queueService = queueService;
            m_boardStorageService = boardStorageService;
        }

        public override async Task OnConnectedAsync()
        {
            string gameId = Context.GetHttpContext()?.Request.Query["gameId"];

            if (!string.IsNullOrEmpty(gameId))
            {
                m_connectionGroups[Context.ConnectionId] = gameId;
                await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            if (m_connectionGroups.TryRemove(Context.ConnectionId, out string gameId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, gameId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMove(string playerId, string gameId, string x, string y, string c)
        {
            m_queueService.EnqueueMove(gameId, x, y, c);
            await Clients.Group(gameId).SendAsync("ReceiveMove", playerId, x, y, c);
        }

        public async Task SelectClue(string playerId, string gameId, string x, string y, string direction)
        {
            await Clients.Group(gameId).SendAsync("ReceiveRemoteSelect", playerId, x, y, direction);
        }

        public async Task ClearGrid(string playerId, string gameId)
        {
            await m_boardStorageService.ClearBoard(gameId);
            await Clients.Group(gameId).SendAsync("ReceiveClearGrid", playerId);
        }

        public async Task RevealGrid(string playerId, string gameId)
        {
            await m_boardStorageService.CompleteBoard(gameId);
            await Clients.Group(gameId).SendAsync("ReceiveRevealGrid", playerId);
        }
    }
}