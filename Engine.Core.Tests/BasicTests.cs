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

    [Fact]
    public void RepetitionDetection_ShouldDetectThreefoldFromHistory()
    {
        var pos = Position.CreateStartPosition();

        var repetitionLine = new[]
        {
            new Move(6, 21),  // Ng1-f3
            new Move(62, 45), // Ng8-f6
            new Move(21, 6),  // Nf3-g1
            new Move(45, 62), // Nf6-g8
            new Move(6, 21),
            new Move(62, 45),
            new Move(21, 6),
            new Move(45, 62)
        };

        foreach (var move in repetitionLine)
        {
            pos.MakeMove(move);
        }

        Assert.True(pos.IsThreefoldRepetition());
    }

    [Fact]
    public void RepetitionScore_ShouldPreferDrawWhenMateriallyBehind()
    {
        var winningSideToMove = Fen.Parse("4k3/8/8/8/8/8/8/3QK3 w - - 0 1");
        var losingSideToMove = Fen.Parse("3qk3/8/8/8/8/8/8/4K3 w - - 0 1");

        int winningScore = Eval.GetRepetitionScore(winningSideToMove, ply: 1);
        int losingScore = Eval.GetRepetitionScore(losingSideToMove, ply: 1);

        Assert.True(winningScore < 0);
        Assert.True(losingScore > 0);
    }

    [Fact]
    public void TT_ProbeMiss_ShouldNotLeakBestMoveFromCollidingEntry()
    {
        TranspositionTable.Clear();

        ulong keyA = 1;
        ulong keyB = keyA + (1UL << 20); // same TT bucket as keyA
        var bestMoveA = new Move(12, 28);

        TranspositionTable.Store(keyA, depth: 4, score: 25, flag: TTFlag.Exact, bestMove: bestMoveA);

        bool hit = TranspositionTable.Probe(
            keyB,
            depth: 1,
            alpha: -1000,
            beta: 1000,
            out _,
            out Move probedMove);

        Assert.False(hit);
        Assert.Equal(default, probedMove);
    }

    [Fact]
    public void TT_Collision_ShouldKeepDeeperEntry()
    {
        TranspositionTable.Clear();

        ulong keyA = 7;
        ulong keyB = keyA + (1UL << 20); // same TT bucket as keyA
        var moveA = new Move(1, 18);

        TranspositionTable.Store(keyA, depth: 6, score: 42, flag: TTFlag.Exact, bestMove: moveA);
        TranspositionTable.Store(keyB, depth: 2, score: -13, flag: TTFlag.Exact, bestMove: new Move(6, 21));

        bool hitA = TranspositionTable.Probe(keyA, depth: 1, alpha: -1000, beta: 1000, out int scoreA, out Move bestMoveA);

        Assert.True(hitA);
        Assert.Equal(42, scoreA);
        Assert.Equal(moveA, bestMoveA);
    }
}