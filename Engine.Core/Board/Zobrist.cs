namespace Engine.Core.Board;

public static class Zobrist
{
    // [StukType, Kleur, VeldIndex]
    private static readonly ulong[,,] Table = new ulong[8, 2, 64];
    public static readonly ulong SideToMove;

    static Zobrist()
    {
        var rnd = new Random(42); // Gebruik een 'seed' voor consistente hashes
        for (int piece = 0; piece < 8; piece++)
        for (int color = 0; color < 2; color++)
        for (int square = 0; square < 64; square++)
            Table[piece, color, square] = RandomUlong(rnd);

        SideToMove = RandomUlong(rnd);
    }

    private static ulong RandomUlong(Random rnd)
    {
        byte[] buffer = new byte[8];
        rnd.NextBytes(buffer);
        return BitConverter.ToUInt64(buffer, 0);
    }

    public static ulong GetPieceHash(int piece, int color, int square) 
        => Table[piece, color, square];
}
