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
}