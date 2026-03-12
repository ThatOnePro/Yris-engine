using Engine.Core.Moves;

namespace Engine.Core.Board;

public sealed class Position
{
    public Piece[] Squares { get; set; } = new Piece[64];
    public Color SideToMove { get; set; } = Color.White;
    
    private readonly Stack<UndoState> _history = new();
    
    public ulong ZobristKey { get; set; } = 0;

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
        var capturedPiece = Squares[move.To];
        
        int color = (int)SideToMove;
        ZobristKey ^= Zobrist.GetPieceHash(PieceExtensions.GetPieceType(movingPiece), color, move.From);
        
        if (capturedPiece != Piece.None)
        {
            ZobristKey ^= Zobrist.GetPieceHash(PieceExtensions.GetPieceType(capturedPiece), 1 - color, move.To);
        }
        
        ZobristKey ^= Zobrist.GetPieceHash(PieceExtensions.GetPieceType(movingPiece), color, move.To);
        ZobristKey ^= Zobrist.SideToMove;
        
        
        bool isPawn = movingPiece == Piece.WP || movingPiece == Piece.BP;
        bool isEnPassantCapture = isPawn && move.To == EnPassantSquare && capturedPiece == Piece.None && (move.From % 8 != move.To % 8);
        int epCapturedSquare = -1;

        if (isEnPassantCapture)
        {
            epCapturedSquare = move.To + (movingPiece == Piece.WP ? -8 : 8);
            capturedPiece = Squares[epCapturedSquare];
        }

        _history.Push(new UndoState(
            move,
            capturedPiece,
            EnPassantSquare,
            Castling,
            HalfmoveClock,
            FullmoveNumber
        ));

        Squares[move.To] = move.Promotion != Piece.None ? move.Promotion : movingPiece;
        Squares[move.From] = Piece.None;

        if (isEnPassantCapture)
            Squares[epCapturedSquare] = Piece.None;

        // castling rook move
        if (movingPiece == Piece.WK && move.From == 4 && move.To == 6) { Squares[5] = Piece.WR; Squares[7] = Piece.None; }
        if (movingPiece == Piece.WK && move.From == 4 && move.To == 2) { Squares[3] = Piece.WR; Squares[0] = Piece.None; }
        if (movingPiece == Piece.BK && move.From == 60 && move.To == 62) { Squares[61] = Piece.BR; Squares[63] = Piece.None; }
        if (movingPiece == Piece.BK && move.From == 60 && move.To == 58) { Squares[59] = Piece.BR; Squares[56] = Piece.None; }

        // castling rights update on king move
        if (movingPiece == Piece.WK) Castling &= ~(CastlingRights.WhiteKingSide | CastlingRights.WhiteQueenSide);
        if (movingPiece == Piece.BK) Castling &= ~(CastlingRights.BlackKingSide | CastlingRights.BlackQueenSide);

        // castling rights update on rook move
        if (movingPiece == Piece.WR && move.From == 0) Castling &= ~CastlingRights.WhiteQueenSide;
        if (movingPiece == Piece.WR && move.From == 7) Castling &= ~CastlingRights.WhiteKingSide;
        if (movingPiece == Piece.BR && move.From == 56) Castling &= ~CastlingRights.BlackQueenSide;
        if (movingPiece == Piece.BR && move.From == 63) Castling &= ~CastlingRights.BlackKingSide;

        // castling rights update on rook capture
        if (capturedPiece == Piece.WR && move.To == 0) Castling &= ~CastlingRights.WhiteQueenSide;
        if (capturedPiece == Piece.WR && move.To == 7) Castling &= ~CastlingRights.WhiteKingSide;
        if (capturedPiece == Piece.BR && move.To == 56) Castling &= ~CastlingRights.BlackQueenSide;
        if (capturedPiece == Piece.BR && move.To == 63) Castling &= ~CastlingRights.BlackKingSide;

        EnPassantSquare = -1;

        if (isPawn && Math.Abs(move.To - move.From) == 16)
            EnPassantSquare = (move.From + move.To) / 2;

        if (isPawn || capturedPiece != Piece.None) HalfmoveClock = 0;
        else HalfmoveClock++;

        if (SideToMove == Color.Black) FullmoveNumber++;
        SideToMove = SideToMove.Opposite();
    }

    public void UndoMove()
    {
        var undo = _history.Pop();
        var move = undo.Move;
        
        SideToMove = SideToMove.Opposite();
        if (SideToMove == Color.Black) FullmoveNumber--;

        var movingPiece = Squares[move.To];
        bool wasPromotion = move.Promotion != Piece.None;
        bool wasPawnMove = wasPromotion || movingPiece == Piece.WP || movingPiece == Piece.BP;
        bool wasEnPassantCapture = wasPawnMove && move.To == undo.EnPassantSquare && undo.Captured != Piece.None && (move.From % 8 != move.To % 8);

        Squares[move.From] = wasPromotion
            ? (SideToMove == Color.White ? Piece.WP : Piece.BP)
            : movingPiece;

        if (wasEnPassantCapture)
        {
            Squares[move.To] = Piece.None;
            int epCapturedSquare = move.To + (SideToMove == Color.White ? -8 : 8);
            Squares[epCapturedSquare] = undo.Captured;
        }
        else
        {
            Squares[move.To] = undo.Captured;
        }

        // undo castling rook move
        if (Squares[move.From] == Piece.WK && move.From == 4 && move.To == 6) { Squares[7] = Piece.WR; Squares[5] = Piece.None; }
        if (Squares[move.From] == Piece.WK && move.From == 4 && move.To == 2) { Squares[0] = Piece.WR; Squares[3] = Piece.None; }
        if (Squares[move.From] == Piece.BK && move.From == 60 && move.To == 62) { Squares[63] = Piece.BR; Squares[61] = Piece.None; }
        if (Squares[move.From] == Piece.BK && move.From == 60 && move.To == 58) { Squares[56] = Piece.BR; Squares[59] = Piece.None; }

        EnPassantSquare = undo.EnPassantSquare;
        Castling = undo.CastlingRights;
        HalfmoveClock = undo.HalfMoveClock;
        FullmoveNumber = undo.FullMoveNumber;
    }

    public Position Clone()
    {
        return new Position
        {
            // Belangrijk: Maak een NIEUWE array en kopieer de inhoud
            // Gebruik Array.Copy of .ToArray() voor snelheid
            Squares = (Piece[])this.Squares.Clone(),
            
            // Value types (int, enum, ulong) worden automatisch gekopieerd
            SideToMove = this.SideToMove,
            Castling = this.Castling,
            EnPassantSquare = this.EnPassantSquare,
            ZobristKey = this.ZobristKey
        };
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
