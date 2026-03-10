// using Engine.Core.Board;
// using Engine.Core.Moves;
// using Engine.Core.Search;

// // Inital
// var pos1 = Fen.Parse("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1 \n");
// // Kiwipete
// var pos2 = Fen.Parse("r3k2r/pppq1ppp/2n1bn2/3p4/3P4/2N1BN2/PPPQ1PPP/R3K2R w KQkq - 0 1 \n");
// // 
// var pos3 = Fen.Parse("8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1 \n");
// var pos4 = Fen.Parse("r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1\n");
// var pos5 = Fen.Parse("rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8  \n");
// var pos6 = Fen.Parse("r4rk1/1pp1qppp/p1np1n2/2b1p1B1/2B1P1b1/P1NP1N2/1PP1QPPP/R4RK1 w - - 0 10 \n");
// Console.WriteLine("Running PERFT, pos 1");
// Console.WriteLine("Expected: 20, 400, 8902");
// for (int i = 1; i <= 4; i++){
//     Console.WriteLine(Perft.Run(pos1, i));
// }
//
// Console.WriteLine("Running PERFT, pos 2");
// Console.WriteLine("Expected: 48, 2039, 97862");
// for (int i = 1; i <= 3; i++)
// {
//     Console.WriteLine(Perft.Run(pos2, i));
//
// }
//
// Console.WriteLine("Running PERFT, pos 3");
// Console.WriteLine("Expected: 14, 191, 2812");
// for (int i = 1; i <= 3; i++)
// {
//     Console.WriteLine(Perft.Run(pos3, i));
//
// }
//
// Console.WriteLine("Running PERFT, pos 4");
// Console.WriteLine("Expected: 6, 264, 9467");
// for (int i = 1; i <= 3; i++)
// {
//     Console.WriteLine(Perft.Run(pos4, i)); 
// }
//
// Console.WriteLine("Running PERFT, pos 5");
// Console.WriteLine("Expected: 44, 1486, 62379");
// for (int i = 1; i <= 3; i++)
// {
//     Console.WriteLine(Perft.Run(pos5, i)); 
// }
//
// Console.WriteLine("Running PERFT, pos 6");
// Console.WriteLine("Expected: 46, 2079, 89890");
// for (int i = 1; i <= 3; i++){
//     Console.WriteLine(Perft.Run(pos6, i));
// }


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



