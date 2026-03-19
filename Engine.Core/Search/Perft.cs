using Engine.Core.Board;
using Engine.Core.MoveGen;
using Engine.Core.Moves;
using System.Diagnostics;

namespace Engine.Core.Search;

public static class Perft
{
    public readonly record struct PerftBenchmarkResult(long Nodes, long ElapsedMilliseconds, long Nps);

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

    public static PerftBenchmarkResult Benchmark(Position pos, int depth)
    {
        if (depth < 0)
            throw new ArgumentOutOfRangeException(nameof(depth), "Depth must be >= 0.");

        var benchmarkPosition = pos.Clone();
        var sw = Stopwatch.StartNew();
        long nodes = Run(benchmarkPosition, depth);
        sw.Stop();

        long elapsedMs = sw.ElapsedMilliseconds;
        long nps = sw.ElapsedTicks > 0
            ? (long)(nodes * (double)Stopwatch.Frequency / sw.ElapsedTicks)
            : 0;

        return new PerftBenchmarkResult(nodes, elapsedMs, nps);
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
    
}