using Engine.Core.Board;
using Engine.Core.Book.bins;
using Engine.Core.MoveGen;
using Engine.Core.Moves;
using Engine.Core.Search;

namespace Engine.Core.Tests;

public class BasicTests
{
    private static bool s_bookLoaded;
    private static readonly object s_bookLoadLock = new();

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

    [Theory]
    [InlineData("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", 0x463B96181691FC9CUL)]
    [InlineData("rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1", 0x823C9B50FD114196UL)]
    [InlineData("rnbqkbnr/pppppppp/8/8/3pP3/8/PPP1PPPP/RNBQKBNR b KQkq e3 0 1", 0xCB53F9B62DD4E8E2UL)]
    [InlineData("rnbq1rk1/ppp2ppp/3bpn2/3p4/3P4/2NBPN2/PPP2PPP/R1BQ1RK1 w - - 0 8", 0xDB7D978D401881D9UL)]
    public void PolyglotHash_ShouldMatchKnownVectors(string fen, ulong expected)
    {
        var pos = Fen.Parse(fen);

        Assert.Equal(expected, pos.PolyglotKey);
        Assert.Equal(expected, Polyglot.ComputeHash(pos));
    }

    [Fact]
    public void PolyglotDecodeMove_ShouldNormalizeCastlingSquares()
    {
        // e1h1 in Polyglot must become e1g1 in normal move representation.
        ushort polyCastleKingSide = (ushort)((4 << 6) | 7);

        var move = OpeningBook.DecodeMove(polyCastleKingSide, Color.White);

        Assert.Equal(4, move.From);
        Assert.Equal(6, move.To);
    }

    [Fact]
    public void FindBestMove_WithLoadedBookFromStartPosition_ShouldReturnLegalMove()
    {
        EnsureOpeningBookLoaded();

        var pos = Position.CreateStartPosition();
        var bestMove = Negamax.FindBestMove(pos, depth: 3);
        var legalMoves = MoveGenerator.GenerateLegalMoves(pos);

        Assert.Contains(bestMove, legalMoves);
    }

    [Fact]
    public void GetBestMove_FromStartDepth1_ShouldPreferPrincipledOpeningMove()
    {
        var pos = Position.CreateStartPosition();

        var bestMove = Negamax.GetBestMove(pos, depth: 1);

        var preferredMoves = new HashSet<Move>
        {
            new(12, 28), // e2e4
            new(11, 27), // d2d4
            new(12, 20), // e2e3
            new(11, 19), // d2d3
            new(10, 26), // c2c4
            new(10, 18), // c2c3
            new(6, 21),  // g1f3
            new(1, 18)   // b1c3
        };

        var antiPrincipledMoves = new HashSet<Move>
        {
            new(3, 11),  // Qd1d2
            new(3, 19),  // Qd1d3
            new(3, 27),  // Qd1d4
            new(8, 16),  // a2a3
            new(8, 24),  // a2a4
            new(9, 17),  // b2b3
            new(9, 25),  // b2b4
            new(14, 22), // g2g3
            new(14, 30), // g2g4
            new(15, 23), // h2h3
            new(15, 31)  // h2h4
        };

        Assert.Contains(bestMove, preferredMoves);
        Assert.DoesNotContain(bestMove, antiPrincipledMoves);
    }

    private static void EnsureOpeningBookLoaded()
    {
        if (s_bookLoaded)
            return;

        lock (s_bookLoadLock)
        {
            if (s_bookLoaded)
                return;

            string baseDir = AppContext.BaseDirectory;
            string bookPath = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "Engine.Core", "Book", "bins", "Titans.bin"));

            if (!File.Exists(bookPath))
            {
                throw new FileNotFoundException($"Could not find opening book at expected path: {bookPath}");
            }

            OpeningBook.Load(bookPath);
            s_bookLoaded = true;
        }
    }
}