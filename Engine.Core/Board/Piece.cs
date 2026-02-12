namespace Engine.Core.Board;

public enum Piece : byte
{
    None = 0,
    
    WP, WN, WB, WR, WQ, WK,
    BP, BN, BB, BR, BQ, BK
}

public static class PieceExtensions
{
    public static bool IsWhite(this Piece piece) => piece is >= Piece.WP and <= Piece.WK;
    public static bool IsBlack(this Piece piece) => piece is >= Piece.BP and <= Piece.BK;
}