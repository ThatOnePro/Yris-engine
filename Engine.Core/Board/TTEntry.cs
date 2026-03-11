using Engine.Core.Moves;

namespace Engine.Core.Board;


public enum HashFlag : byte { Exact, LowerBound, UpperBound }
public struct TTEntry
{
    public ulong Key;      // De Zobrist Hash
    public int Depth;      // Hoe diep was de zoekopdracht?
    public double Value;   // De score
    public HashFlag Flag;  // Is dit de exacte score, of een alpha/beta cutoff?
    public Move BestMove;  // De beste zet uit deze stelling (voor move ordering!)
}