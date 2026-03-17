namespace Engine.Core.Board;

public enum Piece : byte
{
    None = 0,
    
    WP, WN, WB, WR, WQ, WK,
    BP, BN, BB, BR, BQ, BK
}

public enum PieceType : byte
{
    None = 0,
    Pawn = 1, Knight = 2, Bishop = 3, Rook = 4, Queen = 5, King = 6 
}

public static class PieceExtensions
{
    public static bool IsWhite(this Piece piece) => piece is >= Piece.WP and <= Piece.WK;
    public static bool IsBlack(this Piece piece) => piece is >= Piece.BP and <= Piece.BK;

    public static int GetPieceType(Piece piece) => piece switch
    {
        Piece.WK => 6, Piece.BK => 6,
        Piece.WQ => 5, Piece.BQ => 5,
        Piece.WR => 4,  Piece.BR => 4,
        Piece.WB => 3, Piece.BB => 3,
        Piece.WN => 2, Piece.BN => 2,
        Piece.WP => 1,   Piece.BP => 1,
        _ => 0
    };
}
