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
    private const int DrawContempt = 12;
    private const int MaterialContemptDivisor = 10;
    private const int MaxMaterialContempt = 50;
    private const int RootRepetitionPenaltyWindow = 6;
    private const int RootRepetitionPenaltyStep = 2;
    

    public static int MaterialScore(Position position)
    {
        int score = MaterialOnlyScore(position);

        // 2. Additive Mobility (instead of multiplicative)
        // Note: Ideally, pass the move list into Eval to avoid double generation
        var moves = MoveGenerator.GenerateLegalMoves(position);
        score += (position.SideToMove == Color.White) 
            ? moves.Count * MobilityWeight 
            : -moves.Count * MobilityWeight;

        // 3. Negamax requirement: Return score relative to the moving player
        return position.SideToMove == Color.White ? score : -score;
    }

    public static int MaterialOnlyScore(Position position)
    {
        int score = 0;

        foreach (var piece in position.Squares)
        {
            score += GetPieceValue(piece);
        }

        return score;
    }

    public static int GetRepetitionScore(Position position, int ply)
    {
        int materialScore = MaterialOnlyScore(position);
        int materialForSideToMove = position.SideToMove == Color.White ? materialScore : -materialScore;

        int materialContempt = Math.Clamp(
            materialForSideToMove / MaterialContemptDivisor,
            -MaxMaterialContempt,
            MaxMaterialContempt);

        // If side to move is ahead, repetition should be less attractive (more negative).
        int drawScore = -DrawContempt - materialContempt;

        int nearRootPenalty = Math.Max(0, RootRepetitionPenaltyWindow - ply) * RootRepetitionPenaltyStep;
        return drawScore - nearRootPenalty;
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