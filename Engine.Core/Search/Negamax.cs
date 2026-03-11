using System.Collections.Concurrent;
using Engine.Core.Board;
using Engine.Core.MoveGen;
using Engine.Core.Moves;

namespace Engine.Core.Search;

public static class Negamax
{
    
    private static TranspositionTable _tt = new TranspositionTable(256);
    
    public static List<ScoredMove> OrderMoves(Position pos, List<Move> moves)
    {
        List<ScoredMove> orderedMoves = new();
        foreach (var move in moves)
        {
            var movingPiece = pos.Squares[move.From];
            var capturedPiece = pos.Squares[move.To];

            int score = 0;

            // 1. MVV-LVA (Most Valuable Victim - Least Valuable Attacker)
            if (capturedPiece != 0)
            {
                score = 100 * Math.Abs(Eval.GetPieceValue(capturedPiece)) - Math.Abs(Eval.GetPieceValue(movingPiece));
                score += 10000; // Zorg dat slaan altijd boven stille zetten staat
            }

            // 2. Promoties zijn ook topprioriteit
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
        double bestValue = double.NegativeInfinity;
        double alpha = double.NegativeInfinity;
        double beta = double.PositiveInfinity;

        var moves = MoveGenerator.GenerateLegalMoves(pos);

        foreach (var move in moves)
        {
            pos.MakeMove(move);
            double boardValue = -NegamaxScore(pos, depth - 1, -beta, -alpha);
            pos.UndoMove();

            if (boardValue > bestValue)
            {
                bestValue = boardValue;
                bestMove = move;
            }

            alpha = Math.Max(alpha, boardValue);
        }

        return bestMove;
    }
    
    // public Move GetBestMoveParallel(Position pos, int depth)
    // {
    //     var moves = MoveGenerator.GenerateLegalMoves(pos);
    //     var orderedMoves = OrderMoves(pos, moves);
    //
    //     // Gebruik een thread-safe container voor de resultaten
    //     var results = new ConcurrentBag<(Move move, double score)>();
    //
    //     Parallel.ForEach(orderedMoves, moveWrap =>
    //     {
    //         // ELKE THREAD HEEFT EEN EIGEN KOPIE VAN HET BORD NODIG
    //         Position threadPos = pos.Clone(); 
    //     
    //         threadPos.MakeMove(moveWrap.Move);
    //         // We starten Negamax op de kopie
    //         double score = -NegamaxScore(threadPos, depth - 1, double.NegativeInfinity, double.PositiveInfinity);
    //         threadPos.UndoMove();
    //
    //         results.Add((moveWrap.Move, score));
    //     });
    //
    //     // Zoek de beste score uit de verzamelde resultaten
    //     return results.OrderByDescending(r => r.score).First().move;
    // }

    
    private static double NegamaxScore(Position pos, int depth, double alpha, double beta)
    {
        TTEntry? entry = _tt.Lookup(pos.ZobristKey);
        if (entry.HasValue && entry.Value.Depth >= depth)
        {
            if (entry.Value.Flag == HashFlag.Exact) return entry.Value.Value;
            if (entry.Value.Flag == HashFlag.LowerBound) alpha = Math.Max(alpha, entry.Value.Value);
            if (entry.Value.Flag == HashFlag.UpperBound) beta = Math.Min(beta, entry.Value.Value);
        
            if (alpha >= beta) return entry.Value.Value;
        }
        
        if (depth == 0) return Eval.MaterialScore(pos);

        var moves = MoveGenerator.GenerateLegalMoves(pos);
        var orderedMoves = OrderMoves(pos, moves);

        double maxEval = double.NegativeInfinity;

        foreach (var move in moves)
        {
            pos.MakeMove(move);
            double eval = -NegamaxScore(pos, depth - 1, -beta, -alpha);
            pos.UndoMove();

            maxEval = Math.Max(maxEval, eval);
            alpha = Math.Max(alpha, eval);

            if (alpha >= beta)
            {
                break;
            }
        }

        return maxEval;
    }
}