using Game.Core.Model;

namespace Game.Tests;

public sealed class StatCatalogTests
{
    [Fact]
    public void Parse_SupportsJsonCodeAndChineseName()
    {
        Assert.Equal(StatType.Bili, StatCatalog.Parse("bili"));
        Assert.Equal(StatType.Bili, StatCatalog.Parse("臂力"));
        Assert.Equal(StatType.MaxHp, StatCatalog.Parse("max_hp"));
        Assert.Equal(StatType.MaxHp, StatCatalog.Parse("气血上限"));
    }

    [Fact]
    public void Metadata_ExposesStableCodeNameAndTenDimensionLists()
    {
        Assert.Equal("bili", StatCatalog.GetCode(StatType.Bili));
        Assert.Equal("臂力", StatCatalog.GetDisplayNameCn(StatType.Bili));
        Assert.Equal(
            [
                StatType.Quanzhang,
                StatType.Jianfa,
                StatType.Daofa,
                StatType.Qimen,
                StatType.Bili,
                StatType.Shenfa,
                StatType.Wuxing,
                StatType.Fuyuan,
                StatType.Gengu,
                StatType.Dingli,
            ],
            StatCatalog.TenDimensionStats);
        Assert.Equal(
            [
                StatType.MaxHp,
                StatType.MaxMp,
                StatType.Quanzhang,
                StatType.Jianfa,
                StatType.Daofa,
                StatType.Qimen,
                StatType.Bili,
                StatType.Shenfa,
                StatType.Wuxing,
                StatType.Fuyuan,
                StatType.Gengu,
                StatType.Dingli,
            ],
            StatCatalog.MinusMaxPointsStats);
    }
}
