using System.Text.Json;
using StackExchange.Redis;

namespace Crossword.Storage
{
    public class BoardStorage
    {
        public class CellData
        {
            public string Value { get; set; }
        }

        public Dictionary<string, CellData> Cells { get; set; } = new();

        public static RedisKey GameStateKey(string gameId) => $"crossword_state_{gameId}";
        public static RedisKey GameCompleteKey(string gameId) => $"crossword_state_complete_{gameId}";
        public static string CellId(string x, string y) => $"cell_{x}_{y}";
        public static string CellId(int x, int y) => CellId(x.ToString(), y.ToString());

        public static RedisKey GameJSONKeyFromURL(string url)
        {
            Uri uri = new Uri(url);
            string key = uri.AbsolutePath.Replace('/', '_');
            return $"crossword_json_{key}";
        }
    }

    public class BoardStorageService
    {
        private readonly ConnectionMultiplexer m_redis;

        public BoardStorageService()
        {
            m_redis = ConnectionMultiplexer.Connect("localhost");
        }

        public async Task<string> GetBoardJSONAsync(string url)
        {
            var db = m_redis.GetDatabase();
            RedisValue existingGame = await db.StringGetAsync(BoardStorage.GameJSONKeyFromURL(url));
            return existingGame.HasValue ? existingGame : (string)null;
        }

        public async Task SetBoardJSONAsync(string url, string gameJSON)
        {
            var db = m_redis.GetDatabase();
            await db.StringSetAsync(BoardStorage.GameJSONKeyFromURL(url), gameJSON);
        }

        public async Task<BoardStorage> GetBoardStateCompleteAsync(string gameId) => await GetBoardStateAsync(gameId, BoardStorage.GameCompleteKey(gameId));
        public async Task<BoardStorage> GetBoardStateAsync(string gameId) => await GetBoardStateAsync(gameId, BoardStorage.GameStateKey(gameId));
        private async Task<BoardStorage> GetBoardStateAsync(string gameId, RedisKey key)
        {
            var db = m_redis.GetDatabase();

            RedisValue existingGame = await db.StringGetAsync(key);
            BoardStorage gameBoard = null;

            if (existingGame.HasValue)
            {
                gameBoard = JsonSerializer.Deserialize<BoardStorage>(existingGame);
            }

            return gameBoard ?? new BoardStorage();
        }

        public async Task SetBoardStateCompleteAsync(string gameId, BoardStorage board) => await SetBoardStateAsync(gameId, board, BoardStorage.GameCompleteKey(gameId));
        public async Task SetBoardStateAsync(string gameId, BoardStorage board) => await SetBoardStateAsync(gameId, board, BoardStorage.GameStateKey(gameId));
        private async Task SetBoardStateAsync(string gameId, BoardStorage board, RedisKey key)
        {
            var db = m_redis.GetDatabase();
            await db.StringSetAsync(key, JsonSerializer.Serialize(board));
        }

        public async Task ClearBoard(string gameId)
        {
            var db = m_redis.GetDatabase();
            await db.KeyDeleteAsync(BoardStorage.GameStateKey(gameId));
        }

        public async Task CompleteBoard(string gameId)
        {
            // Copy the completed board to the current board state
            var completedBoard = await GetBoardStateCompleteAsync(gameId);
            if (completedBoard.Cells.Any())
            {
                await SetBoardStateAsync(gameId, completedBoard);
            }
        }
    }
}
