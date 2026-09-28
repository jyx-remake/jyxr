using Game.Application;

namespace Game.Tests;

public sealed class DeferredPersistenceTests
{
    [Fact]
    public void BurstWritesLatestSnapshotOnlyOnce_AndExplicitFlushCancelsPendingWrite()
    {
        var writes = new List<string>();
        var clock = new ManualClock();
        var persistence = new DeferredPersistence<string>("initial", writes.Add, clock);
        persistence.Update("first");
        clock.Advance(0.5);
        persistence.Update("latest");
        persistence.FlushIfDue();
        Assert.Empty(writes);
        persistence.Flush();
        clock.Advance(2);
        persistence.FlushIfDue();
        persistence.Flush();
        Assert.Equal(new[] { "latest" }, writes);
    }

    [Fact]
    public void ContinuousChangesHaveBoundedDelay_AndRevertingNeedsNoWrite()
    {
        var writes = new List<int>();
        var clock = new ManualClock();
        var persistence = new DeferredPersistence<int>(0, writes.Add, clock);
        persistence.Update(1);
        clock.Advance(0.6);
        persistence.Update(2);
        clock.Advance(0.4);
        persistence.FlushIfDue();
        Assert.Equal(new[] { 2 }, writes);
        persistence.Update(3);
        persistence.Update(2);
        clock.Advance(2);
        persistence.FlushIfDue();
        Assert.Single(writes);
        Assert.False(persistence.IsDirty);
    }

    [Fact]
    public void FailedWriteRetainsLatestSnapshot_AndWaitsBeforeRetry()
    {
        var clock = new ManualClock();
        var attempts = 0;
        var writes = new List<string>();
        var persistence = new DeferredPersistence<string>("initial", value =>
        {
            if (++attempts == 1) throw new IOException("unavailable");
            writes.Add(value);
        }, clock);
        persistence.Update("first");
        clock.Advance(1);
        Assert.Throws<IOException>(persistence.FlushIfDue);
        Assert.True(persistence.IsDirty);
        persistence.Update("latest");
        persistence.FlushIfDue();
        Assert.Equal(1, attempts);
        clock.Advance(1);
        persistence.FlushIfDue();
        Assert.Equal(new[] { "latest" }, writes);
        Assert.False(persistence.IsDirty);
    }

    private sealed class ManualClock : TimeProvider
    {
        private long _timestamp;
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => _timestamp;
        public void Advance(double seconds) => _timestamp += TimeSpan.FromSeconds(seconds).Ticks;
    }
}
