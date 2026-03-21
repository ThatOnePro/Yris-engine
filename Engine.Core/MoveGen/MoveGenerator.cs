using Engine.Core.Board;
using Engine.Core.Moves;

namespace Engine.Core.MoveGen;

public static class MoveGenerator
{
    public static List<Move> GeneratePseudoLegalMoves(Position pos)
    {
        var moves = new List<Move>(64);

        for (int from = 0; from < 64; from++)
        {
            var piece = pos.Squares[from];
            if (piece == Piece.None) continue;

            if (pos.SideToMove == Color.White && !piece.IsWhite()) continue;
            if (pos.SideToMove == Color.Black && !piece.IsBlack()) continue;

            switch (piece)
            {
                case Piece.WN:
                case Piece.BN:
                    AddKnightMoves(pos, from, piece, moves);
                    break;

                case Piece.WP:
                case Piece.BP:
                    AddPawnMoves(pos, from, piece, moves);
                    break;

                case Piece.WB:
                case Piece.BB:
                    AddSlidingMoves(pos, from, piece, moves, BishopDirections);
                    break;

                case Piece.WR:
                case Piece.BR:
                    AddSlidingMoves(pos, from, piece, moves, RookDirections);
                    break;

                case Piece.WQ:
                case Piece.BQ:
                    AddSlidingMoves(pos, from, piece, moves, BishopDirections);
                    AddSlidingMoves(pos, from, piece, moves, RookDirections);
                    break;

                case Piece.WK:
                case Piece.BK:
                    AddKingMoves(pos, from, piece, moves);
                    break;
            }
        }

        return moves;
    }
    
    public static List<Move> GenerateLegalMoves(Position pos)
    {
        var pseudo = MoveGenerator.GeneratePseudoLegalMoves(pos);
        var legal = new List<Move>();

        foreach (var move in pseudo)
        {
            // In plaats van de bestaande 'pos' aan te passen, 
            // controleren we de legaliteit direct.
            if (IsMoveLegal(pos, move))
            {
                legal.Add(move);
            }
        }

        return legal;
    }

    private static bool IsMoveLegal(Position pos, Move move)
    {
        // Gebruik hier een TIJDELIJKE kopie of een hele lichte check
        pos.MakeMove(move);
        bool inCheck = AttackDetector.IsKingInCheck(pos, pos.SideToMove.Opposite());
        pos.UndoMove();
        return !inCheck;
    }
    
    private static readonly int[] KnightOffsets = { -17, -15, -10, -6, 6, 10, 15, 17 };

    private static void AddKnightMoves(Position pos, int from, Piece piece, List<Move> moves)
    {
        int fromFile = from % 8;
        int fromRank = from / 8;

        foreach (var o in KnightOffsets)
        {
            int to = from + o;
            if (to < 0 || to >= 64) continue;

            int toFile = to % 8;
            int toRank = to / 8;

            if (Math.Abs(fromFile - toFile) > 2 || Math.Abs(fromRank - toRank) > 2) continue;

            var target = pos.Squares[to];
            if (target == Piece.None || (piece.IsWhite() && target.IsBlack()) || (piece.IsBlack() && target.IsWhite()))
                moves.Add(new Move(from, to));
        }
    }

    private static void AddPawnMoves(Position pos, int from, Piece piece, List<Move> moves)
    {
        bool white = piece.IsWhite();
        int dir = white ? 8 : -8;
        int startRank = white ? 1 : 6;
        int promotionRank = white ? 6 : 1;

        int fromRank = from / 8;
        int fromFile = from % 8;

        int one = from + dir;
        if (one is >= 0 and < 64 && pos.Squares[one] == Piece.None)
        {
            if (fromRank == promotionRank) AddPromotionMoves(from, one, white, moves);
            else
            {
                moves.Add(new Move(from, one));
                if (fromRank == startRank)
                {
                    int two = from + 2 * dir;
                    if (pos.Squares[two] == Piece.None) moves.Add(new Move(from, two));
                }
            }
        }

        int[] caps = white ? new[] { 7, 9 } : new[] { -7, -9 };
        foreach (var o in caps)
        {
            int to = from + o;
            if (to < 0 || to >= 64) continue;

            int toFile = to % 8;
            if (Math.Abs(fromFile - toFile) != 1) continue;

            var target = pos.Squares[to];
            if (target != Piece.None && ((white && target.IsBlack()) || (!white && target.IsWhite())))
            {
                if (fromRank == promotionRank) AddPromotionMoves(from, to, white, moves);
                else moves.Add(new Move(from, to));
                continue;
            }

            if (to == pos.EnPassantSquare)
                moves.Add(new Move(from, to));
        }
    }

    private static void AddPromotionMoves(int from, int to, bool white, List<Move> moves)
    {
        moves.Add(new Move(from, to, white ? Piece.WQ : Piece.BQ));
        moves.Add(new Move(from, to, white ? Piece.WR : Piece.BR));
        moves.Add(new Move(from, to, white ? Piece.WB : Piece.BB));
        moves.Add(new Move(from, to, white ? Piece.WN : Piece.BN));
    }

    private static readonly int[] BishopDirections = { -9, -7, 7, 9 };
    private static readonly int[] RookDirections   = { -8, -1, 1, 8 };
    
    private static void AddSlidingMoves(Position pos, int from, Piece piece, List<Move> moves, int[] directions)
    {
        bool white = piece.IsWhite();
        int fromFile = from % 8;

        foreach (var dir in directions)
        {
            int to = from;

            while (true)
            {
                int next = to + dir;
                if (next < 0 || next >= 64)
                    break;

                int nextFile = next % 8;

                if (Math.Abs(nextFile - (to % 8)) > 1 && (dir == -1 || dir == 1))
                    break;

                if ((dir == -9 || dir == 7) && nextFile > (to % 8))
                    break;

                if ((dir == -7 || dir == 9) && nextFile < (to % 8))
                    break;

                var target = pos.Squares[next];

                if (target == Piece.None)
                {
                    moves.Add(new Move(from, next));
                }
                else
                {
                    if ((white && target.IsBlack()) || (!white && target.IsWhite()))
                        moves.Add(new Move(from, next));

                    break; 
                }

                to = next;
            }
        }
    }
    
    private static readonly int[] KingOffsets =
    {
        -9, -8, -7,
        -1,      1,
        7,  8,  9
    };

    private static void AddKingMoves(Position pos, int from, Piece piece, List<Move> moves)
    {
        bool white = piece.IsWhite();
        int fromFile = from % 8;

        foreach (var o in KingOffsets)
        {
            int to = from + o;
            if (to < 0 || to >= 64)
                continue;

            int toFile = to % 8;

            if (Math.Abs(fromFile - toFile) > 1)
                continue;

            var target = pos.Squares[to];

            if (target == Piece.None ||
                (white && target.IsBlack()) ||
                (!white && target.IsWhite()))
            {
                moves.Add(new Move(from, to));
            }
        }

        AddCastlingMoves(pos, from, piece, moves);
    }

    private static void AddCastlingMoves(Position pos, int from, Piece piece, List<Move> moves)
    {
        if (piece == Piece.WK && from == 4)
        {
            if ((pos.Castling & CastlingRights.WhiteKingSide) != 0 &&
                pos.Squares[5] == Piece.None && pos.Squares[6] == Piece.None &&
                !AttackDetector.IsSquareAttacked(pos, 4, Color.Black) &&
                !AttackDetector.IsSquareAttacked(pos, 5, Color.Black) &&
                !AttackDetector.IsSquareAttacked(pos, 6, Color.Black))
            {
                moves.Add(new Move(4, 6));
            }

            if ((pos.Castling & CastlingRights.WhiteQueenSide) != 0 &&
                pos.Squares[1] == Piece.None && pos.Squares[2] == Piece.None && pos.Squares[3] == Piece.None &&
                !AttackDetector.IsSquareAttacked(pos, 4, Color.Black) &&
                !AttackDetector.IsSquareAttacked(pos, 3, Color.Black) &&
                !AttackDetector.IsSquareAttacked(pos, 2, Color.Black))
            {
                moves.Add(new Move(4, 2));
            }
        }

        if (piece == Piece.BK && from == 60)
        {
            if ((pos.Castling & CastlingRights.BlackKingSide) != 0 &&
                pos.Squares[61] == Piece.None && pos.Squares[62] == Piece.None &&
                !AttackDetector.IsSquareAttacked(pos, 60, Color.White) &&
                !AttackDetector.IsSquareAttacked(pos, 61, Color.White) &&
                !AttackDetector.IsSquareAttacked(pos, 62, Color.White))
            {
                moves.Add(new Move(60, 62));
            }

            if ((pos.Castling & CastlingRights.BlackQueenSide) != 0 &&
                pos.Squares[57] == Piece.None && pos.Squares[58] == Piece.None && pos.Squares[59] == Piece.None &&
                !AttackDetector.IsSquareAttacked(pos, 60, Color.White) &&
                !AttackDetector.IsSquareAttacked(pos, 59, Color.White) &&
                !AttackDetector.IsSquareAttacked(pos, 58, Color.White))
            {
                moves.Add(new Move(60, 58));
            }
        }
    }
}