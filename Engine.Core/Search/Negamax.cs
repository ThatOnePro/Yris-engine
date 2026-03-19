using System.Collections.Concurrent;
using Engine.Core.Board;
using Engine.Core.Book.bins;
using Engine.Core.MoveGen;
using Engine.Core.Moves;

namespace Engine.Core.Search;

public static class Negamax
{
    private const int MateScore = 100000;
    private const int MateThreshold = MateScore - 1000;

    public static List<ScoredMove> OrderMoves(Position pos, List<Move> moves, Move ttMove = default)
    {
        List<ScoredMove> orderedMoves = new(moves.Count);

        foreach (var move in moves)
        {
            var movingPiece = pos.Squares[move.From];
            var capturedPiece = pos.Squares[move.To];

            int score = 0;

            // TT move gets highest priority
            if (move.Equals(ttMove))
            {
                score += 20000;
            }

            // MVV-LVA
            if (capturedPiece != 0)
            {
                score += 10000 +
                         100 * Math.Abs(Eval.GetPieceValue(capturedPiece)) -
                         Math.Abs(Eval.GetPieceValue(movingPiece));
            }

            // Promotion bonus
            if (move.Promotion != 0)
            {
                score += 5000;
            }

            orderedMoves.Add(new ScoredMove(move, score));
        }

        orderedMoves.Sort((a, b) => b.Score.CompareTo(a.Score));

        return orderedMoves;
    }
    
    public static Move GetBestMoveIterative(Position pos, int maxDepth)
    {
        Move bestMove = default;
    
        for (int currentDepth = 1; currentDepth <= maxDepth; currentDepth++)
        {
            // De parallelle loop profiteert van de resultaten van de vorige diepte in de TT
            bestMove = GetBestMoveParallel(pos, currentDepth);
        
            Console.WriteLine($"Depth {currentDepth} completed. Best move: {bestMove}");
        }
        return bestMove;
    }

    public static Move GetBestMoveParallel(Position pos, int depth)
    {
        var moves = MoveGenerator.GenerateLegalMoves(pos);
        var orderedMoves = OrderMoves(pos, moves);
    
        var options = new ParallelOptions { MaxDegreeOfParallelism = 4 };
        
        // Gebruik een thread-safe container voor de resultaten
        var results = new ConcurrentBag<(Move move, double score)>();
    
        Parallel.ForEach(
            orderedMoves, 
            options, 
            // Initialisatie: dit wordt één keer per thread uitgevoerd
            () => pos.Clone(), 
            // De eigenlijke loop body
            (moveWrap, loopState, threadSpecificPos) => 
            {
                threadSpecificPos.MakeMove(moveWrap.Move);
            
                // Gebruik stabiele integers voor Alpha/Beta om overflows te voorkomen
                double score = -NegamaxScore(threadSpecificPos, depth - 1, -1000000, 1000000, 1);
                score += GetOpeningRootBias(pos, moveWrap.Move);
            
                threadSpecificPos.UndoMove();
                results.Add((moveWrap.Move, score));
            
                return threadSpecificPos; // Geef het bord door naar de volgende iteratie op deze thread
            },
            // Finalisatie per thread (optioneel)
            (threadSpecificPos) => { }
        );

        return results.OrderByDescending(r => r.score).First().move;
    }
    public static Move FindBestMove(Position pos, int depth)
    {
        var legalMoves = MoveGenerator.GenerateLegalMoves(pos);
        var bookMove = OpeningBook.GetBookMove(pos.PolyglotKey, pos.SideToMove, legalMoves);

        if (bookMove != null)
            return bookMove.Value;

        return GetBestMove(pos, depth);
    }
    
    public static Move GetBestMove(Position pos, int depth)
    {
        int alpha = int.MinValue + 1;
        int beta = int.MaxValue;
        int bestValue = int.MinValue + 1;

        var moves = MoveGenerator.GenerateLegalMoves(pos);
        if (moves.Count == 0)
        {
            return default;
        }

        Move bestMove = moves[0];
        var orderedMoves = OrderMoves(pos, moves);

        foreach (var scoredMove in orderedMoves)
        {
            if (TimeManager.TimeExpired)
            {
                break;
            }

            int openingBias = GetOpeningRootBias(pos, scoredMove.Move);
            pos.MakeMove(scoredMove.Move);

            int boardValue = -NegamaxScore(pos, depth - 1, -beta, -alpha, 1);
            boardValue += openingBias;

            pos.UndoMove();

            if (boardValue > bestValue)
            {
                bestValue = boardValue;
                bestMove = scoredMove.Move;
            }

            alpha = Math.Max(alpha, bestValue);
        }

        return bestMove;
    }

    private static int GetOpeningRootBias(Position pos, Move move)
    {
        if (pos.FullmoveNumber > 12)
            return 0;

        int bonus = 0;
        Piece movingPiece = pos.Squares[move.From];

        if (IsCastleMove(movingPiece, move))
            bonus += 30;

        if (IsMainCenter(move.To))
            bonus += 20;
        else if (IsExtendedCenter(move.To))
            bonus += 10;

        if (IsMinorPiece(movingPiece) && IsHomeMinorSquare(movingPiece, move.From))
            bonus += 14;

        if (movingPiece == Piece.WQ || movingPiece == Piece.BQ)
        {
            if (pos.FullmoveNumber <= 8)
                bonus -= 20;
        }

        if (movingPiece == Piece.WP || movingPiece == Piece.BP)
        {
            int toFile = move.To % 8;
            bool flankPawnPush = toFile is 0 or 1 or 6 or 7;
            if (flankPawnPush && pos.FullmoveNumber <= 6)
                bonus -= 10;
        }

        return bonus;
    }

    private static bool IsCastleMove(Piece movingPiece, Move move)
    {
        return (movingPiece == Piece.WK && move.From == 4 && (move.To == 6 || move.To == 2)) ||
               (movingPiece == Piece.BK && move.From == 60 && (move.To == 62 || move.To == 58));
    }

    private static bool IsMinorPiece(Piece piece)
    {
        return piece is Piece.WN or Piece.BN or Piece.WB or Piece.BB;
    }

    private static bool IsHomeMinorSquare(Piece piece, int from)
    {
        return (piece == Piece.WN && (from == 1 || from == 6)) ||
               (piece == Piece.BN && (from == 57 || from == 62)) ||
               (piece == Piece.WB && (from == 2 || from == 5)) ||
               (piece == Piece.BB && (from == 58 || from == 61));
    }

    private static bool IsMainCenter(int square)
    {
        return square is 27 or 28 or 35 or 36;
    }

    private static bool IsExtendedCenter(int square)
    {
        return square is 18 or 19 or 20 or 21 or 26 or 29 or 34 or 37 or 42 or 43 or 44 or 45;
    }

    private static int NegamaxScore(Position pos, int depth, int alpha, int beta, int ply)
    {
        int originalAlpha = alpha;

        // Periodic time check
        TimeManager.RecordNodes(1);
        if (TimeManager.TimeExpired && ply > 0)
        {
            return Eval.MaterialScore(pos);
        }

        if (pos.HalfmoveClock >= 100 || (ply > 0 && pos.IsThreefoldRepetition()))
        {
            return Eval.GetRepetitionScore(pos, ply);
        }

        ulong hash = pos.ZobristKey;

        // Transposition Table Probe
        if (TranspositionTable.Probe(hash, depth, alpha, beta, out int ttScore, out Move ttMove))
        {
            return ScoreFromTT(ttScore, ply);
        }

        if (depth == 0)
        {
            return Quiescence(pos, alpha, beta, ply);
        }

        var moves = MoveGenerator.GenerateLegalMoves(pos);

        // Checkmate / stalemate
        if (moves.Count == 0)
        {
            if (AttackDetector.IsKingInCheck(pos, pos.SideToMove))
            {
                return -MateScore + ply;
            }

            return 0;
        }

        Move bestMove = default;

        var orderedMoves = OrderMoves(pos, moves, ttMove);

        int maxEval = int.MinValue + 1;

        foreach (var orderedMove in orderedMoves)
        {
            pos.MakeMove(orderedMove.Move);

            int eval = -NegamaxScore(pos, depth - 1, -beta, -alpha, ply + 1);

            pos.UndoMove();

            if (eval > maxEval)
            {
                maxEval = eval;
                bestMove = orderedMove.Move;
            }

            alpha = Math.Max(alpha, eval);

            if (alpha >= beta)
            {
                break; // beta cutoff
            }
        }

        // Determine TT flag
        TTFlag flag;

        if (maxEval <= originalAlpha)
        {
            flag = TTFlag.UpperBound;
        }
        else if (maxEval >= beta)
        {
            flag = TTFlag.LowerBound;
        }
        else
        {
            flag = TTFlag.Exact;
        }

        // Store in TT
        TranspositionTable.Store(hash, depth, ScoreToTT(maxEval, ply), flag, bestMove);

        return maxEval;
    }

    private static int Quiescence(Position pos, int alpha, int beta, int ply)
    {
        if (pos.HalfmoveClock >= 100 || pos.IsThreefoldRepetition())
        {
            return Eval.GetRepetitionScore(pos, ply);
        }

        int standPat = Eval.MaterialScore(pos);

        bool inCheck = AttackDetector.IsKingInCheck(pos, pos.SideToMove);

        if (!inCheck)
        {
            if (standPat >= beta)
            {
                return beta;
            }

            if (alpha < standPat)
            {
                alpha = standPat;
            }
        }

        var moves = OrderMoves(pos, MoveGenerator.GenerateLegalMoves(pos));

        foreach (var scoredMove in moves)
        {
            if (!inCheck &&
                pos.Squares[scoredMove.Move.To] == 0 &&
                scoredMove.Move.Promotion == 0)
                continue;

            pos.MakeMove(scoredMove.Move);

            int score = -Quiescence(pos, -beta, -alpha, ply + 1);

            pos.UndoMove();

            if (score >= beta)
            {
                return beta;
            }

            if (score > alpha)
            {
                alpha = score;
            }
        }

        return alpha;
    }

    private static int ScoreToTT(int score, int ply)
    {
        if (score >= MateThreshold)
            return score + ply;

        if (score <= -MateThreshold)
            return score - ply;

        return score;
    }

    private static int ScoreFromTT(int score, int ply)
    {
        if (score >= MateThreshold)
            return score - ply;

        if (score <= -MateThreshold)
            return score + ply;

        return score;
    }
}