using Engine.Core.Board;
using Engine.Core.Moves;
using Engine.Core.Search;

Position position = new Position();
var depth = 5;
Color engineColor = Color.White;

while (true)
{
    string? command = Console.ReadLine();
    if (command == null) continue;

    if (command == "uci")
    {
        Console.WriteLine("id name Yris-Engine");
        Console.WriteLine("id author Axel Peeters, Lars Peleman, Jan Van Herck");
        Console.WriteLine("uciok");
    }

    else if (command == "isready")
    {
        Console.WriteLine("readyok");
    }

    else if (command == "ucinewgame")
    {
        position = new Position();
        TranspositionTable.Clear();
    }

    else if (command.StartsWith("position"))
    {
        ParsePosition(command, ref position, ref engineColor);
    }

    else if (command.StartsWith("go"))
    {
        int searchDepth = depth;
        long allocatedTimeMs = 1000; // default 1 second
        
        var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 1; i < parts.Length; i++)
        {
            if (parts[i] == "depth" && i + 1 < parts.Length && int.TryParse(parts[i + 1], out var d))
            {
                searchDepth = d;
            }
            else if (parts[i] == "movetime" && i + 1 < parts.Length && long.TryParse(parts[i + 1], out var mt))
            {
                allocatedTimeMs = mt;
            }
            else if (parts[i] == "wtime" && i + 1 < parts.Length && long.TryParse(parts[i + 1], out var wt))
            {
                if (engineColor == Color.White)
                {
                    // Use 1/40th of remaining time + 1 second increment if provided
                    allocatedTimeMs = Math.Max(10, wt / 40);
                }
            }
            else if (parts[i] == "btime" && i + 1 < parts.Length && long.TryParse(parts[i + 1], out var bt))
            {
                if (engineColor == Color.Black)
                {
                    allocatedTimeMs = Math.Max(10, bt / 40);
                }
            }
            else if (parts[i] == "winc" && i + 1 < parts.Length && long.TryParse(parts[i + 1], out var wi))
            {
                if (engineColor == Color.White)
                {
                    allocatedTimeMs += wi;
                }
            }
            else if (parts[i] == "binc" && i + 1 < parts.Length && long.TryParse(parts[i + 1], out var bi))
            {
                if (engineColor == Color.Black)
                {
                    allocatedTimeMs += bi;
                }
            }
        }

        TimeManager.StartSearch(allocatedTimeMs);
        var bestMove = Negamax.GetBestMove(position, depth: searchDepth);
        Console.WriteLine($"bestmove {bestMove.ToUci()}");
    }

    else if (command == "stop")
    {
        // ignore for now
    }

    else if (command == "quit")
    {
        break;
    }

    else if (command.StartsWith("depth"))
    {
        var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2 && int.TryParse(parts[1], out var newDepth) && newDepth > 0)
        {
            depth = newDepth;
        }
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

    // Update engine color based on side to move
    engineColor = pos.SideToMove;
}

// var move = Negamax.GetBestMove(Fen.Parse("rnbqkbnr/ppp2p1p/3p2p1/4p2Q/2B1P3/3P4/PPP2PPP/RNB1K1NR b KQkq - 0 4"), 4);
// Console.WriteLine($"{move.ToUci()}");

// var tests = new List<PerftTest>
// {
//     new("Initial Position",
//         "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1",
//         new long[] {20, 400, 8902, 197281, 4865609}),
//
//     new("Kiwipete",
//         "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1",
//         new long[] {48, 2039, 97862, 4085603}),
//
//     new("Position 3",
//         "8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1",
//         new long[] {14, 191, 2812, 43238, 674624}),
//
//     new("Position 4",
//         "r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1",
//         new long[] {6, 264, 9467, 422333, 15833292}),
//
//     new("Position 5",
//         "rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8",
//         new long[] {44, 1486, 62379, 2103487}),
//
//     new("Position 6",
//         "r4rk1/1pp1qppp/p1np1n2/2b1p1B1/2B1P1b1/P1NP1N2/1PP1QPPP/R4RK1 w - - 0 10",
//         new long[] {46, 2079, 89890, 3894594})
// };
//
// foreach (var test in tests)
// {
//     Console.WriteLine($"===== {test.Name} =====");
//
//     var position = Fen.Parse(test.Fen);
//
//     for (int depth = 1; depth <= test.Expected.Length; depth++)
//     {
//         var result = Perft.Run(position, depth);
//         var expected = test.Expected[depth - 1];
//
//         var status = result == expected ? "PASS" : "FAIL";
//
//         Console.WriteLine(
//             $"Depth {depth,-2} | Result: {result,-10} | Expected: {expected,-10} | {status}");
//     }
//
//     Console.WriteLine();
// }
//
// Perft.Divide(Fen.Parse(tests[1].Fen), 3);
// public record PerftTest(string Name, string Fen, long[] Expected);

