namespace Engine.Core.Moves;

public readonly struct ScoredMove
{
    public readonly Move Move;
    public readonly int Score;

    public ScoredMove(Move move, int score)
    {
        Move = move;
        Score = score;
    }
}