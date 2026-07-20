namespace RestaurantApi.Tests;

internal sealed class HorlogeFigee : TimeProvider
{
    private readonly DateTimeOffset _instant;

    public HorlogeFigee(int heure, int minute = 0)
        => _instant = new DateTimeOffset(2026, 1, 1, heure, minute, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => _instant;

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}
