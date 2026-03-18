using Engine.Core.Board;
using Engine.Core.Moves;

namespace Engine.Core.Book.bins;

public static class OpeningBook
{
    private static List<PolyglotEntry> entries = new();

    public static void Load(string path)
    {
        entries.Clear();

        using var fs = new FileStream(path, FileMode.Open);
        using var br = new BinaryReader(fs);

        while (fs.Position < fs.Length)
        {
            PolyglotEntry entry = new()
            {
                Key = ReadUInt64BigEndian(br),
                Move = ReadUInt16BigEndian(br),
                Weight = ReadUInt16BigEndian(br),
                Learn = ReadUInt32BigEndian(br)
            };

            entries.Add(entry);
        }
    }

    private static ulong ReadUInt64BigEndian(BinaryReader br)
    {
        var bytes = br.ReadBytes(8);
        Array.Reverse(bytes);
        return BitConverter.ToUInt64(bytes, 0);
    }

    private static ushort ReadUInt16BigEndian(BinaryReader br)
    {
        var bytes = br.ReadBytes(2);
        Array.Reverse(bytes);
        return BitConverter.ToUInt16(bytes, 0);
    }

    private static uint ReadUInt32BigEndian(BinaryReader br)
    {
        var bytes = br.ReadBytes(4);
        Array.Reverse(bytes);
        return BitConverter.ToUInt32(bytes, 0);
    }
    
    public static Move DecodeMove(ushort polyMove, Color sideToMove)
    {
        int from = (polyMove >> 6) & 0x3F;
        int to = polyMove & 0x3F;
        int promo = (polyMove >> 12) & 0x7;

        // Polyglot encodes castling as king-from to rook-from squares.
        if (from == 4 && to == 7) to = 6;
        else if (from == 4 && to == 0) to = 2;
        else if (from == 60 && to == 63) to = 62;
        else if (from == 60 && to == 56) to = 58;

        bool white = sideToMove == Color.White;
        Piece promotionPiece = promo switch
        {
            1 => white ? Piece.WN : Piece.BN,
            2 => white ? Piece.WB : Piece.BB,
            3 => white ? Piece.WR : Piece.BR,
            4 => white ? Piece.WQ : Piece.BQ,
            _ => Piece.None
        };

        return new Move(from, to, promotionPiece);
    }
    
    public static Move? GetBookMove(ulong polyglotKey, Color sideToMove)
    {
        var moves = new List<PolyglotEntry>();

        foreach (var entry in entries)
        {
            if (entry.Key == polyglotKey)
                moves.Add(entry);
        }

        if (moves.Count == 0)
            return null;

        // Weighted random selection
        int totalWeight = moves.Sum(e => e.Weight);
        int rnd = Random.Shared.Next(totalWeight);

        foreach (var entry in moves)
        {
            rnd -= entry.Weight;
            if (rnd <= 0)
                return DecodeMove(entry.Move, sideToMove);
        }

        return DecodeMove(moves[0].Move, sideToMove);
    }
}

