using Engine.Core.Board;
using Engine.Core.Search;

// var pos = Fen.Parse("8/P7/8/8/8/8/7p/8 w - - 0 1");
Console.WriteLine("Running PERFT...");
var pos = Position.CreateStartPosition();
Console.WriteLine(Perft.Run(pos, 6));

// Console.WriteLine(Perft.Run(pos, 1));
// Perft.Divide(pos, 1);
// Perft.Divide(Position.CreateStartPosition(), 2);
