using Engine.Core.Board;
using Engine.Core.MoveGen;
using Engine.Core.Moves;

namespace Engine.Core.Search;

public class Montecarlo
{
    private Position pos;
    private List<Move> moves;
    private Color color;

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

    public Color Color
    {
        get => color;
        set => color = value;
    }

    public Montecarlo(Position pos, Color color)
    {
        this.pos = pos;
        this.color = color;
    }


    public Move findBestMove()
    {
        double currentEval = Eval.MaterialScore(pos);
        Move bestMove = new Move();
        
        foreach (var m in moves)
        {
            pos.MakeMove(m);
            if (currentEval < Eval.MaterialScore(pos))
            {
                bestMove = m;
                currentEval = Eval.MaterialScore(pos);
            }
            pos.UndoMove();
        }
        return bestMove;
    }
}