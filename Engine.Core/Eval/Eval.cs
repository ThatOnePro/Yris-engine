using Engine.Core.Board;
using Engine.Core.MoveGen;

public static class Eval
{
    // Use centipawns (integers) for better performance and precision
    private const int QueenVal = 900;
    private const int RookVal = 500;
    private const int BishopVal = 320;
    private const int KnightVal = 300;
    private const int PawnVal = 100;
    private const int MobilityWeight = 5; // 5 centipawns per legal move
    

    public static int MaterialScore(Position position)
    {
        int score = 0;

        // 1. Material Evaluation
        foreach (var piece in position.Squares)
        {
            score += GetPieceValue(piece);
        }

        // 2. Additive Mobility (instead of multiplicative)
        // Note: Ideally, pass the move list into Eval to avoid double generation
        var moves = MoveGenerator.GenerateLegalMoves(position);
        score += (position.SideToMove == Color.White) 
            ? moves.Count * MobilityWeight 
            : -moves.Count * MobilityWeight;

        // 3. Negamax requirement: Return score relative to the moving player
        return position.SideToMove == Color.White ? score : -score;
    }

    public static int GetPieceValue(Piece piece) => piece switch
    {
        Piece.WQ => QueenVal, Piece.BQ => -QueenVal,
        Piece.WR => RookVal,  Piece.BR => -RookVal,
        Piece.WB => BishopVal, Piece.BB => -BishopVal,
        Piece.WN => KnightVal, Piece.BN => -KnightVal,
        Piece.WP => PawnVal,   Piece.BP => -PawnVal,
        _ => 0
    };
}