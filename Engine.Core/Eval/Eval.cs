using Engine.Core.Board;

public static class Eval
{
    // Static values used by move ordering / MVV-LVA.
    public const int QueenVal  = 900;
    public const int RookVal   = 500;
    public const int BishopVal = 320;
    public const int KnightVal = 300;
    public const int PawnVal   = 100;

    // Middlegame / endgame material values (tapered evaluation).
    private const int QueenValMg  = 900;  private const int QueenValEg  = 900;
    private const int RookValMg   = 500;  private const int RookValEg   = 520;
    private const int BishopValMg = 330;  private const int BishopValEg = 350;
    private const int KnightValMg = 320;  private const int KnightValEg = 300;
    private const int PawnValMg   = 100;  private const int PawnValEg   = 120;

    // Structural bonuses
    private const int BishopPairBonusMg       = 30;
    private const int BishopPairBonusEg       = 50;
    private const int RookOpenFileBonusMg     = 25;
    private const int RookSemiOpenFileBonusMg = 12;
    private const int RookSeventhRankBonus    = 20;

    // Pawn structure
    private const int DoubledPawnPenalty  = 10;
    private const int IsolatedPawnPenalty = 15;

    // Passed pawn bonuses indexed by rank (rank 0 = home, rank 7 = promotion).
    // White pawn on rank r has index r; black pawn on rank r has index (7 - r).
    private static readonly int[] PassedPawnBonusMg = { 0,  5, 10, 20, 35, 55,  80, 0 };
    private static readonly int[] PassedPawnBonusEg = { 0, 10, 20, 40, 65, 95, 135, 0 };

    // King safety
    private const int PawnShieldMissingPenalty     = 12; // per missing shield pawn
    private const int OpenFileNearKingPenalty      = 25;
    private const int SemiOpenFileNearKingPenalty  = 12;

    // Game phase weights (0 = full endgame, MaxPhase = full opening/middlegame).
    private const int PhaseKnight = 1;
    private const int PhaseBishop = 1;
    private const int PhaseRook   = 2;
    private const int PhaseQueen  = 4;
    private const int MaxPhase    = 24;

    // Draw / repetition scoring
    private const int DrawContempt                = 12;
    private const int MaterialContemptDivisor     = 10;
    private const int MaxMaterialContempt         = 50;
    private const int RootRepetitionPenaltyWindow = 6;
    private const int RootRepetitionPenaltyStep   = 2;

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Full positional evaluation, returned from the side-to-move's perspective
    /// (positive = good for the side to move).
    /// </summary>
    public static int MaterialScore(Position position)
    {
        // Count pawns per file once; reused by structure, passed-pawn, and king-safety routines.
        Span<int> wFiles = stackalloc int[8];
        Span<int> bFiles = stackalloc int[8];
        CountPawnFiles(position, wFiles, bFiles);

        var (mgScore, egScore, phase) = EvaluatePhased(position);

        // --- Pawn structure (phase-independent penalty, keeps things fast) ---
        int pawnStructure = EvaluatePawnStructure(wFiles, bFiles);
        mgScore += pawnStructure;
        egScore += pawnStructure;

        // --- Passed pawns ---
        EvaluatePassedPawns(position, wFiles, bFiles, out int passedMg, out int passedEg);
        mgScore += passedMg;
        egScore += passedEg;

        // --- Bishop pair ---
        EvaluateBishopPair(position, out int bpMg, out int bpEg);
        mgScore += bpMg;
        egScore += bpEg;

        // --- Rooks on open / semi-open files and 7th rank ---
        mgScore += EvaluateRooks(position, wFiles, bFiles);

        // --- King safety (middlegame only; scales with phase) ---
        mgScore += EvaluateKingSafety(position, wFiles, bFiles, phase);

        int score = Interpolate(mgScore, egScore, phase);

        return position.SideToMove == Color.White ? score : -score;
    }

    /// <summary>Material-only score (white positive), used for contempt / draw detection.</summary>
    public static int MaterialOnlyScore(Position position)
    {
        var (mg, eg, phase) = EvaluatePhased(position);
        return Interpolate(mg, eg, phase);
    }

    public static int GetRepetitionScore(Position position, int ply)
    {
        int materialScore      = MaterialOnlyScore(position);
        int materialForStm     = position.SideToMove == Color.White ? materialScore : -materialScore;
        int materialContempt   = Math.Clamp(materialForStm / MaterialContemptDivisor,
                                            -MaxMaterialContempt, MaxMaterialContempt);
        int drawScore          = -DrawContempt - materialContempt;
        int nearRootPenalty    = Math.Max(0, RootRepetitionPenaltyWindow - ply) * RootRepetitionPenaltyStep;
        return drawScore - nearRootPenalty;
    }

    // MVV-LVA helper used by move ordering.
    public static int GetPieceValue(Piece piece) => piece switch
    {
        Piece.WQ => QueenVal,  Piece.BQ => -QueenVal,
        Piece.WR => RookVal,   Piece.BR => -RookVal,
        Piece.WB => BishopVal, Piece.BB => -BishopVal,
        Piece.WN => KnightVal, Piece.BN => -KnightVal,
        Piece.WP => PawnVal,   Piece.BP => -PawnVal,
        _ => 0
    };

    // -------------------------------------------------------------------------
    // Core tapered evaluation
    // -------------------------------------------------------------------------

    private static (int MiddlegameScore, int EndgameScore, int Phase) EvaluatePhased(Position position)
    {
        int mg = 0, eg = 0, phase = 0;

        for (int sq = 0; sq < 64; sq++)
        {
            var piece = position.Squares[sq];
            if (piece == Piece.None) continue;

            mg    += GetPieceValueMg(piece) + GetPieceSquareValueMg(piece, sq);
            eg    += GetPieceValueEg(piece) + GetPieceSquareValueEg(piece, sq);
            phase += GetPhaseValue(piece);
        }

        return (mg, eg, Math.Clamp(phase, 0, MaxPhase));
    }

    private static int Interpolate(int mg, int eg, int phase)
        => (mg * phase + eg * (MaxPhase - phase)) / MaxPhase;

    // -------------------------------------------------------------------------
    // Structural evaluations
    // -------------------------------------------------------------------------

    private static void CountPawnFiles(Position pos, Span<int> wFiles, Span<int> bFiles)
    {
        for (int sq = 0; sq < 64; sq++)
        {
            var p = pos.Squares[sq];
            if      (p == Piece.WP) wFiles[sq % 8]++;
            else if (p == Piece.BP) bFiles[sq % 8]++;
        }
    }

    private static int EvaluatePawnStructure(Span<int> wFiles, Span<int> bFiles)
    {
        int score = 0;
        for (int f = 0; f < 8; f++)
        {
            // Doubled pawns
            if (wFiles[f] > 1) score -= DoubledPawnPenalty * (wFiles[f] - 1);
            if (bFiles[f] > 1) score += DoubledPawnPenalty * (bFiles[f] - 1);

            // Isolated pawns
            bool leftEmpty  = f == 0;
            bool rightEmpty = f == 7;

            bool wIsolated = wFiles[f] > 0
                && (leftEmpty  || wFiles[f - 1] == 0)
                && (rightEmpty || wFiles[f + 1] == 0);

            bool bIsolated = bFiles[f] > 0
                && (leftEmpty  || bFiles[f - 1] == 0)
                && (rightEmpty || bFiles[f + 1] == 0);

            if (wIsolated) score -= IsolatedPawnPenalty;
            if (bIsolated) score += IsolatedPawnPenalty;
        }
        return score;
    }

    private static void EvaluatePassedPawns(
        Position pos,
        Span<int> wFiles, Span<int> bFiles,
        out int mgBonus, out int egBonus)
    {
        mgBonus = 0;
        egBonus = 0;

        for (int sq = 0; sq < 64; sq++)
        {
            var piece = pos.Squares[sq];
            if (piece != Piece.WP && piece != Piece.BP) continue;

            bool white = piece == Piece.WP;
            int  file  = sq % 8;
            int  rank  = sq / 8; // 0 = rank 1 (white home rank)

            if (IsPassedPawn(pos, sq, white, wFiles, bFiles))
            {
                // For white: rank 6 is closest to promotion (index 6 in the bonus tables).
                // For black: rank 1 maps to the same index (7 - rank).
                int rankIdx = white ? rank : (7 - rank);
                if (white)
                {
                    mgBonus += PassedPawnBonusMg[rankIdx];
                    egBonus += PassedPawnBonusEg[rankIdx];
                }
                else
                {
                    mgBonus -= PassedPawnBonusMg[rankIdx];
                    egBonus -= PassedPawnBonusEg[rankIdx];
                }
            }
        }
    }

    /// <summary>
    /// A pawn is passed when no enemy pawn stands on its file or adjacent files
    /// ahead of it.
    /// </summary>
    private static bool IsPassedPawn(
        Position pos, int sq, bool white,
        Span<int> wFiles, Span<int> bFiles)
    {
        int file = sq % 8;
        int rank = sq / 8;

        int fMin = Math.Max(0, file - 1);
        int fMax = Math.Min(7, file + 1);

        Piece enemy = white ? Piece.BP : Piece.WP;

        if (white)
        {
            for (int r = rank + 1; r < 8; r++)
                for (int f = fMin; f <= fMax; f++)
                    if (pos.Squares[r * 8 + f] == enemy) return false;
        }
        else
        {
            for (int r = rank - 1; r >= 0; r--)
                for (int f = fMin; f <= fMax; f++)
                    if (pos.Squares[r * 8 + f] == enemy) return false;
        }
        return true;
    }

    private static void EvaluateBishopPair(Position pos, out int mgBonus, out int egBonus)
    {
        mgBonus = 0;
        egBonus = 0;

        int wBishops = 0, bBishops = 0;
        for (int sq = 0; sq < 64; sq++)
        {
            if (pos.Squares[sq] == Piece.WB) wBishops++;
            else if (pos.Squares[sq] == Piece.BB) bBishops++;
        }

        if (wBishops >= 2) { mgBonus += BishopPairBonusMg; egBonus += BishopPairBonusEg; }
        if (bBishops >= 2) { mgBonus -= BishopPairBonusMg; egBonus -= BishopPairBonusEg; }
    }

    private static int EvaluateRooks(Position pos, Span<int> wFiles, Span<int> bFiles)
    {
        int score = 0;

        for (int sq = 0; sq < 64; sq++)
        {
            var piece = pos.Squares[sq];
            if (piece != Piece.WR && piece != Piece.BR) continue;

            bool white = piece == Piece.WR;
            int  file  = sq % 8;
            int  rank  = sq / 8;

            bool friendlyPawn = white ? wFiles[file] > 0 : bFiles[file] > 0;
            bool enemyPawn    = white ? bFiles[file] > 0 : wFiles[file] > 0;

            int rookBonus = 0;
            if (!friendlyPawn && !enemyPawn)
                rookBonus += RookOpenFileBonusMg;      // open file
            else if (!friendlyPawn)
                rookBonus += RookSemiOpenFileBonusMg;  // semi-open file

            // Rook on 7th rank (white rank 7 = index 6, black rank 2 = index 1)
            if (( white && rank == 6) || (!white && rank == 1))
                rookBonus += RookSeventhRankBonus;

            score += white ? rookBonus : -rookBonus;
        }
        return score;
    }

    /// <summary>
    /// Simple king safety: pawn shield + open/semi-open files near king.
    /// The result is scaled by the game phase so it fades in the endgame.
    /// </summary>
    private static int EvaluateKingSafety(
        Position pos,
        Span<int> wFiles, Span<int> bFiles,
        int phase)
    {
        // King safety only matters when there are attacking pieces on the board.
        if (phase < 8) return 0;

        int score = 0;

        for (int sq = 0; sq < 64; sq++)
        {
            var piece = pos.Squares[sq];
            if (piece != Piece.WK && piece != Piece.BK) continue;

            bool white  = piece == Piece.WK;
            int  kFile  = sq % 8;
            int  kRank  = sq / 8;
            int  penalty = 0;

            // Pawn shield: three squares directly in front of the king.
            int shieldRank = white ? kRank + 1 : kRank - 1;
            if (shieldRank is >= 0 and < 8)
            {
                for (int f = Math.Max(0, kFile - 1); f <= Math.Min(7, kFile + 1); f++)
                {
                    Piece shieldPawn = white ? Piece.WP : Piece.BP;
                    if (pos.Squares[shieldRank * 8 + f] != shieldPawn)
                        penalty += PawnShieldMissingPenalty;
                }
            }

            // Open / semi-open files adjacent to and including the king's file.
            for (int f = Math.Max(0, kFile - 1); f <= Math.Min(7, kFile + 1); f++)
            {
                bool friendly = white ? wFiles[f] > 0 : bFiles[f] > 0;
                bool enemy    = white ? bFiles[f] > 0 : wFiles[f] > 0;

                if (!friendly && !enemy) penalty += OpenFileNearKingPenalty;
                else if (!friendly)      penalty += SemiOpenFileNearKingPenalty;
            }

            // Scale penalty with phase (king safety is less critical in the endgame).
            int scaledPenalty = penalty * phase / MaxPhase;
            score += white ? -scaledPenalty : scaledPenalty;
        }

        return score;
    }

    // -------------------------------------------------------------------------
    // Piece-square tables & helpers
    // -------------------------------------------------------------------------

    private static int GetPieceValueMg(Piece piece) => piece switch
    {
        Piece.WQ => QueenValMg,  Piece.BQ => -QueenValMg,
        Piece.WR => RookValMg,   Piece.BR => -RookValMg,
        Piece.WB => BishopValMg, Piece.BB => -BishopValMg,
        Piece.WN => KnightValMg, Piece.BN => -KnightValMg,
        Piece.WP => PawnValMg,   Piece.BP => -PawnValMg,
        _ => 0
    };

    private static int GetPieceValueEg(Piece piece) => piece switch
    {
        Piece.WQ => QueenValEg,  Piece.BQ => -QueenValEg,
        Piece.WR => RookValEg,   Piece.BR => -RookValEg,
        Piece.WB => BishopValEg, Piece.BB => -BishopValEg,
        Piece.WN => KnightValEg, Piece.BN => -KnightValEg,
        Piece.WP => PawnValEg,   Piece.BP => -PawnValEg,
        _ => 0
    };

    private static int GetPieceSquareValueMg(Piece piece, int sq) => piece switch
    {
        Piece.WP => PST.Pawn[sq],
        Piece.BP => -PST.Pawn[PST.Mirror(sq)],
        Piece.WN => PST.Knight[sq],
        Piece.BN => -PST.Knight[PST.Mirror(sq)],
        Piece.WB => PST.Bishop[sq],
        Piece.BB => -PST.Bishop[PST.Mirror(sq)],
        Piece.WR => PST.Rook[sq],
        Piece.BR => -PST.Rook[PST.Mirror(sq)],
        Piece.WQ => PST.Queen[sq],
        Piece.BQ => -PST.Queen[PST.Mirror(sq)],
        Piece.WK => PST.King[sq],
        Piece.BK => -PST.King[PST.Mirror(sq)],
        _ => 0
    };

    private static int GetPieceSquareValueEg(Piece piece, int sq) => piece switch
    {
        Piece.WP => PST.PawnEndgame[sq],
        Piece.BP => -PST.PawnEndgame[PST.Mirror(sq)],
        Piece.WN => PST.Knight[sq],
        Piece.BN => -PST.Knight[PST.Mirror(sq)],
        Piece.WB => PST.Bishop[sq],
        Piece.BB => -PST.Bishop[PST.Mirror(sq)],
        Piece.WR => PST.Rook[sq],
        Piece.BR => -PST.Rook[PST.Mirror(sq)],
        Piece.WQ => PST.Queen[sq],
        Piece.BQ => -PST.Queen[PST.Mirror(sq)],
        Piece.WK => PST.KingEndgame[sq],
        Piece.BK => -PST.KingEndgame[PST.Mirror(sq)],
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
}