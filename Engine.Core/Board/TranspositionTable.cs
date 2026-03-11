using Engine.Core.Moves;

namespace Engine.Core.Board;

public class TranspositionTable
{
    private readonly TTEntry[] _table;
    private readonly ulong _size;

    public TranspositionTable(int sizeMb)
    {
        // Bereken hoeveel entries er in het geheugen passen
        int entrySize = System.Runtime.InteropServices.Marshal.SizeOf<TTEntry>();
        _size = (ulong)(sizeMb * 1024 * 1024 / entrySize);
        _table = new TTEntry[_size];
    }

    public void Store(ulong key, int depth, double value, HashFlag flag, Move bestMove)
    {
        ulong index = key % _size;

        // Eenvoudige vervangingsstrategie: 
        // Overschrijf als de nieuwe zoektocht dieper was (en dus betrouwbaarder)
        if (_table[index].Key == 0 || _table[index].Depth <= depth)
        {
            _table[index] = new TTEntry
            {
                Key = key,
                Value = (short)value,
                Depth = (short)depth,
                Flag = flag,
                BestMove = bestMove
            };
        }
    }

    public TTEntry? Lookup(ulong key)
    {
        ulong index = key % _size;
        TTEntry entry = _table[index];

        if (entry.Key == key) return entry;
        return null;
    }
}