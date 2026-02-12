using Engine.Core.Board;
using Engine.Core.Moves;

namespace Engine.Core.Tests;

public class BasicTests
{
        
    [Fact]
    public void MakeUndo_ShouldRestorePosition()
    {
        var pos = Position.CreateStartPosition();
        var original = pos.Squares.ToArray();

        var move = new Move(12, 28); // e2 to e4

        pos.MakeMove(move);
        pos.UndoMove(move);

        Assert.Equal(original, pos.Squares);
    }
}