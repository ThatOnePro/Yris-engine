using Engine.Core.Board;
using Engine.Core.MoveGen;
using Engine.Core.Moves;

namespace Engine.Core.Search;

public static class Negamax
{
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