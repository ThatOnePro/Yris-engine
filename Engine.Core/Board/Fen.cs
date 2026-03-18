using Engine.Core.Board;

namespace Engine.Core.Board;

public static class Fen
{
    public static Position Parse(string fen)
    {
        var parts = fen.Split(' ');

        var position = new Position();

        ParseBoard(parts[0], position);
        ParseSide(parts[1], position);
        ParseCastling(parts[2], position);
        ParseEnPassant(parts[3], position);

        position.HalfmoveClock = int.Parse(parts[4]);
        position.FullmoveNumber = int.Parse(parts[5]);
        position.ZobristKey = Zobrist.ComputeHash(position);
        position.PolyglotKey = Polyglot.ComputeHash(position);

        return position;
    }

    private static void ParseBoard(string board, Position pos)
    {
        int square = 56; // start at a8

        foreach (char c in board)
        {
            if (c == '/')
            {
                square -= 16;
                continue;
            }

            if (char.IsDigit(c))
            {
                square += c - '0';
                continue;
            }

            pos.Squares[square] = CharToPiece(c);
            square++;
        }
    }

    private static void ParseSide(string side, Position pos)
    {
        pos.SideToMove = side == "w" ? Color.White : Color.Black;
    }

    private static void ParseCastling(string castling, Position pos)
    {
        pos.Castling = CastlingRights.None;

        if (castling.Contains('K')) pos.Castling |= CastlingRights.WhiteKingSide;
        if (castling.Contains('Q')) pos.Castling |= CastlingRights.WhiteQueenSide;
        if (castling.Contains('k')) pos.Castling |= CastlingRights.BlackKingSide;
        if (castling.Contains('q')) pos.Castling |= CastlingRights.BlackQueenSide;
    }

    private static void ParseEnPassant(string ep, Position pos)
    {
        if (ep == "-")
        {
            pos.EnPassantSquare = -1;
            return;
        }

        int file = ep[0] - 'a';
        int rank = ep[1] - '1';

        pos.EnPassantSquare = rank * 8 + file;
    }

    private static Piece CharToPiece(char c)
    {
        return c switch
        {
            'P' => Piece.WP,
            'N' => Piece.WN,
            'B' => Piece.WB,
            'R' => Piece.WR,
            'Q' => Piece.WQ,
            'K' => Piece.WK,

            'p' => Piece.BP,
            'n' => Piece.BN,
            'b' => Piece.BB,
            'r' => Piece.BR,
            'q' => Piece.BQ,
            'k' => Piece.BK,

            _ => Piece.None
        };
    }
}