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

        position.ZobristKey = Zobrist.ComputeHash(position);

        return position;
    }

    public void MakeMove(Move move)
    {
        if ((uint)move.From >= 64 || (uint)move.To >= 64)
            throw new ArgumentOutOfRangeException(nameof(move), $"Move squares must be within 0..63, got {move.From}->{move.To}.");

        var movingPiece = Squares[move.From];
        if (movingPiece == Piece.None)
            throw new InvalidOperationException($"Cannot make move {move}: source square {move.From} is empty.");

        var capturedPiece = Squares[move.To];

        bool isPawn = movingPiece == Piece.WP || movingPiece == Piece.BP;

        bool isEnPassantCapture =
            isPawn &&
            move.To == EnPassantSquare &&
            capturedPiece == Piece.None &&
            (move.From % 8 != move.To % 8);

        int epCapturedSquare = -1;

        if (isEnPassantCapture)
        {
            epCapturedSquare = move.To + (movingPiece == Piece.WP ? -8 : 8);
            if ((uint)epCapturedSquare >= 64)
                throw new InvalidOperationException($"Invalid en passant capture square for move {move}: {epCapturedSquare}.");
            capturedPiece = Squares[epCapturedSquare];
        }

        _history.Push(new UndoState(
            move,
            capturedPiece,
            EnPassantSquare,
            Castling,
            HalfmoveClock,
            FullmoveNumber,
            ZobristKey
        ));

        // Remove piece from origin
        ZobristKey ^= Zobrist.PieceKeys[(int)movingPiece - 1, move.From];

        // Remove captured piece
        if (capturedPiece != Piece.None)
        {
            int captureSquare = isEnPassantCapture ? epCapturedSquare : move.To;
            ZobristKey ^= Zobrist.PieceKeys[(int)capturedPiece - 1, captureSquare];
        }

        // Remove old en passant hash
        if (EnPassantSquare != -1)
            ZobristKey ^= Zobrist.EnPassantKeys[EnPassantSquare];

        // Remove castling hash
        ZobristKey ^= Zobrist.CastlingKeys[(int)Castling];

        // Move piece
        Squares[move.From] = Piece.None;
        var placedPiece = move.Promotion != Piece.None ? move.Promotion : movingPiece;
        Squares[move.To] = placedPiece;

        // Add moved piece
        ZobristKey ^= Zobrist.PieceKeys[(int)placedPiece - 1, move.To];

        // En passant capture
        if (isEnPassantCapture)
            Squares[epCapturedSquare] = Piece.None;

        // Castling rook moves
        if (movingPiece == Piece.WK && move.From == 4 && move.To == 6)
        {
            Squares[5] = Piece.WR;
            Squares[7] = Piece.None;

            ZobristKey ^= Zobrist.PieceKeys[(int)Piece.WR - 1, 7];
            ZobristKey ^= Zobrist.PieceKeys[(int)Piece.WR - 1, 5];
        }

        if (movingPiece == Piece.WK && move.From == 4 && move.To == 2)
        {
            Squares[3] = Piece.WR;
            Squares[0] = Piece.None;

            ZobristKey ^= Zobrist.PieceKeys[(int)Piece.WR - 1, 0];
            ZobristKey ^= Zobrist.PieceKeys[(int)Piece.WR - 1, 3];
        }

        if (movingPiece == Piece.BK && move.From == 60 && move.To == 62)
        {
            Squares[61] = Piece.BR;
            Squares[63] = Piece.None;

            ZobristKey ^= Zobrist.PieceKeys[(int)Piece.BR - 1, 63];
            ZobristKey ^= Zobrist.PieceKeys[(int)Piece.BR - 1, 61];
        }

        if (movingPiece == Piece.BK && move.From == 60 && move.To == 58)
        {
            Squares[59] = Piece.BR;
            Squares[56] = Piece.None;

            ZobristKey ^= Zobrist.PieceKeys[(int)Piece.BR - 1, 56];
            ZobristKey ^= Zobrist.PieceKeys[(int)Piece.BR - 1, 59];
        }

        // Update castling rights
        if (movingPiece == Piece.WK)
            Castling &= ~(CastlingRights.WhiteKingSide | CastlingRights.WhiteQueenSide);

        if (movingPiece == Piece.BK)
            Castling &= ~(CastlingRights.BlackKingSide | CastlingRights.BlackQueenSide);

        if (movingPiece == Piece.WR && move.From == 0)
            Castling &= ~CastlingRights.WhiteQueenSide;

        if (movingPiece == Piece.WR && move.From == 7)
            Castling &= ~CastlingRights.WhiteKingSide;

        if (movingPiece == Piece.BR && move.From == 56)
            Castling &= ~CastlingRights.BlackQueenSide;

        if (movingPiece == Piece.BR && move.From == 63)
            Castling &= ~CastlingRights.BlackKingSide;

        if (capturedPiece == Piece.WR && move.To == 0)
            Castling &= ~CastlingRights.WhiteQueenSide;

        if (capturedPiece == Piece.WR && move.To == 7)
            Castling &= ~CastlingRights.WhiteKingSide;

        if (capturedPiece == Piece.BR && move.To == 56)
            Castling &= ~CastlingRights.BlackQueenSide;

        if (capturedPiece == Piece.BR && move.To == 63)
            Castling &= ~CastlingRights.BlackKingSide;

        // Add new castling hash
        ZobristKey ^= Zobrist.CastlingKeys[(int)Castling];

        EnPassantSquare = -1;

        if (isPawn && Math.Abs(move.To - move.From) == 16)
            EnPassantSquare = (move.From + move.To) / 2;

        if (EnPassantSquare != -1)
            ZobristKey ^= Zobrist.EnPassantKeys[EnPassantSquare];

        if (isPawn || capturedPiece != Piece.None)
            HalfmoveClock = 0;
        else
            HalfmoveClock++;

        if (SideToMove == Color.Black)
            FullmoveNumber++;

        SideToMove = SideToMove.Opposite();

        ZobristKey ^= Zobrist.SideKey;
    }

    public void UndoMove()
    {
        var undo = _history.Pop();
        var move = undo.Move;

        SideToMove = SideToMove.Opposite();

        if (SideToMove == Color.Black)
            FullmoveNumber--;

        var movingPiece = Squares[move.To];

        bool wasPromotion = move.Promotion != Piece.None;
        bool wasEnPassantCapture =
            (SideToMove == Color.White ? undo.Captured == Piece.BP : undo.Captured == Piece.WP) &&
            move.To == undo.EnPassantSquare &&
            move.From % 8 != move.To % 8;

        Squares[move.From] = wasPromotion
            ? (SideToMove == Color.White ? Piece.WP : Piece.BP)
            : movingPiece;

        Squares[move.To] = Piece.None;

        if (wasEnPassantCapture)
        {
            int capturedSquare = move.To + (SideToMove == Color.White ? -8 : 8);
            Squares[capturedSquare] = undo.Captured;
        }
        else
        {
            Squares[move.To] = undo.Captured;
        }

        // Undo castling rook move
        if (Squares[move.From] == Piece.WK && move.From == 4 && move.To == 6)
        {
            Squares[7] = Piece.WR;
            Squares[5] = Piece.None;
        }

        if (Squares[move.From] == Piece.WK && move.From == 4 && move.To == 2)
        {
            Squares[0] = Piece.WR;
            Squares[3] = Piece.None;
        }

        if (Squares[move.From] == Piece.BK && move.From == 60 && move.To == 62)
        {
            Squares[63] = Piece.BR;
            Squares[61] = Piece.None;
        }

        if (Squares[move.From] == Piece.BK && move.From == 60 && move.To == 58)
        {
            Squares[56] = Piece.BR;
            Squares[59] = Piece.None;
        }

        EnPassantSquare = undo.EnPassantSquare;
        Castling = undo.CastlingRights;
        HalfmoveClock = undo.HalfMoveClock;
        FullmoveNumber = undo.FullMoveNumber;

        ZobristKey = undo.ZobristKey;
    }
    
    // ============================================================
// ADD THESE THREE METHODS TO YOUR EXISTING Position CLASS
// ============================================================

// Place these alongside MakeMove / UndoMove in Position.cs.

/// <summary>
/// Returns true when the side to move has at least one non-pawn, non-king piece.
/// Used to guard null-move pruning against zugzwang in KP-vs-K endings.
/// </summary>
public bool HasNonPawnMaterial()
{
    for (int sq = 0; sq < 64; sq++)
    {
        var p = Squares[sq];
        if (p == Piece.None) continue;

        if (SideToMove == Color.White)
        {
            if (p == Piece.WN || p == Piece.WB || p == Piece.WR || p == Piece.WQ)
                return true;
        }
        else
        {
            if (p == Piece.BN || p == Piece.BB || p == Piece.BR || p == Piece.BQ)
                return true;
        }
    }
    return false;
}

/// <summary>
/// Makes a null move: passes the turn without moving a piece.
/// Updates the Zobrist key and clears the en-passant square.
/// </summary>
public void MakeNullMove()
{
    // Push a state so UndoNullMove can restore everything.
    _history.Push(new UndoState(
        default,        // no real move
        Piece.None,     // no captured piece
        EnPassantSquare,
        Castling,
        HalfmoveClock,
        FullmoveNumber,
        ZobristKey
    ));

    // Remove en-passant from hash before clearing it.
    if (EnPassantSquare != -1)
        ZobristKey ^= Zobrist.EnPassantKeys[EnPassantSquare];

    EnPassantSquare = -1;
    HalfmoveClock++;

    if (SideToMove == Color.Black) FullmoveNumber++;

    SideToMove  =  SideToMove.Opposite();
    ZobristKey  ^= Zobrist.SideKey;
}

/// <summary>Undoes a null move made by MakeNullMove.</summary>
public void UndoNullMove()
{
    var state = _history.Pop();

    SideToMove = SideToMove.Opposite();

    if (SideToMove == Color.Black) FullmoveNumber--;

    EnPassantSquare = state.EnPassantSquare;
    HalfmoveClock   = state.HalfMoveClock;
    FullmoveNumber   = state.FullMoveNumber;
    ZobristKey       = state.ZobristKey;
    // Castling rights are not affected by a null move – no need to restore.
}

    public bool IsThreefoldRepetition()
    {
        // Current position counts as one occurrence; we need at least two matching prior states.
        return CountPreviousOccurrencesOfCurrentKey(minimumMatches: 2) >= 2;
    }

    private int CountPreviousOccurrencesOfCurrentKey(int minimumMatches)
    {
        if (_history.Count == 0 || HalfmoveClock < 4)
            return 0;

        int maxPliesToScan = Math.Min(HalfmoveClock, _history.Count);
        int scanned = 0;
        int matches = 0;

        foreach (var undo in _history)
        {
            if (scanned++ >= maxPliesToScan)
                break;

            if (undo.ZobristKey != ZobristKey)
                continue;

            matches++;
            if (matches >= minimumMatches)
                break;
        }

        return matches;
    }
    
    public Position Clone()
    {
        var clone = new Position
        {
            Squares = (Piece[])Squares.Clone(),
            SideToMove = SideToMove,
            Castling = Castling,
            EnPassantSquare = EnPassantSquare,
            ZobristKey = ZobristKey,
            HalfmoveClock = HalfmoveClock,
            FullmoveNumber = FullmoveNumber,
        };

        // Preserve stack order (oldest -> newest push) so top-of-stack matches original.
        var history = _history.ToArray();
        for (int i = history.Length - 1; i >= 0; i--)
        {
            clone._history.Push(history[i]);
        }

        return clone;
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
