namespace NarrativeEngine.Api.Infrastructure;

public static class CacheKeys
{
    public static string EmotionChapter(long chapterId) => $"emotion:chapter:{chapterId}";
    public static string MusicBgm(string tag) => $"music:bgm:{tag}";
    public static string ContextSummary(long chapterId) => $"context:summary:{chapterId}";
    public static string LockEmotion(long chapterId) => $"lock:emotion:{chapterId}";
}
