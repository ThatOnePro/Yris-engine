namespace Engine.Core.Notation;

public static class Square
{
    public static string ToString(int square)
    {
        int file = square % 8;
        int rank = square / 8;

        char fileChar = (char)('a' + file);
        char rankChar = (char)('1' + rank);

        return $"{fileChar}{rankChar}";
    }

    public static int FromString(string s)
    {
        int file = s[0] - 'a';
        int rank = s[1] - '1';

        return rank * 8 + file;
    }
}