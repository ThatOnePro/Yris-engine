using Engine.Core.Board;
using Engine.Core.MoveGen;

public static class Eval
{
    private static int queenWt = 9;
    private static int rookWt = 5;
    private static int bishopWt = 3;
    private static int knightWt = 3;
    private static int pawnWt = 1;
    private static double doubledIsolatedWt = -0.5;


    public static int GetPieceValue(Piece piece)
    {
        switch (piece)
        {
            case Piece.WN:
            case Piece.BN:
                return knightWt;
            
            case Piece.WP:
            case Piece.BP:
                return pawnWt;

            case Piece.WB:
            case Piece.BB:
                return bishopWt;

            case Piece.WR:
            case Piece.BR:
                return rookWt;

            case Piece.WQ:
            case Piece.BQ:
                return queenWt;
        }
        return 0;
    }


    public static int FindDoubledPawns(Position position, char colour)
    {
        int counter = 0;
        for (int i = 0; i < 8; i++)
        {
            int pawnsInColumn = 0;
            for (int j = i; j < 64; j += 8)
            {
                if (colour == 'W')
                {
                    if (position.Squares[j] == Piece.BP) pawnsInColumn++;
                }
                else if (colour == 'B')
                {
                    if (position.Squares[j] == Piece.BP) pawnsInColumn++;
                }
            }

            if (pawnsInColumn > 1) counter++;
        }

        return counter;
    }


    public static double
        MaterialScore(
            Position position) // isolated pawns and development(amount of legal moves) still need to be added.
    {
        double score = queenWt *
                       (position.Squares.Count(s => s == Piece.WQ) - position.Squares.Count(s => s == Piece.BQ));
        score += rookWt * (position.Squares.Count(s => s == Piece.WR) - position.Squares.Count(s => s == Piece.BR));
        score += bishopWt * (position.Squares.Count(s => s == Piece.WB) - position.Squares.Count(s => s == Piece.BB));
        score += knightWt * (position.Squares.Count(s => s == Piece.WN) - position.Squares.Count(s => s == Piece.BN));
        score += pawnWt * (position.Squares.Count(s => s == Piece.WP) - position.Squares.Count(s => s == Piece.BP));

        score += doubledIsolatedWt * (FindDoubledPawns(position, 'W') - FindDoubledPawns(position, 'B'));

        Position whitePosition = new Position //niet performant, Axel schuldig verklaard
        {
            Castling = position.Castling,
            SideToMove = Color.White,
            EnPassantSquare = position.EnPassantSquare,
            FullmoveNumber = position.FullmoveNumber,
            HalfmoveClock = position.HalfmoveClock
        };

        double whiteMoves = MoveGenerator.GeneratePseudoLegalMoves(whitePosition).Count;

        Position blackPosition = new Position
        {
            Castling = position.Castling,
            SideToMove = Color.Black,
            EnPassantSquare = position.EnPassantSquare,
            FullmoveNumber = position.FullmoveNumber,
            HalfmoveClock = position.HalfmoveClock
        };

        double blackMoves = MoveGenerator.GeneratePseudoLegalMoves(blackPosition).Count;

        score = score * (whiteMoves / blackMoves);


        if (position.SideToMove == Color.Black)
        {
            score = -1 * score;
        }

        return score;
    }
}