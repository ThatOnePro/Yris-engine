using Engine.Core.Board;
using Engine.Core.MoveGen;

public static class Eval
{
    // Static values used by move ordering / MVV-LVA.
    private const int QueenVal = 900;
    private const int RookVal = 500;
    private const int BishopVal = 320;
    private const int KnightVal = 300;
    private const int PawnVal = 100;

    // Middlegame / endgame material values used by tapered evaluation.
    private const int QueenValMg = 900;
    private const int QueenValEg = 900;
    private const int RookValMg = 500;
    private const int RookValEg = 520;
    private const int BishopValMg = 330;
    private const int BishopValEg = 350;
    private const int KnightValMg = 320;
    private const int KnightValEg = 300;
    private const int PawnValMg = 100;
    private const int PawnValEg = 120;

    private const int MobilityWeightMg = 5;
    private const int MobilityWeightEg = 3;

    // Standard game phase model (0 = pure endgame, MaxPhase = opening).
    private const int PhaseKnight = 1;
    private const int PhaseBishop = 1;
    private const int PhaseRook = 2;
    private const int PhaseQueen = 4;
    private const int MaxPhase = 24;

    private const int DrawContempt = 12;
    private const int MaterialContemptDivisor = 10;
    private const int MaxMaterialContempt = 50;
    private const int RootRepetitionPenaltyWindow = 6;
    private const int RootRepetitionPenaltyStep = 2;
    
    public static int MaterialScore(Position position)
    {
        var phased = EvaluatePhased(position);
        int score = Interpolate(phased.MiddlegameScore, phased.EndgameScore, phased.Phase);

        // Mobility
        var moves = MoveGenerator.GeneratePseudoLegalMoves(position);
        int mobilityScore = Interpolate(
            moves.Count * MobilityWeightMg,
            moves.Count * MobilityWeightEg,
            phased.Phase);
        score += position.SideToMove == Color.White ? mobilityScore : -mobilityScore;

        // Pawn structure evaluation
        int pawnStructure = EvaluatePawnStructure(position);
        score += position.SideToMove == Color.White ? pawnStructure : -pawnStructure;

        // Negamax relative
        return position.SideToMove == Color.White ? score : -score;
    }

    public static int MaterialOnlyScore(Position position)
    {
        var phased = EvaluatePhased(position);
        return Interpolate(phased.MiddlegameScore, phased.EndgameScore, phased.Phase);
    }

    private static (int MiddlegameScore, int EndgameScore, int Phase) EvaluatePhased(Position position)
    {
        int middlegame = 0;
        int endgame = 0;
        int phase = 0;

        for (int square = 0; square < position.Squares.Length; square++)
        {
            var piece = position.Squares[square];
            if (piece == Piece.None)
                continue;

            middlegame += GetPieceValueMg(piece) + GetPieceSquareValueMg(piece, square);
            endgame += GetPieceValueEg(piece) + GetPieceSquareValueEg(piece, square);
            phase += GetPhaseValue(piece);
        }

        return (middlegame, endgame, Math.Clamp(phase, 0, MaxPhase));
    }

    private static int Interpolate(int middlegame, int endgame, int phase)
        => (middlegame * phase + endgame * (MaxPhase - phase)) / MaxPhase;

    public static int GetRepetitionScore(Position position, int ply)
    {
        int materialScore = MaterialOnlyScore(position);
        int materialForSideToMove = position.SideToMove == Color.White ? materialScore : -materialScore;

        int materialContempt = Math.Clamp(
            materialForSideToMove / MaterialContemptDivisor,
            -MaxMaterialContempt,
            MaxMaterialContempt);

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

    private static int GetPieceValueMg(Piece piece) => piece switch
    {
        Piece.WQ => QueenValMg, Piece.BQ => -QueenValMg,
        Piece.WR => RookValMg,  Piece.BR => -RookValMg,
        Piece.WB => BishopValMg, Piece.BB => -BishopValMg,
        Piece.WN => KnightValMg, Piece.BN => -KnightValMg,
        Piece.WP => PawnValMg,   Piece.BP => -PawnValMg,
        _ => 0
    };

    private static int GetPieceValueEg(Piece piece) => piece switch
    {
        Piece.WQ => QueenValEg, Piece.BQ => -QueenValEg,
        Piece.WR => RookValEg,  Piece.BR => -RookValEg,
        Piece.WB => BishopValEg, Piece.BB => -BishopValEg,
        Piece.WN => KnightValEg, Piece.BN => -KnightValEg,
        Piece.WP => PawnValEg,   Piece.BP => -PawnValEg,
        _ => 0
    };

    private static int GetPieceSquareValueMg(Piece piece, int square) => piece switch
    {
        Piece.WP => PST.Pawn[square],
        Piece.BP => -PST.Pawn[PST.Mirror(square)],
        Piece.WN => PST.Knight[square],
        Piece.BN => -PST.Knight[PST.Mirror(square)],
        Piece.WB => PST.Bishop[square],
        Piece.BB => -PST.Bishop[PST.Mirror(square)],
        Piece.WR => PST.Rook[square],
        Piece.BR => -PST.Rook[PST.Mirror(square)],
        Piece.WQ => PST.Queen[square],
        Piece.BQ => -PST.Queen[PST.Mirror(square)],
        Piece.WK => PST.King[square],
        Piece.BK => -PST.King[PST.Mirror(square)],
        _ => 0
    };

    private static int GetPieceSquareValueEg(Piece piece, int square) => piece switch
    {
        Piece.WP => PST.Pawn[square],
        Piece.BP => -PST.Pawn[PST.Mirror(square)],
        Piece.WN => PST.Knight[square],
        Piece.BN => -PST.Knight[PST.Mirror(square)],
        Piece.WB => PST.Bishop[square],
        Piece.BB => -PST.Bishop[PST.Mirror(square)],
        Piece.WR => PST.Rook[square],
        Piece.BR => -PST.Rook[PST.Mirror(square)],
        Piece.WQ => PST.Queen[square],
        Piece.BQ => -PST.Queen[PST.Mirror(square)],
        Piece.WK => PST.KingEndgame[square],
        Piece.BK => -PST.KingEndgame[PST.Mirror(square)],
        _ => 0
    };

    private static int GetPhaseValue(Piece piece) => piece switch
    {
        Piece.WN or Piece.BN => PhaseKnight,
        Piece.WB or Piece.BB => PhaseBishop,
        Piece.WR or Piece.BR => PhaseRook,
        Piece.WQ or Piece.BQ => PhaseQueen,
        _ => 0
    };
    
    private static int EvaluatePawnStructure(Position position)
    {
        Span<int> whiteFiles = stackalloc int[8];
        Span<int> blackFiles = stackalloc int[8];

        // Count pawns per file
        for (int square = 0; square < 64; square++)
        {
            var piece = position.Squares[square];
            int file = square % 8;
            if (piece == Piece.WP) whiteFiles[file]++;
            else if (piece == Piece.BP) blackFiles[file]++;
        }

        int score = 0;
        
        for (int f = 0; f < 8; f++)
        {
            // Doubled pawns
            if (whiteFiles[f] > 1) score -= 10 * (whiteFiles[f] - 1);
            if (blackFiles[f] > 1) score += 10 * (blackFiles[f] - 1);

            // Isolated pawns
            bool whiteIsolated = whiteFiles[f] > 0 && (f == 0 ? whiteFiles[1] == 0 : f == 7 ? whiteFiles[6] == 0 : (whiteFiles[f - 1] == 0 && whiteFiles[f + 1] == 0));
            bool blackIsolated = blackFiles[f] > 0 && (f == 0 ? blackFiles[1] == 0 : f == 7 ? blackFiles[6] == 0 : (blackFiles[f - 1] == 0 && blackFiles[f + 1] == 0));

            if (whiteIsolated) score -= 15;
            if (blackIsolated) score += 15;
        }

        return score;
    }
}