using Engine.Core.Board;
using Engine.Core.MoveGen;
using Engine.Core.Moves;

namespace Engine.Core.Search;

public class Minimax
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

    public Minimax(Position pos)
    {
        this.pos = pos;
    }
    
    public Move FindBestMove()  //houdt rekening met de tegenstander's countermoves en kiest de beste uitkomst      is technisch gezien minimax
    {
        Move bestMove = new Move();
        double currentEval = Eval.MaterialScore(pos);
        
        
        foreach (var move in moves)
        {
            pos.MakeMove(move);
            foreach (var countermove in MoveGenerator.GeneratePseudoLegalMoves(Pos))
            {
                pos.MakeMove(countermove);
                if (currentEval < Eval.MaterialScore(pos))
                {
                    bestMove = move;
                    currentEval = Eval.MaterialScore(pos);
                }
                pos.UndoMove();
            }
            pos.UndoMove();
        }
        return bestMove;
    }
}