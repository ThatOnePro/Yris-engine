using Engine.Core.Board;
using Engine.Core.Notation;

namespace Engine.Core.Moves;

public static class MoveParser
{
    /// <summary>
    /// Parses a move in UCI notation (e.g., "e2e4" or "e7e8q") and returns the corresponding Move object.
    /// </summary>
    /// <param name="position">The current position to validate the move</param>
    /// <param name="uciMove">The move in UCI notation</param>
    /// <returns>The parsed Move object</returns>
    public static Move FromUci(Position position, string uciMove)
    {
        if (string.IsNullOrWhiteSpace(uciMove) || uciMove.Length < 4)
            throw new ArgumentException($"Invalid UCI move format: {uciMove}");

        // Parse source and destination squares
        int from = Square.FromString(uciMove.Substring(0, 2));
        int to = Square.FromString(uciMove.Substring(2, 2));

        // Check for promotion
        Piece promotion = Piece.None;
        if (uciMove.Length >= 5)
        {
            char promotionChar = uciMove[4];
            bool isWhitePromotion = position.SideToMove == Color.White;

            promotion = promotionChar switch
            {
                'q' => isWhitePromotion ? Piece.WQ : Piece.BQ,
                'r' => isWhitePromotion ? Piece.WR : Piece.BR,
                'b' => isWhitePromotion ? Piece.WB : Piece.BB,
                'n' => isWhitePromotion ? Piece.WN : Piece.BN,
                _ => throw new ArgumentException($"Invalid promotion piece: {promotionChar}")
            };
        }

        return new Move(from, to, promotion);
    }
}

