namespace Engine.Core.Board;

public enum Color : byte
{
    White, Black   
}

public static class ColorExtensions
{
    public static Color Opposite(this Color color) =>
        color == Color.White ? Color.Black : Color.White;
}