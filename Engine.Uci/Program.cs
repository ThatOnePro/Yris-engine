using Engine.Core.Board;
using Engine.Core.Moves;
using Engine.Core.Search;

Position position = new Position();
Color engineColor = Color.White;
Task? searchTask = null;

while (true)
{
    string? command = Console.ReadLine();
    if (command == null) continue;

    if (command == "uci")
    {
        Console.WriteLine("id name Yris-Engine");
        Console.WriteLine("id author Axel Peeters, Lars Peleman, Jan Van Herck");
        Console.WriteLine("option name Hash type spin default 16 min 1 max 1024");
        Console.WriteLine("option name Threads type spin default 1 min 1 max 1");
        Console.WriteLine("uciok");
    }

    else if (command == "isready")
    {
        Console.WriteLine("readyok");
    }

    else if (command == "ucinewgame")
    {
        TimeManager.Stop();
        searchTask?.Wait(500);
        position = new Position();
        TranspositionTable.Clear();
    }

    else if (command.StartsWith("position"))
    {
        ParsePosition(command, ref position, ref engineColor);
    }

    else if (command.StartsWith("go"))
    {
        // Stop any running search first
        TimeManager.Stop();
        searchTask?.Wait(500);

        int maxDepth = 7;
        long allocatedTimeMs = -1;
        bool infinite = false;
        bool movetime = false;
        int perftDepth = -1;

        var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 1; i < parts.Length; i++)
        {
            if (parts[i] == "infinite")
            {
                infinite = true;
            }
            else if (parts[i] == "perft" && i + 1 < parts.Length && int.TryParse(parts[i + 1], out var pd))
            {
                perftDepth = pd;
                i++;
            }
            else if (parts[i] == "depth" && i + 1 < parts.Length && int.TryParse(parts[i + 1], out var d))
            {
                maxDepth = d;
                i++;
            }
            else if (parts[i] == "movetime" && i + 1 < parts.Length && long.TryParse(parts[i + 1], out var mt))
            {
                allocatedTimeMs = mt;
                movetime = true;
                i++;
            }
            else if (parts[i] == "wtime" && i + 1 < parts.Length && long.TryParse(parts[i + 1], out var wt))
            {
                if (engineColor == Color.White && !movetime && allocatedTimeMs < 0)
                    allocatedTimeMs = Math.Max(10, wt / 40);
                i++;
            }
            else if (parts[i] == "btime" && i + 1 < parts.Length && long.TryParse(parts[i + 1], out var bt))
            {
                if (engineColor == Color.Black && !movetime && allocatedTimeMs < 0)
                    allocatedTimeMs = Math.Max(10, bt / 40);
                i++;
            }
            else if (parts[i] == "winc" && i + 1 < parts.Length && long.TryParse(parts[i + 1], out var wi))
            {
                if (engineColor == Color.White && !movetime)
                    allocatedTimeMs = allocatedTimeMs < 0 ? wi / 2 : allocatedTimeMs + wi / 2;
                i++;
            }
            else if (parts[i] == "binc" && i + 1 < parts.Length && long.TryParse(parts[i + 1], out var bi))
            {
                if (engineColor == Color.Black && !movetime)
                    allocatedTimeMs = allocatedTimeMs < 0 ? bi / 2 : allocatedTimeMs + bi / 2;
                i++;
            }
            else if (parts[i] == "movestogo" && i + 1 < parts.Length)
            {
                // consumed but not used (already handled by wtime/btime division)
                i++;
            }
        }

        if (infinite || allocatedTimeMs < 0)
            allocatedTimeMs = long.MaxValue;

        var searchPos = position.Clone();
        int localMaxDepth = maxDepth;
        long localTimeMs = allocatedTimeMs;
        int localPerftDepth = perftDepth;

        searchTask = Task.Run(() =>
        {
            if (localPerftDepth > 0)
            {
                var result = Perft.Benchmark(searchPos, localPerftDepth);
                Console.WriteLine($"info nodes {result.Nodes} time {result.ElapsedMilliseconds} nps {result.Nps}");
                Console.WriteLine("bestmove 0000");
                return;
            }

            TimeManager.StartSearch(localTimeMs);

            Move bestMove = default;
            int bestScore = 0;

            for (int d = 1; d <= localMaxDepth; d++)
            {
                if (TimeManager.TimeExpired) break;

                var result = Negamax.GetBestMove(searchPos, d);

                // Only commit result if depth completed fully (or it's the first depth)
                bool completed = !TimeManager.TimeExpired || d == 1;
                if (completed)
                {
                    bestMove = result.BestMove;
                    bestScore = result.Score;

                    long elapsed = Math.Max(1, TimeManager.GetElapsedMs());
                    long nps = result.Nodes * 1000 / elapsed;
                    string scoreStr = FormatScore(bestScore);
                    string pvStr = bestMove == default ? "0000" : bestMove.ToUci();
                    Console.WriteLine($"info depth {d} score {scoreStr} nodes {result.Nodes} time {elapsed} nps {nps} pv {pvStr}");
                }
            }

            string bestMoveStr = bestMove == default ? "0000" : bestMove.ToUci();
            Console.WriteLine($"bestmove {bestMoveStr}");
        });
    }

    else if (command.StartsWith("bench"))
    {
        TimeManager.Stop();
        searchTask?.Wait(500);

        int depth = 10;
        var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length > 1 && !int.TryParse(parts[1], out depth))
        {
            Console.WriteLine("info string usage: bench [depth]");
            continue;
        }

        if (depth < 1)
        {
            Console.WriteLine("info string bench requires depth >= 1");
            continue;
        }

        var benchmarkFens = new[]
        {
            "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1",
            "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1",
            "8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1",
            "rnbqkb1r/pp1p1ppp/2p5/4p3/2B1P3/5N2/PPPP1PPP/RNBQK2R w KQkq - 0 1",
            "r1bqkbnr/pppp1ppp/2n5/1B2p3/4P3/5N2/PPPP1PPP/RNBQK2R w KQkq - 0 1",
        };

        long totalNodes = 0;
        long totalMs = 0;

        Console.WriteLine($"info string Starting bench at depth {depth}");

        for (int i = 0; i < benchmarkFens.Length; i++)
        {
            var benchPos = Fen.Parse(benchmarkFens[i]);
            
            // Initialize TimeManager for this position's search
            TimeManager.StartSearch(long.MaxValue);
            
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var searchResult = Negamax.GetBestMove(benchPos, depth);
            sw.Stop();

            long nps = sw.ElapsedMilliseconds > 0 ? searchResult.Nodes * 1000 / sw.ElapsedMilliseconds : 0;
            totalNodes += searchResult.Nodes;
            totalMs += sw.ElapsedMilliseconds;

            Console.WriteLine($"info string bench position {i + 1}/5 nodes {searchResult.Nodes} time {sw.ElapsedMilliseconds} nps {nps}");
        }

        long avgNps = totalMs > 0 ? totalNodes * 1000 / totalMs : 0;
        Console.WriteLine($"info string bench done depth {depth} totalNodes {totalNodes} totalTime {totalMs} avgNps {avgNps}");
    }

    else if (command == "stop")
    {
        TimeManager.Stop();
        searchTask?.Wait(500);
    }

    else if (command == "quit")
    {
        TimeManager.Stop();
        searchTask?.Wait(500);
        break;
    }
}


static void ParsePosition(string command, ref Position pos, ref Color engineColor)
{
    var parts = command.Split(' ');
    if (parts.Length < 2) return;

    int index = 1;

    if (parts[index] == "startpos")
    {
        pos = Fen.Parse("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
        index++;
    }
    else if (parts[index] == "fen")
    {
        string fen = string.Join(" ", parts.Skip(index + 1).Take(6));
        pos = Fen.Parse(fen);
        index += 7;
    }

    if (index < parts.Length && parts[index] == "moves")
    {
        index++;
        for (int i = index; i < parts.Length; i++)
        {
            Move move = MoveParser.FromUci(pos, parts[i]);
            pos.MakeMove(move);
        }
    }

    engineColor = pos.SideToMove;
}

static string FormatScore(int score)
{
    const int MateScore = 100000;
    const int MateThreshold = 99000;

    if (score >= MateThreshold)
    {
        int plies = MateScore - score;
        int moves = (plies + 1) / 2;
        return $"mate {moves}";
    }
    if (score <= -MateThreshold)
    {
        int plies = MateScore + score;
        int moves = (plies + 1) / 2;
        return $"mate -{moves}";
    }
    return $"cp {score}";
}
