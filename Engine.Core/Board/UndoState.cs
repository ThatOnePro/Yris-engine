using Engine.Core.Moves;

namespace Engine.Core.Board;

public readonly record struct UndoState(Move Move, Piece Captured, int EnPassantSquare, CastlingRights CastlingRights, int HalfMoveClock, int FullMoveNumber, ulong ZobristKey);