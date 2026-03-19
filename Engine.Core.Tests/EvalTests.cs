using Engine.Core.Board;

namespace Engine.Core.Tests;

public class EvalTests
{
    [Fact]
    public void MaterialOnlyScore_StartPosition_ShouldStayBalanced()
    {
        var position = Position.CreateStartPosition();

        int score = Eval.MaterialOnlyScore(position);

        Assert.InRange(score, -20, 20);
    }

    [Fact]
    public void MaterialOnlyScore_ShouldRewardActiveKingMoreInEndgameThanOpening()
    {
        var endgame = Fen.Parse("4k3/8/8/3K4/8/8/8/8 w - - 0 1");
        var openingLike = Fen.Parse("rnbqkbnr/pppppppp/8/3K4/8/8/PPPPPPPP/RNBQ1BNR w - - 0 1");

        int endgameScore = Eval.MaterialOnlyScore(endgame);
        int openingLikeScore = Eval.MaterialOnlyScore(openingLike);

        Assert.True(
            endgameScore > openingLikeScore,
            $"Expected endgame score ({endgameScore}) to be greater than opening-like score ({openingLikeScore}) for the same active king placement.");
    }
}

