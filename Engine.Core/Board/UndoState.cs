namespace Engine.Core.Board;

public readonly record struct UndoState(Piece Captured, int EnPassantSquare, CastlingRights CastlingRights, int HalfMoveClock, int FullMoveNumber);