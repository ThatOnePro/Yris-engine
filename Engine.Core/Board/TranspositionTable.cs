using Engine.Core.Moves;

public static class TranspositionTable
{
    private const int Size = 1 << 20; // ~1M entries

    private static TTEntry[] table = new TTEntry[Size];

    public static void Store(ulong key, int depth, int score, TTFlag flag, Move bestMove)
    {
        int index = (int)(key % Size);

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

        bestMove = entry.BestMove;

        if (entry.Key == key && entry.Depth >= depth)
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