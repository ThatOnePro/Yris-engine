using System.Collections.Concurrent;
using Engine.Core.Board;
using Engine.Core.MoveGen;
using Engine.Core.Moves;

namespace Engine.Core.Search;

public static class Negamax
{
    
    private static TranspositionTable _tt = new TranspositionTable(256);
    
    private const int MateScore = 100000;

    public static List<ScoredMove> OrderMoves(Position pos, List<Move> moves)
    {
        List<ScoredMove> orderedMoves = new(moves.Count);

        foreach (var move in moves)
        {
            var movingPiece = pos.Squares[move.From];
            var capturedPiece = pos.Squares[move.To];

            int score = 0;

            // MVV-LVA
            if (capturedPiece != 0)
            {
                score = 10000 +
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

    public static Move GetBestMove(Position pos, int depth)
    {
        Move bestMove = new Move();

        int bestValue = int.MinValue + 1;
        int alpha = int.MinValue + 1;
        int beta = int.MaxValue;

        var moves = OrderMoves(pos, MoveGenerator.GenerateLegalMoves(pos));

        foreach (var scoredMove in moves)
        {
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

    
    public static Move GetBestMoveParallel(Position pos, int depth)
    {
        var moves = MoveGenerator.GenerateLegalMoves(pos);
        var orderedMoves = OrderMoves(pos, moves);
    
        // Gebruik een thread-safe container voor de resultaten
        var results = new ConcurrentBag<(Move move, double score)>();
    
        Parallel.ForEach(orderedMoves, moveWrap =>
        {
            // ELKE THREAD HEEFT EEN EIGEN KOPIE VAN HET BORD NODIG
            Position threadPos = pos.Clone(); 
        
            threadPos.MakeMove(moveWrap.Move);
            // We starten Negamax op de kopie
            double score = -NegamaxScore(threadPos, depth - 1, Convert.ToInt32(double.NegativeInfinity), Convert.ToInt32(double.PositiveInfinity), 1);
            threadPos.UndoMove();
    
            results.Add((moveWrap.Move, score));
        });
    
        // Zoek de beste score uit de verzamelde resultaten
        return results.OrderByDescending(r => r.score).First().move;
    }

    
    private static int NegamaxScore(Position pos, int depth, int alpha, int beta, int ply)
    {
        if (depth == 0)
        {
            return Quiescence(pos, alpha, beta);
        }
        TTEntry? entry = _tt.Lookup(pos.ZobristKey);
        if (entry.HasValue && entry.Value.Depth >= depth)
        {
            if (entry.Value.Flag == HashFlag.Exact) return entry.Value.Value;
            if (entry.Value.Flag == HashFlag.LowerBound) alpha = Math.Max(alpha, entry.Value.Value);
            if (entry.Value.Flag == HashFlag.UpperBound) beta = Math.Min(beta, entry.Value.Value);
        
            if (alpha >= beta) return entry.Value.Value;
        }
        
        var moves = MoveGenerator.GenerateLegalMoves(pos);

        // Checkmate / stalemate detection
        if (moves.Count == 0)
        {
            if (AttackDetector.IsKingInCheck(pos, pos.SideToMove))
            {
                return -MateScore + ply;
            }

            return 0; // stalemate
        }

        var orderedMoves = OrderMoves(pos, moves);

        int maxEval = int.MinValue + 1;

        foreach (var orderedMove in orderedMoves)
        {
            pos.MakeMove(orderedMove.Move);

            int eval = -NegamaxScore(pos, depth - 1, -beta, -alpha, ply + 1);

            pos.UndoMove();

            maxEval = Math.Max(maxEval, eval);
            alpha = Math.Max(alpha, eval);

            if (alpha >= beta)
            {
                break; // beta cutoff
            }
        }

        return maxEval;
    }


    private static int Quiescence(Position pos, int alpha, int beta)
    {
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
        // can be imrpoves by only generating captures and checks ma voor nu is dit ok
        var moves = OrderMoves(pos, MoveGenerator.GenerateLegalMoves(pos));

        foreach (var scoredMove in moves)
        {
            if (pos.Squares[scoredMove.Move.To] == 0 && scoredMove.Move.Promotion == 0)
                continue;

            pos.MakeMove(scoredMove.Move);

            int score = -Quiescence(pos, -beta, -alpha);

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
}