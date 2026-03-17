using Engine.Core.Moves;

public static class TranspositionTable
{
    private const int Size = 1 << 20; // ~1M entries

    private static TTEntry[] table = new TTEntry[Size];

    public static void Clear()
    {
        Array.Clear(table, 0, table.Length);
    }

    public static void Store(ulong key, int depth, int score, TTFlag flag, Move bestMove)
    {
        int index = (int)(key % Size);
        var existing = table[index];

        // Prefer replacing same-key entries or entries with lower searched depth.
        if (existing.Key != key && existing.Depth > depth)
        {
            return;
        }

        table[index] = new TTEntry
        {
            Key = key,
            Depth = depth,
            Score = score,
            Flag = flag,
            BestMove = bestMove
        };
    }

    public static bool Probe(ulong key, int depth, int alpha, int beta, out int score, out Move bestMove)
    {
        int index = (int)(key % Size);
        var entry = table[index];

        bestMove = default;

        if (entry.Key != key)
        {
            score = 0;
            return false;
        }

        bestMove = entry.BestMove;

        if (entry.Depth >= depth)
        {
            if (entry.Flag == TTFlag.Exact)
            {
                score = entry.Score;
                return true;
            }

            if (entry.Flag == TTFlag.LowerBound && entry.Score >= beta)
            {
                score = entry.Score;
                return true;
            }

            if (entry.Flag == TTFlag.UpperBound && entry.Score <= alpha)
            {
                score = entry.Score;
                return true;
            }
        }

        score = 0;
        return false;
    }
}