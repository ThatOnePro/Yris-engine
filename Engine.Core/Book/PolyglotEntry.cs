namespace Engine.Core.Book;

public struct PolyglotEntry
{
    public ulong Key;     // Zobrist hash
    public ushort Move;   // Encoded move
    public ushort Weight; // Frequency
    public uint Learn;    // Ignore
}