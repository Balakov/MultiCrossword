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

        private const string c_completionTimeKeyPrefix = "crossword_complete_time_";
        private const string c_stateKeyPrefix = "crossword_state_";
        private const string c_solvedKeyPrefix = "crossword_state_solved_";
        private const string c_cellKeyPrefix = "cell_";

        public static RedisKey GameCompletionTimeKey(string gameId) => $"{c_completionTimeKeyPrefix}{gameId}";
        public static string GameIdFromGameCompletionTimeKey(string key) => key?.Substring(c_completionTimeKeyPrefix.Length);
        public static RedisKey GameStateKey(string gameId) => $"{c_stateKeyPrefix}{gameId}";
        public static RedisKey GameCompleteKey(string gameId) => $"{c_solvedKeyPrefix}{gameId}";

        public static string GameIdKey(string type, string number) => $"{type}_{number}";
        public static string CellIdKey(string x, string y) => $"{c_cellKeyPrefix}{x}_{y}";
        public static string CellIdKey(int x, int y) => CellIdKey(x.ToString(), y.ToString());

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

        public async Task RegisterComplete(string gameId, int seconds)
        {
            var db = m_redis.GetDatabase();
            await db.StringSetAsync(BoardStorage.GameCompletionTimeKey(gameId), $"{seconds.ToString()},{DateTime.Now.ToFileTime()}");
        }

        public class CrosswordCompletionInfo
        {
            public DateTime Date { get; set; }
            public int Seconds { get; set; }

            public static CrosswordCompletionInfo FromString(string value)
            {
                var info = new CrosswordCompletionInfo();

                string[] valueSplit = value.Split(',') ?? Array.Empty<string>();
                if (value.Length > 0)
                {
                    if (int.TryParse(valueSplit[0], out int seconds))
                    {
                        info.Seconds = seconds;

                        if (value.Length > 1)
                        {
                            if (long.TryParse(valueSplit[1], out long filetime))
                            {
                                info.Date = DateTime.FromFileTime(filetime);
                            }
                        }
                    }
                }

                return info;
            }
        }

        public async Task<CrosswordCompletionInfo> GetCompletionTime(string gameId)
        {
            var db = m_redis.GetDatabase();
            string value = await db.StringGetAsync(BoardStorage.GameCompletionTimeKey(gameId));
            if(!string.IsNullOrEmpty(value))
            {
                return CrosswordCompletionInfo.FromString(value);
            }

            return null;
        }

        public async Task<Dictionary<string, CrosswordCompletionInfo>> GetCompletionTimes(IEnumerable<string> gameIds)
        {
            var db = m_redis.GetDatabase();

            RedisKey[] gameIdKeys = gameIds.Select(x => new RedisKey(BoardStorage.GameCompletionTimeKey(x))).ToArray();
            RedisValue[] values = await db.StringGetAsync(gameIdKeys);
            Dictionary<string, CrosswordCompletionInfo> results = new();

            for (int i=0, max=values.Length; i < max; i++)
            {
                if (values[i].HasValue)
                {
                    results.TryAdd(BoardStorage.GameIdFromGameCompletionTimeKey(gameIdKeys[i]), CrosswordCompletionInfo.FromString(values[i]));
                }
            }

            return results;
        }
    }
}
