using Engine.Core.Board;
using Engine.Core.MoveGen;

namespace Engine.Core.Tests;

public class MoveGenTest
{
    [Fact]
    public void StartPos_PseudoLegalMoveCount_Is20()
    {
        var pos = Position.CreateStartPosition();
        var moves = MoveGenerator.GeneratePseudoLegalMoves(pos);

        Assert.Equal(20, moves.Count);
    }

    [Fact]
    public void StartPos_LegalCaptures_IsEmpty()
    {
        var pos = Position.CreateStartPosition();
        var moves = MoveGenerator.GenerateLegalCaptures(pos);

        Assert.Empty(moves);
    }

    [Fact]
    public void EnPassantPosition_LegalCaptures_ContainsOnlyEnPassantCapture()
    {
        var pos = Fen.Parse("4k3/8/8/3pP3/8/8/8/4K3 w - d6 0 1");
        var moves = MoveGenerator.GenerateLegalCaptures(pos);

        Assert.Single(moves);
        Assert.Equal(new Engine.Core.Moves.Move(36, 43), moves[0]);
    }
}