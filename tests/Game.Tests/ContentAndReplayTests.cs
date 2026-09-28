using System.Text.Json;
using Game.Content.Loading;
using Game.Core.Affix;
using Game.Core.Battle;
using Game.Core.Definitions;
using Game.Core.Definitions.Skills;
using Game.Core.Model;
using Game.Core.Story;
using Game.Core.Serialization;
using GrantModelAffix = Game.Core.Affix.GrantModelAffix;
using GrantTalentAffix = Game.Core.Affix.GrantTalentAffix;

namespace Game.Tests;

public sealed class ContentLoadingTests
{
    [Fact]
    public void JsonLoader_IndexesLegendSkillsByStartSkillInContentOrder()
    {
        var first = new LegendSkillDefinition("first", "first", "strike", 0.5d, [], []);
        var unrelated = first with { Id = "other", StartSkill = "other" };
        var second = first with { Id = "second" };
        var repository = new JsonContentLoader().LoadFromPackage(new ContentPackage
        {
            ExternalSkills =
            [
                TestContentFactory.CreateExternalSkill("strike"),
                TestContentFactory.CreateExternalSkill("other"),
            ],
            LegendSkills = [first, unrelated, second],
        });

        Assert.Equal([first, second], repository.GetLegendSkillsFor("strike"));
        Assert.Same(first, repository.GetLegendSkillsFor("strike")[0]);
        Assert.Equal([unrelated], repository.GetLegendSkillsFor("other"));
        Assert.Empty(repository.GetLegendSkillsFor("missing"));
        Assert.Empty(repository.GetLegendSkillsFor("STRIKE"));
        Assert.Equal([first, unrelated, second], repository.GetLegendSkills());
    }

    private static string SampleContentDirectoryPath =>
        Path.Combine(AppContext.BaseDirectory, "SampleData", "sample-content");

    [Fact]
    public void JsonLoader_LoadsSamplePackage()
    {
        var loader = new JsonContentLoader();
        var repository = loader.LoadFromDirectory(SampleContentDirectoryPath);

        Assert.NotNull(repository.GetBattle("sample_battle"));
        Assert.NotNull(repository.GetExternalSkill("fireball"));
        Assert.NotNull(repository.GetBuff("burning"));
        Assert.NotNull(repository.GetTalent("bloodlust"));
        Assert.NotNull(repository.GetGameTip("sample_tip"));
        Assert.NotNull(repository.GetGrowTemplate("sample_growth"));
        var map = repository.GetMap("sample_map");
        Assert.NotNull(map);
        Assert.Equal(MapKind.Small, map.Kind);
        Assert.Equal(new MapPosition(1, 2), Assert.Single(map.Locations).Position);
        Assert.NotNull(repository.GetResource("sample_music"));
        Assert.NotNull(repository.GetSect("sample_sect"));
        Assert.NotNull(repository.GetShop("sample_shop"));
        Assert.NotNull(repository.GetTower("sample_tower"));
    }

    [Fact]
    public void JsonLoader_LoadsResourceTags()
    {
        const string json =
            """
            {"id":"city","value":"ui/town/city","tags":["map-marker-overflow"]}
            """;
        var resource = JsonSerializer.Deserialize<ResourceDefinition>(json, GameJson.Default);

        Assert.NotNull(resource);
        Assert.Equal(["map-marker-overflow"], resource.Tags);

        var repository = new JsonContentLoader().LoadFromPackage(new ContentPackage { Resources = [resource] });
        Assert.Equal(["map-marker-overflow"], repository.GetResource("city").Tags);
    }

    public static TheoryData<IReadOnlyList<int>?> InvalidDetoxifyValues
    {
        get
        {
            var data = new TheoryData<IReadOnlyList<int>?>();
            data.Add(null);
            data.Add([]);
            data.Add([1]);
            data.Add([1, 2, 3]);
            data.Add([-1, 2]);
            data.Add([1, -2]);
            data.Add([0, 0]);
            return data;
        }
    }

    [Theory]
    [MemberData(nameof(InvalidDetoxifyValues))]
    public void JsonLoader_RejectsInvalidDetoxifyValues(IReadOnlyList<int>? values)
    {
        var package = new ContentPackage
        {
            Items =
            [
                new NormalItemDefinition
                {
                    Id = "detoxifier",
                    Name = "detoxifier",
                    Type = ItemType.Consumable,
                    ConsumeOnUse = true,
                    UseEffects = [new DetoxifyItemUseEffectDefinition(values)],
                },
            ],
        };

        Assert.Throws<InvalidOperationException>(() => new JsonContentLoader().LoadFromPackage(package));
    }

    [Fact]
    public void JsonLoader_DeserializesMapLocationNoEventPresentation()
    {
        const string hiddenJson = """{"id":"hidden","hideWhenNoEvent":true}""";
        const string customJson = """{"id":"custom","noEventImage":"icon"}""";

        var hidden = JsonSerializer.Deserialize<MapLocationDefinition>(hiddenJson, GameJson.Default);
        var custom = JsonSerializer.Deserialize<MapLocationDefinition>(customJson, GameJson.Default);

        Assert.NotNull(hidden);
        Assert.True(hidden.HideWhenNoEvent);
        Assert.Null(hidden.NoEventImage);
        Assert.NotNull(custom);
        Assert.False(custom.HideWhenNoEvent);
        Assert.Equal("icon", custom.NoEventImage);
    }

    [Fact]
    public void JsonLoader_DefaultsMapLocationNoEventPresentation()
    {
        var location = JsonSerializer.Deserialize<MapLocationDefinition>(
            """{"id":"location"}""",
            GameJson.Default);

        Assert.NotNull(location);
        Assert.False(location.HideWhenNoEvent);
        Assert.Null(location.NoEventImage);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void JsonLoader_RejectsEmptyNoEventImage(string image)
    {
        var package = CreateMapPackage(new MapLocationDefinition
        {
            Id = "location",
            NoEventImage = image,
        });

        Assert.Throws<InvalidOperationException>(() => new JsonContentLoader().LoadFromPackage(package));
    }

    [Fact]
    public void JsonLoader_RejectsNoEventImageWhenLocationIsHidden()
    {
        var package = CreateMapPackage(new MapLocationDefinition
        {
            Id = "location",
            HideWhenNoEvent = true,
            NoEventImage = "icon",
        });

        Assert.Throws<InvalidOperationException>(() => new JsonContentLoader().LoadFromPackage(package));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void JsonLoader_RejectsEmptyMapEventId(string eventId)
    {
        var mapEvent = JsonSerializer.Deserialize<MapEventDefinition>(
            $$"""{"id":"{{eventId}}","action":"map('world')"}""",
            GameJson.Default)!;
        var package = CreateMapPackage(new MapLocationDefinition
        {
            Id = "location",
            Events = [mapEvent],
        });

        Assert.Throws<InvalidOperationException>(() => new JsonContentLoader().LoadFromPackage(package));
    }

    [Fact]
    public void JsonLoader_RejectsDuplicateMapEventIdsWithinLocation()
    {
        var first = JsonSerializer.Deserialize<MapEventDefinition>(
            """{"id":"world-location-event","action":"map('world')"}""",
            GameJson.Default)!;
        var second = JsonSerializer.Deserialize<MapEventDefinition>(
            """{"id":"world-location-event","action":"map('world')"}""",
            GameJson.Default)!;
        var package = CreateMapPackage(new MapLocationDefinition
        {
            Id = "location",
            Events = [first, second],
        });

        Assert.Throws<InvalidOperationException>(() => new JsonContentLoader().LoadFromPackage(package));
    }

    private static ContentPackage CreateMapPackage(MapLocationDefinition location) =>
        new()
        {
            Maps =
            [
                new MapDefinition
                {
                    Id = "world",
                    Name = "World",
                    Kind = MapKind.Large,
                    TravelSpeed = 10d,
                    DefaultLocation = location.Id,
                    Locations = [location with { Position = new MapPosition(10, 20) }],
                },
            ],
        };

    [Theory]
    [InlineData(0d)]
    [InlineData(-1d)]
    public void JsonLoader_RejectsLargeMapWithoutPositiveTravelSpeed(double travelSpeed)
    {
        var package = CreateSingleMapPackage(new MapDefinition
        {
            Id = "world",
            Name = "World",
            Kind = MapKind.Large,
            TravelSpeed = travelSpeed,
        });

        Assert.Throws<InvalidOperationException>(() => new JsonContentLoader().LoadFromPackage(package));
    }

    [Fact]
    public void JsonLoader_RejectsNonFiniteLargeMapTravelSpeed()
    {
        var package = CreateSingleMapPackage(new MapDefinition
        {
            Id = "world",
            Name = "World",
            Kind = MapKind.Large,
            TravelSpeed = double.NaN,
        });

        Assert.Throws<InvalidOperationException>(() => new JsonContentLoader().LoadFromPackage(package));
    }

    [Fact]
    public void JsonLoader_RejectsTravelSpeedOnSmallMap()
    {
        var package = CreateSingleMapPackage(new MapDefinition
        {
            Id = "town",
            Name = "Town",
            Kind = MapKind.Small,
            TravelSpeed = 22d,
        });

        Assert.Throws<InvalidOperationException>(() => new JsonContentLoader().LoadFromPackage(package));
    }

    [Fact]
    public void JsonLoader_RejectsLargeMapLocationWithoutPosition()
    {
        var package = CreateSingleMapPackage(new MapDefinition
        {
            Id = "world",
            Name = "World",
            Kind = MapKind.Large,
            TravelSpeed = 22d,
            DefaultLocation = "town",
            Locations = [new MapLocationDefinition { Id = "town" }],
        });

        Assert.Throws<InvalidOperationException>(() => new JsonContentLoader().LoadFromPackage(package));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(1921, 0)]
    [InlineData(0, -1)]
    [InlineData(0, 1081)]
    public void JsonLoader_RejectsLargeMapLocationOutsideLogicalSpace(int x, int y)
    {
        var package = CreateSingleMapPackage(new MapDefinition
        {
            Id = "world",
            Name = "World",
            Kind = MapKind.Large,
            TravelSpeed = 22d,
            DefaultLocation = "town",
            Locations = [new MapLocationDefinition { Id = "town", Position = new MapPosition(x, y) }],
        });

        Assert.Throws<InvalidOperationException>(() => new JsonContentLoader().LoadFromPackage(package));
    }

    private static ContentPackage CreateSingleMapPackage(MapDefinition map) =>
        new() { Maps = [map] };

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("missing")]
    public void JsonLoader_RejectsLargeMapWithoutExistingDefaultLocation(string? defaultLocation)
    {
        var package = CreateSingleMapPackage(new MapDefinition
        {
            Id = "world",
            Name = "World",
            Kind = MapKind.Large,
            TravelSpeed = 22d,
            DefaultLocation = defaultLocation,
            Locations =
            [
                new MapLocationDefinition
                {
                    Id = "start",
                    Position = new MapPosition(10, 20),
                },
            ],
        });

        Assert.Throws<InvalidOperationException>(() => new JsonContentLoader().LoadFromPackage(package));
    }

    [Fact]
    public void JsonLoader_AllowsTheSameMapEventIdInDifferentLocations()
    {
        var first = JsonSerializer.Deserialize<MapEventDefinition>(
            """{"id":"intro","action":"map('world')"}""",
            GameJson.Default)!;
        var second = JsonSerializer.Deserialize<MapEventDefinition>(
            """{"id":"intro","action":"map('world')"}""",
            GameJson.Default)!;
        var package = new ContentPackage
        {
            Maps =
            [
                new MapDefinition
                {
                    Id = "world",
                    Name = "World",
                    Kind = MapKind.Large,
                    TravelSpeed = 10d,
                    DefaultLocation = "village",
                    Locations =
                    [
                        new MapLocationDefinition
                        {
                            Id = "village",
                            Position = new MapPosition(10, 20),
                            Events = [first],
                        },
                        new MapLocationDefinition
                        {
                            Id = "gate",
                            Position = new MapPosition(30, 40),
                            Events = [second],
                        },
                    ],
                },
            ],
        };

        Assert.NotNull(new JsonContentLoader().LoadFromPackage(package));
    }

    [Fact]
    public void JsonLoader_RejectsDefaultLocationOnSmallMap()
    {
        var package = CreateSingleMapPackage(new MapDefinition
        {
            Id = "town",
            Name = "Town",
            DefaultLocation = "start",
            Locations = [new MapLocationDefinition { Id = "start" }],
        });

        Assert.Throws<InvalidOperationException>(() => new JsonContentLoader().LoadFromPackage(package));
    }

    [Fact]
    public void JsonLoader_RejectsDuplicateMapLocationIds()
    {
        var package = CreateSingleMapPackage(new MapDefinition
        {
            Id = "town",
            Name = "Town",
            Locations =
            [
                new MapLocationDefinition { Id = "shop" },
                new MapLocationDefinition { Id = "shop" },
            ],
        });

        Assert.Throws<InvalidOperationException>(() => new JsonContentLoader().LoadFromPackage(package));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void JsonLoader_RejectsEmptyResourceTags(string tag)
    {
        var package = new ContentPackage
        {
            Resources = [new ResourceDefinition { Id = "resource", Tags = [tag] }],
        };

        Assert.Throws<InvalidOperationException>(() => new JsonContentLoader().LoadFromPackage(package));
    }

    [Fact]
    public void JsonLoader_RejectsDuplicateResourceTags()
    {
        var package = new ContentPackage
        {
            Resources =
            [
                new ResourceDefinition
                {
                    Id = "resource",
                    Tags = ["map-marker-overflow", "map-marker-overflow"],
                },
            ],
        };

        Assert.Throws<InvalidOperationException>(() => new JsonContentLoader().LoadFromPackage(package));
    }

    [Fact]
    public void JsonLoader_RejectsScopedEffectCombinationsWithoutRuntimeSemantics()
    {
        var invalidDefinitions = new ScopedBattleEffectDefinition[]
        {
            new()
            {
                Id = "provider_explicit",
                Scope = new ExplicitUnitsBattleUnitSelectorDefinition(),
            },
            new()
            {
                Id = "group_source_alive",
                Scope = new AllAlliesBattleUnitSelectorDefinition(),
                GrantMode = ScopedBattleEffectGrantMode.PerTeamGroup,
                Activation = BattleEffectActivation.SourceAlive,
            },
            new()
            {
                Id = "provider_member_lifetime",
                Scope = new AllAlliesBattleUnitSelectorDefinition(),
                Lifetime = BattleEffectLifetime.RemoveWhenMemberDefeated,
            },
        };

        foreach (var definition in invalidDefinitions)
        {
            var package = new ContentPackage { ScopedBattleEffects = [definition] };
            Assert.Throws<InvalidOperationException>(() => new JsonContentLoader().LoadFromPackage(package));
        }
    }

    [Fact]
    public void JsonLoader_ResolvesGrantTalentAffix()
    {
        const string json = """
            {
              "characters": [],
              "externalSkills": [],
              "internalSkills": [],
              "legendSkills": [],
              "items": [
                {
                  "category": "equipment",
                  "id": "iron_blade",
                  "name": "Iron Blade",
                  "type": "equipment",
                  "consumeOnUse": false,
                  "slotType": "weapon",
                  "level": 1,
                  "price": 0,
                  "cooldown": 0,
                  "canDrop": true,
                  "requirements": [],
                  "useEffects": [],
                  "affixes": [
                    {
                      "type": "grant_talent",
                      "talentId": "bloodlust"
                    }
                  ]
                }
              ],
              "buffs": [],
              "talents": [
                {
                  "id": "bloodlust",
                  "name": "Bloodlust",
                  "affixes": []
                }
              ]
            }
            """;

        var repository = LoadRepositoryFromJson(json);
        var equipment = repository.GetEquipment("iron_blade");
        var affix = Assert.IsType<GrantTalentAffix>(Assert.Single(equipment.Affixes));

        Assert.Same(repository.GetTalent("bloodlust"), affix.Talent);
    }

    [Fact]
    public void JsonLoader_LoadsEquippedInternalSkillEntry()
    {
        const string json = """
            {
              "characters": [
                {
                  "id": "hero_knight",
                  "name": "Hero Knight",
                  "stats": {},
                  "externalSkills": [],
                  "internalSkills": [
                    {
                      "id": "basic_internal",
                      "level": 3,
                      "equipped": true
                    }
                  ],
                  "talentIds": [],
                  "equipmentIds": [],
                  "specialSkillIds": []
                }
              ],
              "externalSkills": [],
              "internalSkills": [
                {
                  "id": "basic_internal",
                  "name": "Basic Internal",
                  "formSkills": [],
                  "affixes": []
                }
              ],
              "legendSkills": [],
              "items": [],
              "buffs": [],
              "talents": [],
            }
            """;

        var repository = LoadRepositoryFromJson(json);
        var character = repository.GetCharacter("hero_knight");
        var entry = Assert.Single(character.InternalSkills);

        Assert.True(entry.Equipped);
    }

    [Fact]
    public void JsonLoader_PreservesDefinedWuxueSentinel()
    {
        const string json = """
            {
              "characters": [
                {
                  "id": "aqing",
                  "name": "Aqing",
                  "stats": {
                    "wuxue": -1,
                    "bili": 12
                  },
                  "externalSkills": [],
                  "internalSkills": [],
                  "talentIds": [],
                  "equipmentIds": [],
                  "specialSkillIds": []
                }
              ],
              "externalSkills": [],
              "internalSkills": [],
              "legendSkills": [],
              "items": [],
              "buffs": [],
              "talents": [],
            }
            """;

        var repository = LoadRepositoryFromJson(json);
        var character = repository.GetCharacter("aqing");

        Assert.Equal(-1, character.Stats[StatType.Wuxue]);
        Assert.Equal(12, character.Stats[StatType.Bili]);
    }

    [Fact]
    public void JsonLoader_ResolvesWrappedSkillAffix()
    {
        const string json = """
            {
              "characters": [],
              "externalSkills": [
                {
                  "id": "focus_strike",
                  "name": "Focus Strike",
                  "affixes": [
                    {
                      "minimumLevel": 3,
                      "effect": {
                        "type": "grant_talent",
                        "talentId": "battle_focus"
                      }
                    }
                  ]
                }
              ],
              "internalSkills": [],
              "legendSkills": [],
              "items": [],
              "buffs": [],
              "talents": [
                {
                  "id": "battle_focus",
                  "name": "Battle Focus",
                  "affixes": []
                }
              ],
            }
            """;

        var repository = LoadRepositoryFromJson(json);
        var skill = repository.GetExternalSkill("focus_strike");
        var skillAffix = Assert.Single(skill.Affixes);
        var affix = Assert.IsType<GrantTalentAffix>(skillAffix.Effect);

        Assert.Equal(3, skillAffix.MinimumLevel);
        Assert.Same(repository.GetTalent("battle_focus"), affix.Talent);
    }

    [Fact]
    public void JsonLoader_ResolvesWrappedInternalSkillGrantTalentAffix()
    {
        const string json = """
            {
              "characters": [],
              "externalSkills": [],
              "internalSkills": [
                {
                  "id": "inner_focus",
                  "name": "Inner Focus",
                  "affixes": [
                    {
                      "minimumLevel": 4,
                      "requiresEquippedInternalSkill": true,
                      "effect": {
                        "type": "grant_talent",
                        "talentId": "battle_focus"
                      }
                    }
                  ]
                }
              ],
              "legendSkills": [],
              "items": [],
              "buffs": [],
              "talents": [
                {
                  "id": "battle_focus",
                  "name": "Battle Focus",
                  "affixes": []
                }
              ],
            }
            """;

        var repository = LoadRepositoryFromJson(json);
        var skill = repository.GetInternalSkill("inner_focus");
        var skillAffix = Assert.Single(skill.Affixes);
        var affix = Assert.IsType<GrantTalentAffix>(skillAffix.Effect);

        Assert.Equal(4, skillAffix.MinimumLevel);
        Assert.True(skillAffix.RequiresEquippedInternalSkill);
        Assert.Same(repository.GetTalent("battle_focus"), affix.Talent);
    }

    [Fact]
    public void JsonLoader_LoadsBattleHookHpRatioEffectiveTalentAndSkillWeaponTypeConditions()
    {
        const string json = """
            {
              "characters": [],
              "externalSkills": [],
              "internalSkills": [],
              "legendSkills": [],
              "items": [],
              "buffs": [],
              "talents": [
                {
                  "id": "caotou",
                  "name": "草头百姓",
                  "affixes": []
                },
                {
                  "id": "yishiren",
                  "name": "异世人",
                  "affixes": [
                    {
                      "type": "hook",
                      "timing": "BeforeDamageCalculation",
                      "conditions": [
                        {
                          "type": "context_unit_role",
                          "role": "source"
                        },
                        {
                          "type": "context_unit_hp_ratio",
                          "minExclusive": 0.2,
                          "maxInclusive": 0.5
                        },
                        {
                          "type": "context_unit_effective_talent",
                          "talentIds": [
                            "caotou"
                          ]
                        },
                        {
                          "type": "context_skill_weapon_type",
                          "weaponTypes": [
                            "jianfa"
                          ]
                        }
                      ],
                      "speech": {
                        "speaker": "source",
                        "lines": [
                          "无招胜有招!"
                        ],
                        "chance": 0.1
                      }
                    }
                  ]
                }
              ]
            }
            """;

        var repository = LoadRepositoryFromJson(json);
        var talent = repository.GetTalent("yishiren");
        var hook = Assert.IsType<HookAffix>(Assert.Single(talent.Affixes));

        Assert.Collection(
            hook.Conditions,
            condition => Assert.IsType<ContextUnitRoleBattleHookConditionDefinition>(condition),
            condition =>
            {
                var hpRatio = Assert.IsType<ContextUnitHpRatioBattleHookConditionDefinition>(condition);
                Assert.Equal(0.2d, hpRatio.MinExclusive);
                Assert.Equal(0.5d, hpRatio.MaxInclusive);
            },
            condition =>
            {
                var effectiveTalent = Assert.IsType<ContextUnitEffectiveTalentBattleHookConditionDefinition>(condition);
                Assert.Equal(["caotou"], effectiveTalent.TalentIds);
            },
            condition =>
            {
                var skillWeaponType = Assert.IsType<ContextSkillWeaponTypeBattleHookConditionDefinition>(condition);
                Assert.Equal([WeaponType.Jianfa], skillWeaponType.WeaponTypes);
            });
    }

    [Fact]
    public void JsonLoader_RejectsInvalidSkillAffixConditions()
    {
        const string externalRequiresEquipped = """
            {
              "characters": [],
              "externalSkills": [
                {
                  "id": "focus_strike",
                  "name": "Focus Strike",
                  "affixes": [
                    {
                      "requiresEquippedInternalSkill": true,
                      "effect": {
                        "type": "trait",
                        "traitId": "swift"
                      }
                    }
                  ]
                }
              ],
              "internalSkills": [],
              "legendSkills": [],
              "items": [],
              "buffs": [],
              "talents": [],
            }
            """;
        const string invalidMinimumLevel = """
            {
              "characters": [],
              "externalSkills": [
                {
                  "id": "focus_strike",
                  "name": "Focus Strike",
                  "affixes": [
                    {
                      "minimumLevel": 0,
                      "effect": {
                        "type": "trait",
                        "traitId": "swift"
                      }
                    }
                  ]
                }
              ],
              "internalSkills": [],
              "legendSkills": [],
              "items": [],
              "buffs": [],
              "talents": [],
            }
            """;
        const string missingEffect = """
            {
              "characters": [],
              "externalSkills": [
                {
                  "id": "focus_strike",
                  "name": "Focus Strike",
                  "affixes": [
                    {
                      "minimumLevel": 1
                    }
                  ]
                }
              ],
              "internalSkills": [],
              "legendSkills": [],
              "items": [],
              "buffs": [],
              "talents": [],
            }
            """;

        Assert.Throws<InvalidOperationException>(() => LoadRepositoryFromJson(externalRequiresEquipped));
        Assert.Throws<InvalidOperationException>(() => LoadRepositoryFromJson(invalidMinimumLevel));
        Assert.Throws<InvalidOperationException>(() => LoadRepositoryFromJson(missingEffect));
    }

    [Fact]
    public void JsonLoader_LoadsItemTypes()
    {
        const string json = """
            {
              "characters": [],
              "externalSkills": [],
              "internalSkills": [],
              "legendSkills": [],
              "items": [
                { "category": "normal", "id": "healing_potion", "name": "Healing Potion", "type": "consumable", "consumeOnUse": true, "level": 1, "price": 25 },
                { "category": "equipment", "id": "iron_sword", "name": "Iron Sword", "type": "equipment", "consumeOnUse": false, "slotType": "weapon", "level": 1, "price": 100, "affixes": [] },
                { "category": "equipment", "id": "cloth_armor", "name": "Cloth Armor", "type": "equipment", "consumeOnUse": false, "slotType": "armor", "level": 1, "price": 80, "affixes": [] },
                { "category": "equipment", "id": "jade_ring", "name": "Jade Ring", "type": "equipment", "consumeOnUse": false, "slotType": "accessory", "level": 1, "price": 120, "affixes": [] },
                { "category": "normal", "id": "sword_manual", "name": "Sword Manual", "type": "skill_book", "consumeOnUse": false, "level": 1, "price": 200 },
                { "category": "normal", "id": "sealed_letter", "name": "Sealed Letter", "type": "quest_item", "consumeOnUse": false, "level": 1, "price": 0 },
                { "category": "normal", "id": "healer_scroll", "name": "Healer Scroll", "type": "special_skill_book", "consumeOnUse": true, "level": 1, "price": 300 },
                { "category": "normal", "id": "talent_notes", "name": "Talent Notes", "type": "talent_book", "consumeOnUse": true, "level": 1, "price": 250 },
                { "category": "normal", "id": "strength_pill", "name": "Strength Pill", "type": "booster", "consumeOnUse": true, "level": 1, "price": 150 },
                { "category": "normal", "id": "travel_talisman", "name": "Travel Talisman", "type": "utility", "consumeOnUse": false, "level": 1, "price": 50 }
              ],
              "buffs": [],
              "talents": [],
            }
            """;

        var repository = LoadRepositoryFromJson(json);

        Assert.Equal(ItemType.Consumable, repository.GetItem("healing_potion").Type);
        Assert.Equal(ItemType.Equipment, repository.GetItem("iron_sword").Type);
        Assert.Equal(ItemType.Equipment, repository.GetItem("cloth_armor").Type);
        Assert.Equal(ItemType.Equipment, repository.GetItem("jade_ring").Type);
        Assert.Equal(ItemType.SkillBook, repository.GetItem("sword_manual").Type);
        Assert.Equal(ItemType.QuestItem, repository.GetItem("sealed_letter").Type);
        Assert.Equal(ItemType.SpecialSkillBook, repository.GetItem("healer_scroll").Type);
        Assert.Equal(ItemType.TalentBook, repository.GetItem("talent_notes").Type);
        Assert.Equal(ItemType.Booster, repository.GetItem("strength_pill").Type);
        Assert.Equal(ItemType.Utility, repository.GetItem("travel_talisman").Type);
        Assert.Equal(EquipmentSlotType.Weapon, repository.GetEquipment("iron_sword").SlotType);
        Assert.Equal(EquipmentSlotType.Armor, repository.GetEquipment("cloth_armor").SlotType);
        Assert.Equal(EquipmentSlotType.Accessory, repository.GetEquipment("jade_ring").SlotType);
    }

    [Fact]
    public void JsonLoader_LoadsTypedItemRequirementsAndUseEffects()
    {
        const string json = """
            {
              "characters": [],
              "externalSkills": [
                {
                  "id": "songfeng",
                  "name": "松风剑法",
                  "affixes": []
                }
              ],
              "internalSkills": [],
              "legendSkills": [],
              "specialSkills": [],
              "items": [
                {
                  "category": "normal",
                  "id": "talent_scroll",
                  "name": "Talent Scroll",
                  "type": "talent_book",
                  "consumeOnUse": true,
                  "level": 1,
                  "price": 250,
                  "requirements": [
                    {
                      "type": "talent",
                      "talentId": "battle_focus"
                    },
                    {
                      "type": "stat",
                      "statId": "wuxue",
                      "value": 60
                    }
                  ],
                  "useEffects": [
                    {
                      "type": "add_stats",
                      "values": {
                        "max_hp": 100,
                        "bili": 5
                      }
                    },
                    {
                      "type": "grant_talent",
                      "talentId": "bloodlust"
                    },
                    {
                      "type": "external_skill",
                      "skillId": "songfeng",
                      "level": 1
                    }
                  ]
                }
              ],
              "buffs": [],
              "talents": [
                {
                  "id": "battle_focus",
                  "name": "Battle Focus",
                  "affixes": []
                },
                {
                  "id": "bloodlust",
                  "name": "Bloodlust",
                  "affixes": []
                }
              ]
            }
            """;

        var repository = LoadRepositoryFromJson(json);
        var item = Assert.IsType<NormalItemDefinition>(repository.GetItem("talent_scroll"));

        var talentRequirement = Assert.IsType<TalentItemRequirementDefinition>(item.Requirements[0]);
        var statRequirement = Assert.IsType<StatItemRequirementDefinition>(item.Requirements[1]);
        var addStats = Assert.IsType<AddStatsItemUseEffectDefinition>(item.UseEffects[0]);
        var grantTalent = Assert.IsType<GrantTalentItemUseEffectDefinition>(item.UseEffects[1]);
        var grantSkill = Assert.IsType<GrantExternalSkillItemUseEffectDefinition>(item.UseEffects[2]);

        Assert.Equal("battle_focus", talentRequirement.TalentId);
        Assert.Equal(StatType.Wuxue, statRequirement.StatId);
        Assert.Equal(60, statRequirement.Value);
        Assert.Equal(100, addStats.Values[StatType.MaxHp]);
        Assert.Equal(5, addStats.Values[StatType.Bili]);
        Assert.Equal("bloodlust", grantTalent.TalentId);
        Assert.Equal("songfeng", grantSkill.SkillId);
        Assert.Equal(1, grantSkill.Level);
    }

    [Fact]
    public void JsonLoader_LoadsRunStoryItemEffectAndValidatesReference()
    {
        var directoryPath = CreateContentDirectory(new Dictionary<string, string>
        {
            ["items.json"] = """
                [
                  {
                    "category": "normal",
                    "id": "story_item",
                    "name": "Story Item",
                    "type": "utility",
                    "consumeOnUse": true,
                    "useEffects": [{ "type": "run_story", "storyId": "item_effect" }]
                  }
                ]
                """,
            ["stories/item-effect.story.json"] = """
                {
                  "version": 3,
                  "segments": [{ "name": "item_effect", "steps": [] }]
                }
                """,
        });

        try
        {
            var repository = new JsonContentLoader().LoadFromDirectory(directoryPath);
            var item = repository.GetItem("story_item");
            var effect = Assert.IsType<RunStoryItemUseEffectDefinition>(Assert.Single(item.UseEffects));
            Assert.Equal("item_effect", effect.StoryId);
        }
        finally
        {
            Directory.Delete(directoryPath, recursive: true);
        }
    }

    [Fact]
    public void JsonLoader_RejectsInvalidRunStoryItemEffects()
    {
        var invalidItemDocuments = new[]
        {
            """
            [{
              "category": "normal", "id": "empty", "name": "Empty", "type": "utility",
              "consumeOnUse": true,
              "useEffects": [{ "type": "run_story", "storyId": "" }]
            }]
            """,
            """
            [{
              "category": "normal", "id": "missing", "name": "Missing", "type": "utility",
              "consumeOnUse": true,
              "useEffects": [{ "type": "run_story", "storyId": "missing_story" }]
            }]
            """,
            """
            [{
              "category": "normal", "id": "mixed", "name": "Mixed", "type": "booster",
              "consumeOnUse": true,
              "useEffects": [
                { "type": "run_story", "storyId": "item_effect" },
                { "type": "add_stats", "values": { "max_hp": 10 } }
              ]
            }]
            """,
            """
            [{
              "category": "normal", "id": "battle_item", "name": "Battle Item", "type": "consumable",
              "consumeOnUse": true,
              "useEffects": [{ "type": "run_story", "storyId": "item_effect" }]
            }]
            """,
        };

        foreach (var itemsJson in invalidItemDocuments)
        {
            var directoryPath = CreateContentDirectory(new Dictionary<string, string>
            {
                ["items.json"] = itemsJson,
                ["stories/item-effect.story.json"] = """
                    {
                      "version": 3,
                      "segments": [{ "name": "item_effect", "steps": [] }]
                    }
                    """,
            });

            try
            {
                Assert.Throws<InvalidOperationException>(
                    () => new JsonContentLoader().LoadFromDirectory(directoryPath));
            }
            finally
            {
                Directory.Delete(directoryPath, recursive: true);
            }
        }
    }

    [Fact]
    public void JsonLoader_LoadsCharacterModelAndGrantModelAffixText()
    {
        const string json = """
            {
              "characters": [
                {
                  "id": "hero_knight",
                  "name": "Hero Knight",
                  "stats": {},
                  "externalSkills": [],
                  "internalSkills": [],
                  "talentIds": [],
                  "equipmentIds": [],
                  "portrait": "head/hero_knight",
                  "model": "hero_knight_model"
                }
              ],
              "externalSkills": [],
              "internalSkills": [
                {
                  "id": "shape_shift",
                  "name": "Shape Shift",
                  "affixes": [
                    {
                      "effect": {
                        "type": "grant_model",
                        "modelId": "guoxiang",
                        "priority": 0,
                        "description": "化身为天山童姥"
                      }
                    }
                  ]
                }
              ],
              "legendSkills": [],
              "specialSkills": [],
              "items": [],
              "buffs": [],
              "talents": []
            }
            """;

        var repository = LoadRepositoryFromJson(json);
        var character = repository.GetCharacter("hero_knight");
        var skill = repository.GetInternalSkill("shape_shift");
        var affix = Assert.IsType<GrantModelAffix>(Assert.Single(skill.Affixes).Effect);

        Assert.Equal("hero_knight_model", character.Model);
        Assert.Equal("guoxiang", affix.ModelId);
        Assert.Equal("化身为天山童姥", affix.Description);
    }

    [Fact]
    public void JsonLoader_LoadsInternalSkillAndFormLegacyFields()
    {
        const string json = """
            {
              "characters": [],
              "externalSkills": [],
              "internalSkills": [
                {
                  "id": "huagong",
                  "name": "化功大法",
                  "description": "星宿老仙丁春秋为武林所不齿的内功绝技",
                  "icon": "icon/icon_neigong_011",
                  "yin": 50,
                  "yang": 0,
                  "attackScale": 0.2,
                  "criticalScale": 0.2,
                  "defenceScale": 0.2,
                  "hard": 4,
                  "formSkills": [
                    {
                      "id": "huagong_fushi_du",
                      "name": "化功大法.腐尸毒",
                      "description": "用自身带毒的内力散去敌方内功",
                      "icon": "icon/icon-huagongdafa",
                      "unlockLevel": 5,
                      "cooldown": 4,
                      "cost": {
                        "mp": 0,
                        "rage": 2
                      },
                      "targeting": {
                        "castType": null,
                        "castSize": 1,
                        "impactType": "fan",
                        "impactSize": 3
                      },
                      "powerExtra": 4,
                      "animation": "ball_purple",
                      "audio": "audio/Atk12",
                      "buffs": []
                    }
                  ],
                  "affixes": []
                }
              ],
              "legendSkills": [],
              "items": [],
              "buffs": [
                {
                  "id": "poison",
                  "name": "Poison",
                  "isDebuff": true,
                  "affixes": []
                }
              ],
              "talents": [],
            }
            """;

        var repository = LoadRepositoryFromJson(json);
        var internalSkill = repository.GetInternalSkill("huagong");

        Assert.Equal("星宿老仙丁春秋为武林所不齿的内功绝技", internalSkill.Description);
        Assert.Equal("icon/icon_neigong_011", internalSkill.Icon);
        Assert.Equal(50, internalSkill.Yin);
        Assert.Equal(0, internalSkill.Yang);
        Assert.Equal(0.2d, internalSkill.AttackScale, 6);
        Assert.Equal(0.2d, internalSkill.CriticalScale, 6);
        Assert.Equal(0.2d, internalSkill.DefenceScale, 6);
        Assert.Equal(4d, internalSkill.Hard, 6);
        Assert.Single(internalSkill.FormSkills);

        var form = internalSkill.FormSkills[0];
        Assert.Equal("用自身带毒的内力散去敌方内功", form.Description);
        Assert.Equal("icon/icon-huagongdafa", form.Icon);
        Assert.Equal(SkillImpactType.Fan, form.Targeting!.ImpactType);
        Assert.Equal(3, form.Targeting.ImpactSize);
        Assert.Equal(1, form.Targeting.CastSize);
        Assert.Equal(4d, form.PowerExtra, 6);
        Assert.Equal("ball_purple", form.Animation);
        Assert.Equal("audio/Atk12", form.Audio);
        Assert.Equal(2, form.Cost.Rage);
        Assert.Equal(5, form.UnlockLevel);
    }

    [Fact]
    public void JsonLoader_LoadsExternalLegendAndSpecialLegacyFields()
    {
        const string json = """
            {
              "characters": [],
              "externalSkills": [
                {
                  "id": "songfeng",
                  "name": "松风剑法",
                  "description": "入门剑法",
                  "icon": "icon/icon_waigong_003",
                  "type": "jianfa",
                  "isHarmony": false,
                  "affinity": 0,
                  "hard": 1,
                  "cooldown": 0,
                  "cost": {
                    "mp": 0,
                    "rage": 0
                  },
                  "targeting": {
                    "castType": null,
                    "castSize": 1,
                    "impactType": "star",
                    "impactSize": 3
                  },
                  "powerBase": 3,
                  "powerStep": 0.55,
                  "audio": "audio/sword",
                  "animation": "baozha_cheng",
                  "buffs": [],
                  "levelOverrides": [
                    {
                      "level": 10,
                      "powerOverride": 12,
                      "animation": "storm_burst",
                      "cooldown": 2
                    }
                  ],
                  "formSkills": [],
                  "affixes": []
                }
              ],
              "internalSkills": [],
              "legendSkills": [
                {
                  "id": "songfeng_legend",
                  "name": "松风奥义",
                  "startSkill": "songfeng",
                  "probability": 0.3,
                  "conditions": [
                    {
                      "type": "special_skill",
                      "targetId": "huatuo"
                    }
                  ],
                  "buffs": [],
                  "powerExtra": 6,
                  "requiredLevel": 8,
                  "animation": "aoyi_jian"
                }
              ],
              "specialSkills": [
                {
                  "id": "huatuo",
                  "name": "华佗再世",
                  "description": "治疗一个角色",
                  "icon": "icon/icon_teji_001",
                  "cooldown": 1,
                  "cost": {
                    "mp": 200,
                    "rage": 0
                  },
                  "targeting": {
                    "canCastAtSelf": true,
                    "canImpactSelf": true,
                    "castType": null,
                    "castSize": 10,
                    "impactType": "single",
                    "impactSize": 1
                  },
                  "animation": "guanghuan_yellow3",
                  "audio": "audio/a_recover2",
                  "buffs": [
                    { "id": "regeneration", "level": 2, "duration": 3, "chance": 100 }
                  ]
                }
              ],
              "items": [],
              "buffs": [
                {
                  "id": "regeneration",
                  "name": "Regeneration",
                  "isDebuff": false,
                  "affixes": []
                }
              ],
              "talents": [],
            }
            """;

        var repository = LoadRepositoryFromJson(json);
        var externalSkill = repository.GetExternalSkill("songfeng");
        var legendSkill = Assert.Single(repository.GetLegendSkills());
        var specialSkill = repository.GetSpecialSkill("huatuo");

        Assert.Equal("入门剑法", externalSkill.Description);
        Assert.Equal(3d, externalSkill.PowerBase, 6);
        Assert.Equal(0.55d, externalSkill.PowerStep, 6);
        Assert.Equal("icon/icon_waigong_003", externalSkill.Icon);
        Assert.Equal(WeaponType.Jianfa, externalSkill.Type);
        Assert.False(externalSkill.IsHarmony);
        Assert.Equal(1d, externalSkill.Hard, 6);
        Assert.Equal(SkillImpactType.Star, externalSkill.Targeting!.ImpactType);
        Assert.Equal(3, externalSkill.Targeting.ImpactSize);
        Assert.Equal(1, externalSkill.Targeting.CastSize);
        Assert.Equal("audio/sword", externalSkill.Audio);
        Assert.Equal("baozha_cheng", externalSkill.Animation);
        Assert.NotNull(externalSkill.LevelOverrides);
        Assert.True(externalSkill.LevelOverrides!.TryGetValue(10, out var level));
        Assert.Equal(12d, level.PowerOverride!.Value, 6);
        Assert.Equal("storm_burst", level.Animation);
        Assert.Equal(2, level.Cooldown);

        Assert.Equal("songfeng", legendSkill.StartSkill);
        Assert.Equal(6d, legendSkill.PowerExtra, 6);
        Assert.Equal(8, legendSkill.RequiredLevel);
        Assert.Equal("aoyi_jian", legendSkill.Animation);
        var specialSkillCondition = Assert.IsType<RequiredSpecialSkillLegendConditionDefinition>(Assert.Single(legendSkill.Conditions));
        Assert.Equal("huatuo", specialSkillCondition.TargetId);

        Assert.Equal("治疗一个角色", specialSkill.Description);
        Assert.Equal("icon/icon_teji_001", specialSkill.Icon);
        Assert.Equal(10, specialSkill.Targeting!.CastSize);
        Assert.Equal(1, specialSkill.Targeting.ImpactSize);
        Assert.Equal("audio/a_recover2", specialSkill.Audio);
        Assert.Equal(SkillImpactType.Single, specialSkill.Targeting.ImpactType);
        Assert.Equal("guanghuan_yellow3", specialSkill.Animation);
        Assert.Equal(1, specialSkill.Cooldown);
        Assert.Equal(200, specialSkill.Cost.Mp);
        Assert.Equal(0, specialSkill.Cost.Rage);
        Assert.True(specialSkill.Targeting.CanImpactSelf);
        Assert.Single(specialSkill.Buffs);
        Assert.Equal("regeneration", specialSkill.Buffs[0].Buff.Id);
    }

    [Fact]
    public void JsonLoader_AllowsLegendStartSkillToReferenceFormSkill()
    {
        const string json = """
            {
              "battles": [],
              "characters": [],
              "externalSkills": [
                {
                  "id": "basic_skill",
                  "name": "基础外功"
                }
              ],
              "internalSkills": [
                {
                  "id": "longxiang",
                  "name": "龙象般若功",
                  "formSkills": [
                    {
                      "id": "longxiang.form",
                      "name": "龙象秘境",
                      "description": "",
                      "icon": "",
                      "unlockLevel": 5,
                      "cooldown": 2,
                      "cost": {},
                      "targeting": null,
                      "powerExtra": 1.0,
                      "animation": "",
                      "audio": "",
                      "buffs": []
                    }
                  ]
                }
              ],
              "legendSkills": [
                {
                  "id": "longxiang.legend",
                  "name": "龙象奥义",
                  "startSkill": "longxiang.form",
                  "probability": 0.3,
                  "requiredLevel": 1,
              "conditions": [
                {
                  "type": "skill",
                  "targetId": "basic_skill"
                },
                {
                  "type": "internal_skill",
                  "targetId": "longxiang"
                }
              ],
                  "buffs": []
                }
              ],
              "specialSkills": [],
              "gameTips": [],
              "growTemplates": [],
              "items": [],
              "buffs": [],
              "talents": [],
              "maps": [],
              "resources": [],
              "sects": [],
              "shops": [],
              "towers": []
            }
            """;

        var repository = LoadRepositoryFromJson(json);

        Assert.Equal("longxiang.form", Assert.Single(repository.GetLegendSkills()).StartSkill);
    }

    [Fact]
    public void JsonLoader_LoadsStoryScriptsFromStoryDirectory()
    {
        var directoryPath = CreateContentDirectory(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["stories/main.story.json"] =
                """
                {
                  "version": 3,
                  "segments": [
                    {
                      "name": "story_intro",
                      "steps": [
                        { "kind": "dialogue", "speaker": "旁白", "text": "开始。" },
                        { "kind": "jump", "target": "story_end" }
                      ]
                    },
                    {
                      "name": "story_end",
                      "steps": [
                        { "kind": "dialogue", "speaker": "旁白", "text": "结束。" }
                      ]
                    }
                  ]
                }
                """,
        });

        try
        {
            var repository = new JsonContentLoader().LoadFromDirectory(directoryPath);

            var script = repository.GetStoryScript("main");
            var segment = repository.GetStorySegment("story_intro");

            Assert.Equal(2, script.Segments.Count);
            Assert.Equal("main", segment.ScriptId);
            Assert.NotSame(script, segment.Script);
            Assert.True(segment.Script.Segments.Count >= script.Segments.Count);
            Assert.Contains(segment.Script.Segments, entry => string.Equals(entry.Name, "story_intro", StringComparison.Ordinal));
            Assert.Contains(segment.Script.Segments, entry => string.Equals(entry.Name, "story_end", StringComparison.Ordinal));
            Assert.Equal("story_intro", segment.Segment.Name);
        }
        finally
        {
            Directory.Delete(directoryPath, recursive: true);
        }
    }

    [Fact]
    public void JsonLoader_LoadsStoryBattleWithoutOutcomeBranches()
    {
        var directoryPath = CreateContentDirectory(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["battles.json"] =
                """
                [
                  {
                    "id": "training",
                    "name": "Training",
                    "background": "battle_bg/training_map"
                  }
                ]
                """,
            ["stories/main.story.json"] =
                """
                {
                  "version": 3,
                  "segments": [
                    {
                      "name": "story_intro",
                      "steps": [
                        { "kind": "battle", "battleId": "training", "outcomes": {} }
                      ]
                    }
                  ]
                }
                """,
        });

        try
        {
            var repository = new JsonContentLoader().LoadFromDirectory(directoryPath);
            var battle = Assert.IsType<BattleStep>(Assert.Single(repository.GetStorySegment("story_intro").Segment.Steps));

            Assert.Empty(battle.Outcomes);
        }
        finally
        {
            Directory.Delete(directoryPath, recursive: true);
        }
    }

    [Fact]
    public void JsonLoader_RejectsMissingStoryJumpTarget()
    {
        var directoryPath = CreateContentDirectory(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["stories/main.story.json"] =
                """
                {
                  "version": 3,
                  "segments": [
                    {
                      "name": "story_intro",
                      "steps": [
                        { "kind": "jump", "target": "story_missing" }
                      ]
                    }
                  ]
                }
                """,
        });

        try
        {
            Assert.Throws<InvalidOperationException>(() => new JsonContentLoader().LoadFromDirectory(directoryPath));
        }
        finally
        {
            Directory.Delete(directoryPath, recursive: true);
        }
    }

    [Fact]
    public void JsonLoader_RejectsMissingStoryCallTarget()
    {
        var directoryPath = CreateContentDirectory(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["stories/main.story.json"] =
                """
                {
                  "version": 3,
                  "segments": [
                    {
                      "name": "story_intro",
                      "steps": [
                        { "kind": "call", "target": "story_missing" }
                      ]
                    }
                  ]
                }
                """,
        });

        try
        {
            Assert.Throws<InvalidOperationException>(() => new JsonContentLoader().LoadFromDirectory(directoryPath));
        }
        finally
        {
            Directory.Delete(directoryPath, recursive: true);
        }
    }

    [Fact]
    public void JsonLoader_RejectsMissingStoryReferencedByMap()
    {
        var directoryPath = CreateContentDirectory(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["maps/test.json"] =
                """
                [
                  {
                    "id": "sample_map",
                    "name": "Sample Map",
                    "kind": "small",
                    "locations": [
                      {
                        "id": "keeper",
                        "name": "Keeper",
                        "events": [
                          {
                            "id": "sample_map-keeper-missing_story",
                            "action": "story('story_missing')"
                          }
                        ]
                      }
                    ]
                  }
                ]
                """,
            ["stories/main.story.json"] =
                """
                {
                  "version": 3,
                  "segments": [
                    {
                      "name": "story_intro",
                      "steps": [
                        { "kind": "dialogue", "speaker": "旁白", "text": "开始。" }
                      ]
                    }
                  ]
                }
                """,
        });

        try
        {
            Assert.Throws<InvalidOperationException>(() => new JsonContentLoader().LoadFromDirectory(directoryPath));
        }
        finally
        {
            Directory.Delete(directoryPath, recursive: true);
        }
    }

    [Theory]
    [InlineData("map('world_map', 'missing')")]
    [InlineData("set_map('world_map', '')")]
    [InlineData("tutorial('small_map', 'door')")]
    public void JsonLoader_RejectsInvalidLiteralMapCommandLocation(string call)
    {
        var directoryPath = CreateContentDirectory(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["maps/test.json"] =
                """
                [
                  {
                    "id": "world_map",
                    "name": "World Map",
                    "kind": "large",
                    "travelSpeed": 10,
                    "defaultLocation": "start",
                    "locations": [
                      {"id":"start","position":{"x":0,"y":0}}
                    ]
                  },
                  {
                    "id": "small_map",
                    "name": "Small Map",
                    "locations": [
                      {"id":"door"}
                    ]
                  }
                ]
                """,
            ["stories/main.story.json"] =
                $$"""
                {
                  "version": 3,
                  "segments": [
                    {
                      "name": "story_intro",
                      "steps": [
                        {"kind":"command","call":{{JsonSerializer.Serialize(call)}}}
                      ]
                    }
                  ]
                }
                """,
        });

        try
        {
            Assert.Throws<InvalidOperationException>(() => new JsonContentLoader().LoadFromDirectory(directoryPath));
        }
        finally
        {
            Directory.Delete(directoryPath, recursive: true);
        }
    }

    [Fact]
    public void JsonLoader_AcceptsLiteralMapCommandLocation()
    {
        var directoryPath = CreateContentDirectory(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["maps/test.json"] =
                """
                [
                  {
                    "id": "world_map",
                    "name": "World Map",
                    "kind": "large",
                    "travelSpeed": 10,
                    "defaultLocation": "start",
                    "locations": [
                      {"id":"start","position":{"x":0,"y":0}},
                      {"id":"gate","position":{"x":10,"y":20}}
                    ]
                  }
                ]
                """,
            ["stories/main.story.json"] =
                """
                {
                  "version": 3,
                  "segments": [
                    {
                      "name": "story_intro",
                      "steps": [
                        {"kind":"command","call":"map('world_map', 'gate')"}
                      ]
                    }
                  ]
                }
                """,
        });

        try
        {
            Assert.NotNull(new JsonContentLoader().LoadFromDirectory(directoryPath));
        }
        finally
        {
            Directory.Delete(directoryPath, recursive: true);
        }
    }

    [Fact]
    public void JsonLoader_RejectsMissingStoryReferencedByWorldTrigger()
    {
        var directoryPath = CreateContentDirectory(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["maps/test.json"] =
                """
                [
                  {
                    "id": "world_map",
                    "name": "World Map",
                    "kind": "large",
                    "travelSpeed": 10,
                    "defaultLocation": "start",
                    "locations": [
                      {"id":"start","position":{"x":0,"y":0}}
                    ]
                  }
                ]
                """,
            ["world-triggers.json"] =
                """
                [
                  {
                    "id": "story_missing",
                    "action": "story('story_missing')"
                  }
                ]
                """,
            ["stories/main.story.json"] =
                """
                {
                  "version": 3,
                  "segments": [
                    {
                      "name": "story_intro",
                      "steps": [
                        { "kind": "dialogue", "speaker": "旁白", "text": "开始。" }
                      ]
                    }
                  ]
                }
                """,
        });

        try
        {
            Assert.Throws<InvalidOperationException>(() => new JsonContentLoader().LoadFromDirectory(directoryPath));
        }
        finally
        {
            Directory.Delete(directoryPath, recursive: true);
        }
    }

    [Fact]
    public void JsonLoader_AcceptsDistinctNestedIdsWithTheSameReward()
    {
        var directoryPath = CreateContentDirectory(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["items.json"] =
                """
                [
                  {"category":"normal","id":"reward","name":"reward","type":"consumable","consumeOnUse":true}
                ]
                """,
            ["battles.json"] =
                """
                [
                  {"id":"battle","name":"battle","background":"battle_bg/map"}
                ]
                """,
            ["shops.json"] =
                """
                [
                  {
                    "id":"shop","name":"shop",
                    "products":[
                      {"id":"first","reward":{"kind":"item","itemId":"reward"}},
                      {"id":"second","reward":{"kind":"item","itemId":"reward"}}
                    ]
                  }
                ]
                """,
            ["towers.json"] =
                """
                [
                  {
                    "id":"tower","name":"tower",
                    "stages":[{
                      "id":"stage","battleId":"battle",
                      "rewards":[
                        {"id":"first","reward":{"kind":"item","itemId":"reward"},"weight":1},
                        {"id":"second","reward":{"kind":"item","itemId":"reward"},"weight":1}
                      ]
                    }]
                  }
                ]
                """,
        });

        try
        {
            Assert.NotNull(new JsonContentLoader().LoadFromDirectory(directoryPath));
        }
        finally
        {
            Directory.Delete(directoryPath, recursive: true);
        }
    }

    [Theory]
    [InlineData("", "second")]
    [InlineData("duplicate", "duplicate")]
    public void JsonLoader_RejectsInvalidShopProductIds(string firstId, string secondId)
    {
        var directoryPath = CreateContentDirectory(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["items.json"] =
                """
                [{"category":"normal","id":"reward","name":"reward","type":"consumable","consumeOnUse":true}]
                """,
            ["shops.json"] =
                $$$"""
                [{"id":"shop","name":"shop","products":[
                  {"id":"{{{firstId}}}","reward":{"kind":"item","itemId":"reward"}},
                  {"id":"{{{secondId}}}","reward":{"kind":"item","itemId":"reward"}}
                ]}]
                """,
        });

        try
        {
            Assert.Throws<InvalidOperationException>(() => new JsonContentLoader().LoadFromDirectory(directoryPath));
        }
        finally
        {
            Directory.Delete(directoryPath, recursive: true);
        }
    }

    [Theory]
    [InlineData("", "second")]
    [InlineData("duplicate", "duplicate")]
    public void JsonLoader_RejectsInvalidTowerRewardIds(string firstId, string secondId)
    {
        var directoryPath = CreateContentDirectory(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["items.json"] =
                """
                [{"category":"normal","id":"reward","name":"reward","type":"consumable","consumeOnUse":true}]
                """,
            ["battles.json"] =
                """
                [{"id":"battle","name":"battle","background":"battle_bg/map"}]
                """,
            ["towers.json"] =
                $$$"""
                [{"id":"tower","name":"tower","stages":[{
                  "id":"stage","battleId":"battle","rewards":[
                    {"id":"{{{firstId}}}","reward":{"kind":"item","itemId":"reward"},"weight":1},
                    {"id":"{{{secondId}}}","reward":{"kind":"item","itemId":"reward"},"weight":1}
                  ]
                }]}]
                """,
        });

        try
        {
            Assert.Throws<InvalidOperationException>(() => new JsonContentLoader().LoadFromDirectory(directoryPath));
        }
        finally
        {
            Directory.Delete(directoryPath, recursive: true);
        }
    }

    [Fact]
    public void JsonLoader_RejectsInvalidExtraStrikeBattleHookEffect()
    {
        var directoryPath = CreateContentDirectory(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["buffs.json"] =
                """
                [
                  {
                    "id": "左右互搏",
                    "name": "左右互搏",
                    "isDebuff": false,
                    "affixes": [
                      {
                        "type": "hook",
                        "timing": "OnHitConfirmed",
                        "effects": [
                          {
                            "type": "extra_strike",
                            "target": { "type": "target" },
                            "chancePerBuffLevel": 5,
                            "damageFactors": []
                          }
                        ]
                      }
                    ]
                  }
                ]
                """,
        });

        try
        {
            Assert.Throws<InvalidOperationException>(() => new JsonContentLoader().LoadFromDirectory(directoryPath));
        }
        finally
        {
            Directory.Delete(directoryPath, recursive: true);
        }
    }

    [Theory]
    [InlineData("OnDamageTaken", "modify_damage")]
    [InlineData("BeforeDamageApplied", "modify_damage_context")]
    [InlineData("BeforeDamageCalculation", "cancel_hit")]
    public void JsonLoader_RejectsBattleEffectsAtUnsupportedTimings(string timing, string effectType)
    {
        var effect = effectType == "modify_damage_context"
            ? """{ "type": "modify_damage_context", "field": "final_damage", "op": "more", "delta": 0.5 }"""
            : effectType == "modify_damage"
                ? """{ "type": "modify_damage", "op": "more", "delta": 0.5 }"""
                : """{ "type": "cancel_hit" }""";
        var directoryPath = CreateContentDirectory(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["talents.json"] =
                $$"""
                [
                  {
                    "id": "invalid_timing",
                    "name": "invalid_timing",
                    "affixes": [
                      {
                        "type": "hook",
                        "timing": "{{timing}}",
                        "effects": [{{effect}}]
                      }
                    ]
                  }
                ]
                """,
        });

        try
        {
            Assert.Throws<InvalidOperationException>(() => new JsonContentLoader().LoadFromDirectory(directoryPath));
        }
        finally
        {
            Directory.Delete(directoryPath, recursive: true);
        }
    }

    private static InMemoryContentRepository LoadRepositoryFromJson(string json)
    {
        var loader = new JsonContentLoader();
        var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
        File.WriteAllText(tempPath, json);

        try
        {
            return loader.LoadFromFile(tempPath);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    private static string CreateContentDirectory(IReadOnlyDictionary<string, string> overrides)
    {
        var directoryPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directoryPath);

        var files = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["battles.json"] = "[]",
            ["scoped-battle-effects.json"] = "[]",
            ["characters.json"] = "[]",
            ["external-skills.json"] = "[]",
            ["game-tips.json"] = "[]",
            ["grow-templates.json"] = "[]",
            ["internal-skills.json"] = "[]",
            ["legend-skills.json"] = "[]",
            ["maps/empty.json"] = "[]",
            ["world-triggers.json"] = "[]",
            ["resources.json"] = "[]",
            ["sects.json"] = "[]",
            ["shops.json"] = "[]",
            ["special-skills.json"] = "[]",
            ["items.json"] = "[]",
            ["item-tags.json"] = "[]",
            ["random-affix-tables.json"] = "[]",
            ["buffs.json"] = "[]",
            ["talents.json"] = "[]",
            ["towers.json"] = "[]",
        };

        foreach (var (relativePath, content) in overrides)
        {
            files[relativePath] = content;
        }

        foreach (var (relativePath, content) in files)
        {
            var fullPath = Path.Combine(directoryPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
            var parentDirectory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(parentDirectory))
            {
                Directory.CreateDirectory(parentDirectory);
            }

            File.WriteAllText(fullPath, content);
        }

        return directoryPath;
    }
}
