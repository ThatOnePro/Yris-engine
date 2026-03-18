using Engine.Core.Board;

namespace Engine.Core.Moves;

public readonly record struct Move(int From, int To, Piece Promotion = Piece.None);

