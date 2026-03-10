using Engine.Core.Board;

namespace Engine.Core.Moves;

public static class AttackDetector
{
    public static bool IsSquareAttacked(Position pos, int square, Color bySide)
    {
        return PawnAttacks(pos, square, bySide)
               || KnightAttacks(pos, square, bySide)
               || BishopAttacks(pos, square, bySide)
               || RookAttacks(pos, square, bySide)
               || KingAttacks(pos, square, bySide);
    }
    
    
    private static bool PawnAttacks(Position pos, int square, Color bySide)
    {
        int file = square % 8;

        if (bySide == Color.White)
        {
            int left = square - 9;
            int right = square - 7;

            if (file > 0 && left >= 0 && pos.Squares[left] == Piece.WP)
                return true;

            if (file < 7 && right >= 0 && pos.Squares[right] == Piece.WP)
                return true;
        }
        else
        {
            int left = square + 7;
            int right = square + 9;

            if (file > 0 && left < 64 && pos.Squares[left] == Piece.BP)
                return true;
            
            if (file < 7 && right < 64 && pos.Squares[right] == Piece.BP)
                return true;

        }

        return false;
    }
    
    private static readonly int[] KnightOffsets =
    {
        -17, -15, -10, -6, 6, 10, 15, 17
    };

    private static bool KnightAttacks(Position pos, int square, Color bySide)
    {
        int file = square % 8;

        foreach (var offset in KnightOffsets)
        {
            int to = square + offset;

            if (to < 0 || to >= 64)
                continue;

            int toFile = to % 8;
            if (Math.Abs(file - toFile) > 2)
                continue;

            var piece = pos.Squares[to];

            if (bySide == Color.White && piece == Piece.WN)
                return true;

            if (bySide == Color.Black && piece == Piece.BN)
                return true;
        }

        return false;
    }
    
    private static readonly int[] BishopDirs = { -9, -7, 7, 9 };

    private static bool BishopAttacks(Position pos, int square, Color bySide)
    {
        foreach (var dir in BishopDirs)
        {
            int s = square;

            while (true)
            {
                int next = s + dir;

                if (next < 0 || next >= 64)
                    break;

                if (Math.Abs((next % 8) - (s % 8)) > 1)
                    break;

                var piece = pos.Squares[next];

                if (piece != Piece.None)
                {
                    if (bySide == Color.White && (piece == Piece.WB || piece == Piece.WQ))
                        return true;

                    if (bySide == Color.Black && (piece == Piece.BB || piece == Piece.BQ))
                        return true;

                    break;
                }

                s = next;
            }
        }

        return false;
    }
    
    private static readonly int[] RookDirs = { -8, -1, 1, 8 };

    private static bool RookAttacks(Position pos, int square, Color bySide)
    {
        foreach (var dir in RookDirs)
        {
            int s = square;

            while (true)
            {
                int next = s + dir;

                if (next < 0 || next >= 64)
                    break;

                if ((dir == -1 || dir == 1) && Math.Abs((next % 8) - (s % 8)) != 1)
                    break;

                var piece = pos.Squares[next];

                if (piece != Piece.None)
                {
                    if (bySide == Color.White && (piece == Piece.WR || piece == Piece.WQ))
                        return true;

                    if (bySide == Color.Black && (piece == Piece.BR || piece == Piece.BQ))
                        return true;

                    break;
                }

                s = next;
            }
        }

        return false;
    }
    
    private static readonly int[] KingOffsets =
    {
        -9, -8, -7,
        -1,      1,
        7,  8,  9
    };

    private static bool KingAttacks(Position pos, int square, Color bySide)
    {
        int file = square % 8;

        foreach (var o in KingOffsets)
        {
            int to = square + o;

            if (to < 0 || to >= 64)
                continue;

            int toFile = to % 8;

            if (Math.Abs(file - toFile) > 1)
                continue;

            var piece = pos.Squares[to];

            if (bySide == Color.White && piece == Piece.WK)
                return true;

            if (bySide == Color.Black && piece == Piece.BK)
                return true;
        }

        return false;
    }
    
    public static bool IsKingInCheck(Position pos, Color side)
    {
        int kingSquare = -1;

        for (int i = 0; i < 64; i++)
        {
            if (pos.Squares[i] == (side == Color.White ? Piece.WK : Piece.BK))
            {
                kingSquare = i;
                break;
            }
        }

        return IsSquareAttacked(pos, kingSquare, side.Opposite());
    }
    
}