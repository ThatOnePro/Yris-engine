namespace Engine.Core.Search;

public static class TimeManager
{
    private static long _startTimeMs;
    private static long _allocatedTimeMs;
    private static int _nodeCheckInterval = 2048;
    private static int _nodesSinceLastCheck;
    public static bool TimeExpired { get; private set; }
    public static long TotalNodes { get; private set; }

    public static void StartSearch(long timeMs)
    {
        _startTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        _allocatedTimeMs = timeMs;
        TimeExpired = false;
        _nodesSinceLastCheck = 0;
        TotalNodes = 0;
    }

    public static void Stop()
    {
        TimeExpired = true;
    }

    public static void RecordNodes(int count)
    {
        _nodesSinceLastCheck += count;
        TotalNodes += count;
        if (_nodesSinceLastCheck >= _nodeCheckInterval)
        {
            CheckTime();
            _nodesSinceLastCheck = 0;
        }
    }

    public static void CheckTime()
    {
        long elapsedMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - _startTimeMs;
        if (elapsedMs >= _allocatedTimeMs)
        {
            TimeExpired = true;
        }
    }

    public static long GetElapsedMs()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - _startTimeMs;
    }
}


