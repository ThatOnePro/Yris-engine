using System.Collections.Concurrent;
using Engine.Core.Board;
using Engine.Core.MoveGen;
using Engine.Core.Moves;

namespace Engine.Core.Search;

public static class Negamax
{
    
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
    
    private static double NegamaxScore(Position pos, int depth, double alpha, double beta)
    {
        if (depth == 0) return Eval.MaterialScore(pos);

        var moves = MoveGenerator.GenerateLegalMoves(pos);

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