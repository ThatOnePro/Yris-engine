using System.Collections.Concurrent;
using Engine.Core.Board;
using Engine.Core.MoveGen;
using Engine.Core.Moves;

namespace Engine.Core.Search;

public static class Negamax
{
    private const int MateScore = 100000;
    private const int MateThreshold = MateScore - 1000;

    public static List<ScoredMove> OrderMoves(Position pos, List<Move> moves, Move ttMove = default)
    {
        List<ScoredMove> orderedMoves = new(moves.Count);

        foreach (var move in moves)
        {
            var movingPiece = pos.Squares[move.From];
            var capturedPiece = pos.Squares[move.To];

            int score = 0;

            // TT move gets highest priority
            if (move.Equals(ttMove))
            {
                score += 20000;
            }

            // MVV-LVA
            if (capturedPiece != 0)
            {
                score += 10000 +
                         100 * Math.Abs(Eval.GetPieceValue(capturedPiece)) -
                         Math.Abs(Eval.GetPieceValue(movingPiece));
            }

            // Promotion bonus
            if (move.Promotion != 0)
            {
                score += 5000;
            }

            orderedMoves.Add(new ScoredMove(move, score));
        }

        orderedMoves.Sort((a, b) => b.Score.CompareTo(a.Score));

        return orderedMoves;
    }
    
    public static Move GetBestMoveIterative(Position pos, int maxDepth)
    {
        Move bestMove = default;
    
        for (int currentDepth = 1; currentDepth <= maxDepth; currentDepth++)
        {
            // De parallelle loop profiteert van de resultaten van de vorige diepte in de TT
            bestMove = GetBestMoveParallel(pos, currentDepth);
        
            Console.WriteLine($"Depth {currentDepth} completed. Best move: {bestMove}");
        }
        return bestMove;
    }

    public static Move GetBestMoveParallel(Position pos, int depth)
    {
        var moves = MoveGenerator.GenerateLegalMoves(pos);
        var orderedMoves = OrderMoves(pos, moves);
    
        var options = new ParallelOptions { MaxDegreeOfParallelism = 4 };
        
        // Gebruik een thread-safe container voor de resultaten
        var results = new ConcurrentBag<(Move move, double score)>();
    
        using (var threadLocalPos = new ThreadLocal<Position>(() => pos.Clone()))
        {
            Parallel.ForEach(orderedMoves, options, move =>
            {
                var p = threadLocalPos.Value; // Hergebruik het bord van deze thread
            
                p.MakeMove(move.Move);
                // Gebruik de gedeelde TT binnen NegamaxScore
                double score = -NegamaxScore(p, depth - 1, Convert.ToInt32(double.NegativeInfinity), Convert.ToInt32(double.PositiveInfinity), 1);
                p.UndoMove();

                results.Add((move.Move, score));
            });
        }

        return results.OrderByDescending(r => r.score).First().move;
    }
    
    
    public static Move GetBestMove(Position pos, int depth)
    {
        int alpha = int.MinValue + 1;
        int beta = int.MaxValue;
        int bestValue = int.MinValue + 1;

        var moves = MoveGenerator.GenerateLegalMoves(pos);
        if (moves.Count == 0)
        {
            return default;
        }

        Move bestMove = moves[0];
        var orderedMoves = OrderMoves(pos, moves);

        foreach (var scoredMove in orderedMoves)
        {
            if (TimeManager.TimeExpired)
            {
                break;
            }

            pos.MakeMove(scoredMove.Move);

            int boardValue = -NegamaxScore(pos, depth - 1, -beta, -alpha, 1);

            pos.UndoMove();

            if (boardValue > bestValue)
            {
                bestValue = boardValue;
                bestMove = scoredMove.Move;
            }

            alpha = Math.Max(alpha, bestValue);
        }

        return bestMove;
    }

    private static int NegamaxScore(Position pos, int depth, int alpha, int beta, int ply)
    {
        int originalAlpha = alpha;

        // Periodic time check
        TimeManager.RecordNodes(1);
        if (TimeManager.TimeExpired && ply > 0)
        {
            return Eval.MaterialScore(pos);
        }

        if (pos.HalfmoveClock >= 100 || (ply > 0 && pos.IsThreefoldRepetition()))
        {
            return Eval.GetRepetitionScore(pos, ply);
        }

        ulong hash = pos.ZobristKey;

        // Transposition Table Probe
        if (TranspositionTable.Probe(hash, depth, alpha, beta, out int ttScore, out Move ttMove))
        {
            return ScoreFromTT(ttScore, ply);
        }

        if (depth == 0)
        {
            return Quiescence(pos, alpha, beta, ply);
        }

        var moves = MoveGenerator.GenerateLegalMoves(pos);

        // Checkmate / stalemate
        if (moves.Count == 0)
        {
            if (AttackDetector.IsKingInCheck(pos, pos.SideToMove))
            {
                return -MateScore + ply;
            }

            return 0;
        }

        Move bestMove = default;

        var orderedMoves = OrderMoves(pos, moves, ttMove);

        int maxEval = int.MinValue + 1;

        foreach (var orderedMove in orderedMoves)
        {
            pos.MakeMove(orderedMove.Move);

            int eval = -NegamaxScore(pos, depth - 1, -beta, -alpha, ply + 1);

            pos.UndoMove();

            if (eval > maxEval)
            {
                maxEval = eval;
                bestMove = orderedMove.Move;
            }

            alpha = Math.Max(alpha, eval);

            if (alpha >= beta)
            {
                break; // beta cutoff
            }
        }

        // Determine TT flag
        TTFlag flag;

        if (maxEval <= originalAlpha)
        {
            flag = TTFlag.UpperBound;
        }
        else if (maxEval >= beta)
        {
            flag = TTFlag.LowerBound;
        }
        else
        {
            flag = TTFlag.Exact;
        }

        // Store in TT
        TranspositionTable.Store(hash, depth, ScoreToTT(maxEval, ply), flag, bestMove);

        return maxEval;
    }

    private static int Quiescence(Position pos, int alpha, int beta, int ply)
    {
        if (pos.HalfmoveClock >= 100 || pos.IsThreefoldRepetition())
        {
            return Eval.GetRepetitionScore(pos, ply);
        }

        int standPat = Eval.MaterialScore(pos);

        bool inCheck = AttackDetector.IsKingInCheck(pos, pos.SideToMove);

        if (!inCheck)
        {
            if (standPat >= beta)
            {
                return beta;
            }

            if (alpha < standPat)
            {
                alpha = standPat;
            }
        }

        var moves = OrderMoves(pos, MoveGenerator.GenerateLegalMoves(pos));

        foreach (var scoredMove in moves)
        {
            if (!inCheck &&
                pos.Squares[scoredMove.Move.To] == 0 &&
                scoredMove.Move.Promotion == 0)
                continue;

            pos.MakeMove(scoredMove.Move);

            int score = -Quiescence(pos, -beta, -alpha, ply + 1);

            pos.UndoMove();

            if (score >= beta)
            {
                return beta;
            }

            if (score > alpha)
            {
                alpha = score;
            }
        }

        return alpha;
    }

    private static int ScoreToTT(int score, int ply)
    {
        if (score >= MateThreshold)
            return score + ply;

        if (score <= -MateThreshold)
            return score - ply;

        return score;
    }

    private static int ScoreFromTT(int score, int ply)
    {
        if (score >= MateThreshold)
            return score - ply;

        if (score <= -MateThreshold)
            return score + ply;

        return score;
    }
}