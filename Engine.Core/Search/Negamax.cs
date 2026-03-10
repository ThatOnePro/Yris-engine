using Engine.Core.Board;
using Engine.Core.MoveGen;
using Engine.Core.Moves;

namespace Engine.Core.Search;

public class Negamax
{
    private Position pos;
    private List<Move> moves;

    public Position Pos
    {
        get => pos;
        set => pos = value;
    }

    public List<Move> Moves
    {
        get => moves;
        set => moves = MoveGenerator.GeneratePseudoLegalMoves(Pos);
    }

    public Negamax(Position pos)
    {
        this.pos = pos;
    }

    public Move GetBestMove(int depth) //houdt rekening met de tegenstander's countermoves en kiest de beste uitkomst
    {
        Move bestMove = new Move();
        double bestValue = double.NegativeInfinity;
        double alpha = double.NegativeInfinity;
        double beta = double.PositiveInfinity;


        foreach (var move in moves)
        {
            pos.MakeMove(move);
            double boardValue = -NegamaxScore(depth - 1, -beta, -alpha);

            pos.UndoMove();
            
            if (boardValue > bestValue)
            {
                bestValue = boardValue;
                bestMove = move;
            }
        
            // Update alpha (onze beste gegarandeerde score)
            alpha = Math.Max(alpha, boardValue);
        }

        return bestMove;
    }

    private double NegamaxScore(int depth, double alpha, double beta)
    {
        // 1. Basisgeval: diepte bereikt
        if (depth == 0) return Eval.MaterialScore(pos);

        var moves = MoveGenerator.GeneratePseudoLegalMoves(pos);

        double maxEval = double.NegativeInfinity;

        foreach (var move in moves)
        {
            pos.MakeMove(move);
            // Recursieve aanroep met omgedraaide alpha/beta en negatief resultaat
            double eval = -NegamaxScore(depth - 1, -beta, -alpha);
            pos.UndoMove();

            maxEval = Math.Max(maxEval, eval);
            alpha = Math.Max(alpha, eval);

            // 3. De Alpha-Beta Cut-off (Pruning)
            if (alpha >= beta)
            {
                break; // Deze tak is te goed voor ons, de tegenstander laat dit nooit toe
            }
        }
        return maxEval;
    }
    
}