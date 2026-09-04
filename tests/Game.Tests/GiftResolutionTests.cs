using Game.Application;
using Game.Core.Definitions;
using Game.Core.Model;

namespace Game.Tests;

public sealed class GiftResolutionTests
{
    private static NormalItemDefinition CreateItem(string id) => new()
    {
        Id = id,
        Name = id,
        Type = ItemType.Utility,
        ConsumeOnUse = false,
    };

    [Fact]
    public void ResolveGiftIndex_MatchesIdOrNameOneBased()
    {
        var item = CreateItem("xiaohuan_dan");

        Assert.Equal(1, GiftResolutionService.ResolveGiftIndex(item, ["xiaohuan_dan", "other"]));
        Assert.Equal(2, GiftResolutionService.ResolveGiftIndex(item, ["other", "xiaohuan_dan"]));

        var named = new NormalItemDefinition
        {
            Id = "mask_id",
            Name = "雄霸面具",
            Type = ItemType.Utility,
            ConsumeOnUse = false,
        };
        Assert.Equal(2, GiftResolutionService.ResolveGiftIndex(named, ["other", "雄霸面具"]));
    }

    [Fact]
    public void ResolveGiftIndex_MismatchCancelOrEmptyYieldsZero()
    {
        var item = CreateItem("xiaohuan_dan");

        Assert.Equal(0, GiftResolutionService.ResolveGiftIndex(item, ["other"]));
        Assert.Equal(0, GiftResolutionService.ResolveGiftIndex(null, ["xiaohuan_dan"]));
        Assert.Equal(0, GiftResolutionService.ResolveGiftIndex(item, []));
    }
}
