using StackExchange.Redis;

namespace Crossword.Models
{
    public class MoveQueueProcessorService : BackgroundService
    {
        private readonly MoveQueueService m_moveQueueService;
        private readonly Storage.BoardStorageService m_boardStorageService;

        public MoveQueueProcessorService(MoveQueueService queueService, Storage.BoardStorageService boardStorageService)
        {
            m_moveQueueService = queueService;
            m_boardStorageService = boardStorageService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    List<MoveQueueService.MoveData> moves = await m_moveQueueService.DequeueAllAsync(stoppingToken);

                    if (moves.Count > 0)
                    {
                        // Group the moves by game id
                        var movesByGameId = moves.GroupBy(m => m.GameId)
                                                 .ToDictionary(g => g.Key, g => g.ToList());

                        foreach (var game in movesByGameId)
                        {
                            var gameBoard = await m_boardStorageService.GetBoardStateAsync(game.Key);

                            foreach (var move in game.Value)
                            {
                                string cellId = Storage.BoardStorage.CellId(move.CellX, move.CellY);

                                if (string.IsNullOrEmpty(move.Value))
                                {
                                    // Delete a value
                                    gameBoard.Cells.Remove(cellId);
                                }
                                else
                                {
                                    // Add a value
                                    if (!gameBoard.Cells.TryGetValue(cellId, out var existingValue))
                                    {
                                        existingValue = new Storage.BoardStorage.CellData();
                                        gameBoard.Cells.Add(cellId, existingValue);
                                    }

                                    existingValue.Value = move.Value;
                                }
                            }

                            await m_boardStorageService.SetBoardStateAsync(game.Key, gameBoard);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }
}
