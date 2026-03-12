using Engine.Core.Board;
using Engine.Core.Moves;
using Engine.Core.Search;

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
        pos.UndoMove();

        Assert.Equal(original, pos.Squares);
    }

    [Fact]
    public void FenParse_ShouldComputeZobristKey()
    {
        var pos = Fen.Parse("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");

        Assert.Equal(Zobrist.ComputeHash(pos), pos.ZobristKey);
    }

    [Fact]
    public void Search_FromParsedFen_ShouldReturnLegalMove()
    {
        var pos = Fen.Parse("rnbqkbnr/ppp2p1p/3p2p1/4p2Q/2B1P3/3P4/PPP2PPP/RNB1K1NR b KQkq - 0 4");

        var bestMove = Negamax.GetBestMove(pos, 3);
        var legalMoves = Engine.Core.MoveGen.MoveGenerator.GenerateLegalMoves(pos);

        Assert.Contains(bestMove, legalMoves);
    }

    [Fact]
    public void EnPassant_MakeUndo_ShouldRestoreBoardAndHash()
    {
        var pos = Fen.Parse("4k3/8/8/3pP3/8/8/8/4K3 w - d6 0 1");
        var originalSquares = pos.Squares.ToArray();
        var originalHash = pos.ZobristKey;
        var originalSide = pos.SideToMove;
        var originalCastling = pos.Castling;
        var originalEnPassant = pos.EnPassantSquare;
        var originalHalfmove = pos.HalfmoveClock;
        var originalFullmove = pos.FullmoveNumber;

        pos.MakeMove(new Move(36, 43)); // e5xd6 ep
        pos.UndoMove();

        Assert.Equal(originalSquares, pos.Squares);
        Assert.Equal(originalHash, pos.ZobristKey);
        Assert.Equal(originalSide, pos.SideToMove);
        Assert.Equal(originalCastling, pos.Castling);
        Assert.Equal(originalEnPassant, pos.EnPassantSquare);
        Assert.Equal(originalHalfmove, pos.HalfmoveClock);
        Assert.Equal(originalFullmove, pos.FullmoveNumber);
    }
}