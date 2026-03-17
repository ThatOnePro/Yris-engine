using Engine.Core.Board;

public static class Zobrist
{
    public static ulong[,] PieceKeys = new ulong[12, 64];
    public static ulong SideKey;
    public static ulong[] CastlingKeys = new ulong[16];
    public static ulong[] EnPassantKeys = new ulong[64];

    private static Random rng = new Random(123456);

    static Zobrist()
    {
        Init();
    }

    public static void Init()
    {
        for (int piece = 0; piece < 12; piece++)
        for (int sq = 0; sq < 64; sq++)
            PieceKeys[piece, sq] = Random64();

        for (int i = 0; i < 16; i++)
            CastlingKeys[i] = Random64();

        for (int i = 0; i < 64; i++)
            EnPassantKeys[i] = Random64();

        SideKey = Random64();
    }

    private static ulong Random64()
    {
        byte[] buffer = new byte[8];
        rng.NextBytes(buffer);
        return BitConverter.ToUInt64(buffer, 0);
    }
    
    public static ulong ComputeHash(Position pos)
    {
        ulong hash = 0;

        for (int sq = 0; sq < 64; sq++)
        {
            var piece = pos.Squares[sq];
            if (piece != Piece.None)
                hash ^= Zobrist.PieceKeys[(int)piece - 1, sq];
        }

        hash ^= Zobrist.CastlingKeys[(int)pos.Castling];

        if (pos.EnPassantSquare != -1)
            hash ^= Zobrist.EnPassantKeys[pos.EnPassantSquare];

        if (pos.SideToMove == Color.Black)
            hash ^= Zobrist.SideKey;

        return hash;
    }
}