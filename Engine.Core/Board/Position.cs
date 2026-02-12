namespace Engine.Core.Board;

public sealed class Position
{
    public Piece[] Squares { get; } = new Piece[64];
    public Color SideToMove { get; set; } = Color.White;
    
    private readonly Stack<UndoState> _history = new();

    
    public CastlingRights Castling { get; set; } =
        CastlingRights.WhiteKingSide | CastlingRights.WhiteQueenSide |
        CastlingRights.BlackKingSide | CastlingRights.BlackQueenSide;

    public int EnPassantSquare { get; set; } = -1;
    public int HalfmoveClock { get; set; } = 0;
    public int FullmoveNumber { get; set; } = 1;

    public static Position CreateStartPosition()
    {
        var position = new Position();
        
        // White pieces
        position.Squares[0] = Piece.WR; position.Squares[1] = Piece.WN; position.Squares[2] = Piece.WB; position.Squares[3] = Piece.WQ; 
        position.Squares[4] = Piece.WK; position.Squares[5] = Piece.WB; position.Squares[6] = Piece.WN; position.Squares[7] = Piece.WR;
        for (int i = 8; i < 16; i++)
            position.Squares[i] = Piece.WP;
        
        // Black pieces
        position.Squares[56] = Piece.BR; position.Squares[57] = Piece.BN; position.Squares[58] = Piece.BB; position.Squares[59] = Piece.BQ;
        position.Squares[60] = Piece.BK; position.Squares[61] = Piece.BB; position.Squares[62] = Piece.BN; position.Squares[63] = Piece.BR;
        for (int i = 48; i < 56; i++)
            position.Squares[i] = Piece.BP;

        position.SideToMove = Color.White;
        return position;
    }
    


} 

[Flags]
public enum CastlingRights : byte
{
    None = 0,
    WhiteKingSide = 1,
    WhiteQueenSide = 2,
    BlackKingSide = 4,
    BlackQueenSide = 8
}
