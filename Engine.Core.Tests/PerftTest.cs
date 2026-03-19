using Engine.Core.Board;
using Engine.Core.Search;

namespace Engine.Core.Tests;

public class PerftTest
{
    [Fact]
    public void Test1()
    {
    }

    [Fact]
    public void Benchmark_Depth1_StartPosition_ReportsExpectedNodesAndMetrics()
    {
        var position = Fen.Parse("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");

        var result = Perft.Benchmark(position, 1);

        Assert.Equal(20, result.Nodes);
        Assert.True(result.ElapsedMilliseconds >= 0);
        Assert.True(result.Nps >= 0);
    }
}