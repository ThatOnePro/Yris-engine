using Engine.Core.Board;
using Engine.Core.Notation;

namespace Engine.Core.Moves;

public static class MoveExtensions
{
    public static string ToUci(this Move move)
    {
        var from = Square.ToString(move.From);
        var to = Square.ToString(move.To);

        if (move.Promotion == Piece.None)
            return from + to;

        char promo = move.Promotion switch
        {
            Piece.WQ or Piece.BQ => 'q',
            Piece.WR or Piece.BR => 'r',
            Piece.WB or Piece.BB => 'b',
            Piece.WN or Piece.BN => 'n',
            _ => '?'
        };

        return from + to + promo;
    }
}