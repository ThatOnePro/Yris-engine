using System.Collections.Concurrent;
using Engine.Core.Board;
using Engine.Core.MoveGen;
using Engine.Core.Moves;

namespace Engine.Core.Search;

public record struct SearchResult(Move BestMove, int Score, long Nodes);

public static class Negamax
{
    private const int MateScore      = 100_000;
    private const int MateThreshold  = MateScore - 1_000;

    // Futility pruning margins per remaining depth (index = depth).
    private static readonly int[] FutilityMargin = { 0, 150, 300, 500 };
    private const int MaxFutilityDepth = 3;

    // Delta pruning margin in quiescence.
    private const int DeltaMargin = 200;

    // -------------------------------------------------------------------------
    // Killer moves: 2 slots per ply, up to MaxPly plies deep.
    // -------------------------------------------------------------------------
    private const int MaxPly = 128;
    private static readonly Move[,] Killers = new Move[MaxPly, 2];

    // -------------------------------------------------------------------------
    // History heuristic: indexed [from][to].
    // Updated when a quiet move causes a beta cut-off.
    // -------------------------------------------------------------------------
    private static readonly int[,] History = new int[64, 64];

    /// <summary>
    /// Call this once before starting a new search (i.e. before the iterative
    /// deepening loop). Clears killers and decays history so stale bonuses
    /// fade without being wiped completely.
    /// </summary>
    public static void PrepareSearch()
    {
        Array.Clear(Killers, 0, Killers.Length);

        for (int f = 0; f < 64; f++)
            for (int t = 0; t < 64; t++)
                History[f, t] /= 4; // decay – keeps useful ordering data
    }

    // -------------------------------------------------------------------------
    // Move ordering
    // -------------------------------------------------------------------------

    public static List<ScoredMove> OrderMoves(
        Position pos, List<Move> moves,
        Move ttMove = default, int ply = 0)
    {
        var ordered = new List<ScoredMove>(moves.Count);

        foreach (var move in moves)
        {
            var movingPiece   = pos.Squares[move.From];
            var capturedPiece = pos.Squares[move.To];
            int score         = 0;

            if (move.Equals(ttMove))
            {
                score = 20_000; // TT move first
            }
            else if (capturedPiece != Piece.None)
            {
                // MVV-LVA: Most Valuable Victim – Least Valuable Attacker
                score = 10_000
                    + 100 * Math.Abs(Eval.GetPieceValue(capturedPiece))
                    -       Math.Abs(Eval.GetPieceValue(movingPiece));
            }
            else if (move.Promotion != Piece.None)
            {
                score = 9_000;
            }
            else
            {
                // Quiet move: killers then history
                if (ply < MaxPly && move.Equals(Killers[ply, 0]))
                    score = 8_000;
                else if (ply < MaxPly && move.Equals(Killers[ply, 1]))
                    score = 7_000;
                else
                    score = Math.Clamp(History[move.From, move.To], 0, 6_999);
            }

            ordered.Add(new ScoredMove(move, score));
        }

        ordered.Sort(static (a, b) => b.Score.CompareTo(a.Score));
        return ordered;
    }

    // -------------------------------------------------------------------------
    // Iterative deepening entry points
    // -------------------------------------------------------------------------

    
    // -------------------------------------------------------------------------
    // Root search – called per depth inside the UCI iterative deepening loop.
    // Call PrepareSearch() once before the loop starts.
    // -------------------------------------------------------------------------

    public static SearchResult GetBestMove(Position pos, int depth)
    {
        int alpha     = int.MinValue + 1;
        int beta      = int.MaxValue;
        int bestValue = int.MinValue + 1;

        var moves = MoveGenerator.GenerateLegalMoves(pos);
        if (moves.Count == 0)
            return new SearchResult(default, 0, TimeManager.TotalNodes);

        Move bestMove    = moves[0];
        var orderedMoves = OrderMoves(pos, moves, default, 0);

        foreach (var scoredMove in orderedMoves)
        {
            if (TimeManager.TimeExpired) break;

            pos.MakeMove(scoredMove.Move);
            int boardValue = -NegamaxScore(pos, depth - 1, -beta, -alpha, 1, false);
            pos.UndoMove();

            if (boardValue > bestValue)
            {
                bestValue = boardValue;
                bestMove  = scoredMove.Move;
            }

            if (boardValue > alpha)
                alpha = boardValue;
        }

        return new SearchResult(bestMove, bestValue, TimeManager.TotalNodes);
    }

    // -------------------------------------------------------------------------
    // Core negamax with alpha-beta + all pruning techniques
    // -------------------------------------------------------------------------

    private static int NegamaxScore(
        Position pos, int depth,
        int alpha, int beta,
        int ply, bool isNullMove)
    {
        int originalAlpha = alpha;

        // --- Time / node budget ---
        TimeManager.RecordNodes(1);
        if (TimeManager.TimeExpired && ply > 0)
            return Eval.MaterialScore(pos);

        // --- Draw detection ---
        if (pos.HalfmoveClock >= 100 || (ply > 0 && pos.IsThreefoldRepetition()))
            return Eval.GetRepetitionScore(pos, ply);

        ulong hash = pos.ZobristKey;

        // --- Transposition table probe ---
        if (TranspositionTable.Probe(hash, depth, alpha, beta, out int ttScore, out Move ttMove))
            return ScoreFromTT(ttScore, ply);

        // --- Quiescence at horizon ---
        if (depth <= 0)
            return Quiescence(pos, alpha, beta, ply);

        // --- In-check detection (needed for several pruning conditions) ---
        bool inCheck = AttackDetector.IsKingInCheck(pos, pos.SideToMove);

        // --- Static evaluation (skip when in check to avoid bogus scores) ---
        int staticEval = inCheck ? (int.MinValue + 1) : Eval.MaterialScore(pos);

        // =====================================================================
        // NULL MOVE PRUNING
        // Skip if: in check, already did null move, endgame / zugzwang risk,
        // or static eval doesn't beat beta (no point trying).
        // =====================================================================
        if (!inCheck
            && !isNullMove
            && depth >= 3
            && staticEval >= beta
            && pos.HasNonPawnMaterial())
        {
            int R = depth >= 6 ? 3 : 2; // reduction

            pos.MakeNullMove();
            int nullScore = -NegamaxScore(pos, depth - 1 - R, -beta, -beta + 1, ply + 1, true);
            pos.UndoNullMove();

            if (nullScore >= beta)
                return beta; // null-move cut-off (don't return nullScore to avoid zugzwang)
        }

        // =====================================================================
        // FUTILITY PRUNING SETUP
        // Prune quiet moves near the horizon if the position is hopeless.
        // =====================================================================
        bool futilityPrune = !inCheck
            && depth <= MaxFutilityDepth
            && staticEval + FutilityMargin[depth] <= alpha;

        // --- Generate legal moves ---
        var moves = MoveGenerator.GenerateLegalMoves(pos);

        // Checkmate / stalemate
        if (moves.Count == 0)
            return inCheck ? -MateScore + ply : 0;

        Move bestMove    = default;
        var orderedMoves = OrderMoves(pos, moves, ttMove, ply);

        int maxEval   = int.MinValue + 1;
        int moveCount = 0;

        foreach (var orderedMove in orderedMoves)
        {
            var  move        = orderedMove.Move;
            bool isCapture   = pos.Squares[move.To] != Piece.None;
            bool isPromotion = move.Promotion != Piece.None;
            bool isQuiet     = !isCapture && !isPromotion;

            // ================================================================
            // FUTILITY PRUNING: skip quiet moves that can't improve alpha
            // ================================================================
            if (futilityPrune && isQuiet && moveCount > 0)
                continue;

            pos.MakeMove(move);
            moveCount++;

            int eval;

            // ================================================================
            // LATE MOVE REDUCTIONS (LMR)
            // Reduce depth for later quiet moves unlikely to be best.
            // ================================================================
            bool doLmr = moveCount >= 4
                && depth >= 3
                && isQuiet
                && !inCheck;

            if (moveCount == 1)
            {
                // First move: full window, full depth.
                eval = -NegamaxScore(pos, depth - 1, -beta, -alpha, ply + 1, false);
            }
            else if (doLmr)
            {
                int R = moveCount >= 8 ? 2 : 1;

                // Search with reduced depth and null window.
                eval = -NegamaxScore(pos, depth - 1 - R, -alpha - 1, -alpha, ply + 1, false);

                if (eval > alpha)
                {
                    // Failed high – re-search at full depth with null window.
                    eval = -NegamaxScore(pos, depth - 1, -alpha - 1, -alpha, ply + 1, false);

                    // If still above alpha, widen to full window (PVS).
                    if (eval > alpha && eval < beta)
                        eval = -NegamaxScore(pos, depth - 1, -beta, -alpha, ply + 1, false);
                }
            }
            else
            {
                // Principal Variation Search (PVS): null-window for non-PV moves.
                eval = -NegamaxScore(pos, depth - 1, -alpha - 1, -alpha, ply + 1, false);

                if (eval > alpha && eval < beta)
                    eval = -NegamaxScore(pos, depth - 1, -beta, -alpha, ply + 1, false);
            }

            pos.UndoMove();

            if (eval > maxEval)
            {
                maxEval  = eval;
                bestMove = move;
            }

            if (eval > alpha)
                alpha = eval;

            if (alpha >= beta)
            {
                // ============================================================
                // KILLER / HISTORY UPDATES on beta cut-off (quiet moves only)
                // ============================================================
                if (isQuiet && ply < MaxPly)
                {
                    // Update killers (shift old #0 to #1 if different).
                    if (!move.Equals(Killers[ply, 0]))
                    {
                        Killers[ply, 1] = Killers[ply, 0];
                        Killers[ply, 0] = move;
                    }

                    // Update history with depth² bonus, capped to avoid overflow.
                    History[move.From, move.To] += depth * depth;
                    if (History[move.From, move.To] > 1_000_000)
                        History[move.From, move.To] = 500_000;
                }
                break; // beta cut-off
            }
        }

        // --- Store in transposition table ---
        TTFlag flag = maxEval <= originalAlpha ? TTFlag.UpperBound
                    : maxEval >= beta          ? TTFlag.LowerBound
                                               : TTFlag.Exact;

        TranspositionTable.Store(hash, depth, ScoreToTT(maxEval, ply), flag, bestMove);

        return maxEval;
    }

    // -------------------------------------------------------------------------
    // Quiescence search
    // -------------------------------------------------------------------------

    private static int Quiescence(Position pos, int alpha, int beta, int ply)
    {
        if (pos.HalfmoveClock >= 100 || pos.IsThreefoldRepetition())
            return Eval.GetRepetitionScore(pos, ply);

        bool inCheck   = AttackDetector.IsKingInCheck(pos, pos.SideToMove);
        int  standPat  = Eval.MaterialScore(pos);

        if (!inCheck)
        {
            if (standPat >= beta) return beta;
            if (standPat > alpha) alpha = standPat;
        }

        var moves        = MoveGenerator.GenerateLegalMoves(pos);
        var orderedMoves = OrderMoves(pos, moves, default, ply);

        foreach (var scoredMove in orderedMoves)
        {
            var move = scoredMove.Move;

            bool isCapture   = pos.Squares[move.To] != Piece.None;
            bool isPromotion = move.Promotion != Piece.None;

            // Not in check: skip quiet moves.
            if (!inCheck && !isCapture && !isPromotion) continue;

            // =================================================================
            // DELTA PRUNING
            // Skip captures whose maximum possible gain still can't raise alpha.
            // =================================================================
            if (!inCheck && isCapture && !isPromotion)
            {
                int captureGain = Math.Abs(Eval.GetPieceValue(pos.Squares[move.To]));
                if (standPat + captureGain + DeltaMargin <= alpha)
                    continue;
            }

            pos.MakeMove(move);
            int score = -Quiescence(pos, -beta, -alpha, ply + 1);
            pos.UndoMove();

            if (score >= beta) return beta;
            if (score > alpha) alpha = score;
        }

        return alpha;
    }

    // -------------------------------------------------------------------------
    // Mate-score adjustment for transposition table storage
    // -------------------------------------------------------------------------

    private static int ScoreToTT(int score, int ply)
    {
        if (score >=  MateThreshold) return score + ply;
        if (score <= -MateThreshold) return score - ply;
        return score;
    }

    private static int ScoreFromTT(int score, int ply)
    {
        if (score >=  MateThreshold) return score - ply;
        if (score <= -MateThreshold) return score + ply;
        return score;
    }
}