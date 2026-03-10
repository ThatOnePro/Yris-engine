using Engine.Core.Board;
using Engine.Core.MoveGen;
using Engine.Core.Moves;

namespace Engine.Core.Search;

public static class Perft
{
    public static long Run(Position pos, int depth)
    {
        if (depth == 0)
            return 1;

        long nodes = 0;

        var moves = MoveGenerator.GenerateLegalMoves(pos);

        foreach (var move in moves)
        {
            pos.MakeMove(move);
            nodes += Run(pos, depth - 1);
            pos.UndoMove();
        }

        return nodes;
    }


    public static void Divide(Position pos, int depth)
    {
        var moves = MoveGenerator.GenerateLegalMoves(pos);

        long total = 0;

        foreach (var move in moves)
        {
            pos.MakeMove(move);
            long nodes = Run(pos, depth - 1);
            pos.UndoMove();

            Console.WriteLine($"{move.ToUci()} : {nodes}");

            total += nodes;
        }

        Console.WriteLine($"Total: {total}");
    }

    static string MoveToString(Move move)
    {
        return SquareName(move.From) + SquareName(move.To);
    }

    static string SquareName(int sq)
    {
        char file = (char)('a' + (sq % 8));
        char rank = (char)('1' + (sq / 8));
        return $"{file}{rank}";
    }
}