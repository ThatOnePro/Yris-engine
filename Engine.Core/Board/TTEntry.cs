using Engine.Core.Moves;

public enum TTFlag
{
    Exact,
    LowerBound,
    UpperBound
}

public struct TTEntry
{
    public ulong Key;
    public int Depth;
    public int Score;
    public TTFlag Flag;
    public Move BestMove;
}