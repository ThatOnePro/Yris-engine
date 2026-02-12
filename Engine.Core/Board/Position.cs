using Engine.Core.Moves;

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
    
    //TODO: Speciale moves nog verder afwerken, castling rechten weghalen wanneer een castle piece moved, en passant implementeren, promotie implementeren, promotie handling afmaken
    public void MakeMove(Move move)
    {
        var movingPiece = Squares[move.From];
        var capturedPiece = Squares[move.To]; // belangerijk om te onthouden elke move is een capture want een leeg veld is een piece called "None"

        _history.Push(new UndoState(
            capturedPiece,
            EnPassantSquare,
            Castling,
            HalfmoveClock,
            FullmoveNumber
        ));

        Squares[move.To] = move.Promotion != Piece.None
            ? move.Promotion
            : movingPiece;

        Squares[move.From] = Piece.None;

        EnPassantSquare = -1;

        if (movingPiece == Piece.WP || movingPiece == Piece.BP || capturedPiece != Piece.None)
            HalfmoveClock = 0;
        else
            HalfmoveClock++;

        if (SideToMove == Color.Black)
            FullmoveNumber++;

        SideToMove = SideToMove.Opposite();
    }

    
    public void UndoMove(Move move)
    {
        var undo = _history.Pop();

        SideToMove = SideToMove.Opposite();

        if (SideToMove == Color.Black)
            FullmoveNumber--;

        var movingPiece = Squares[move.To];

        Squares[move.From] = move.Promotion != Piece.None
            ? (SideToMove == Color.White ? Piece.WP : Piece.BP)
            : movingPiece;

        Squares[move.To] = undo.Captured;

        EnPassantSquare = undo.EnPassantSquare;
        Castling = undo.CastlingRights;
        HalfmoveClock = undo.HalfMoveClock;
        FullmoveNumber = undo.FullMoveNumber;
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
