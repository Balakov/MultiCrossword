using System.Collections.Concurrent;

namespace Crossword.Models
{
    public class MoveQueueService
    {
        public class MoveData
        {
            public MoveData(string cellX, string cellY, string value, string gameId)
            {
                CellX = cellX;
                CellY = cellY;
                Value = value;
                GameId = gameId;
            }

            public readonly string CellX;
            public readonly string CellY;
            public readonly string Value;
            public readonly string GameId;
        }

        private ConcurrentQueue<MoveData> m_queue = new ConcurrentQueue<MoveData>();
        private SemaphoreSlim m_signal = new SemaphoreSlim(0);

        public void EnqueueMove(string gameId, string x, string y, string value)
        {
            m_queue.Enqueue(new MoveData(x, y, value, gameId));
            m_signal.Release(); // Notify the processor
        }

        public async Task<List<MoveData>> DequeueAllAsync(CancellationToken cancellationToken)
        {
            var batch = new List<MoveData>();

            // Wait for at least one message
            await m_signal.WaitAsync(cancellationToken);

            // Process everything available in the queue
            while (m_queue.TryDequeue(out var message))
            {
                batch.Add(message);
            }

            return batch;
        }
    }
}