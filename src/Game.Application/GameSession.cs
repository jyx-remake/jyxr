using Game.Core.Abstractions;
using Game.Core.Model;

namespace Game.Application;

public sealed class GameSession
{
    public GameSession(
        GameState initialState,
        IContentRepository contentRepository,
        IDiagnosticLogger? logger = null,
        GameProfile? initialProfile = null,
        GameConfig? config = null,
        IRandomService? randomService = null,
        TimeProvider? timeProvider = null)
        : this(initialState, contentRepository, NullRuntimeHost.Instance, logger, initialProfile, config, randomService: randomService, timeProvider: timeProvider)
    {
    }

    public GameSession(
        GameState initialState,
        IContentRepository contentRepository,
        Game.Core.Story.IRuntimeHost storyRuntimeHost,
        IDiagnosticLogger? logger = null,
        GameProfile? initialProfile = null,
        GameConfig? config = null,
        GameSettings? settings = null,
        IRandomService? randomService = null,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(initialState);
        ArgumentNullException.ThrowIfNull(contentRepository);
        ArgumentNullException.ThrowIfNull(storyRuntimeHost);
        State = initialState;
        Profile = initialProfile ?? new GameProfile();
        Config = config ?? new GameConfig();
        Settings = settings ?? new GameSettings();
        ContentRepository = contentRepository;
        DiagnosticLogger = logger ?? NullDiagnosticLogger.Instance;
        RandomService = randomService ?? SharedRandomService.Instance;
        TimeProvider = timeProvider ?? global::System.TimeProvider.System;
        GameExpressionSymbols.ValidateDynamicVariables(initialState, StoryExecutionContext.Empty);
        SkillMaxLevelPolicy = new SkillMaxLevelPolicy(this);
        CharacterResourceLimitPolicy = new CharacterResourceLimitPolicy(this);
        PlayTimeService = new PlayTimeService(this, TimeProvider);
        SaveGameService = new SaveGameService(this, DiagnosticLogger);
        ProfileService = new ProfileService(this, DiagnosticLogger);
        SessionFlowService = new SessionFlowService(this);
        PartyService = new PartyService(this);
        InventoryService = new InventoryService(this);
        ChestService = new ChestService(this);
        CharacterService = new CharacterService(this);
        ItemUseService = new ItemUseService(this);
        RewardGrantService = new RewardGrantService(this);
        ShopService = new ShopService(this);
        EquipmentRefinementService = new EquipmentRefinementService(this);
        BattleService = new BattleService(this);
        SpecialBattleService = new SpecialBattleService(this);
        MiniGameService = new MiniGameService(this);
        WorldTriggerService = new WorldTriggerService(this);
        MapService = new MapService(this);
        StoryTimeKeyExpirationService = new StoryTimeKeyExpirationService(this);
        StoryService = new StoryService(this, storyRuntimeHost);
    }

    public SessionEvents Events { get; } = new();
    public GameState State { get; private set; }
    public GameProfile Profile { get; private set; }
    public GameConfig Config { get; }
    public GameSettings Settings { get; }
    public IContentRepository ContentRepository { get; }
    public IRandomService RandomService { get; }
    public TimeProvider TimeProvider { get; }
    internal IDiagnosticLogger DiagnosticLogger { get; }
    public SkillMaxLevelPolicy SkillMaxLevelPolicy { get; }
    public CharacterResourceLimitPolicy CharacterResourceLimitPolicy { get; }
    public PlayTimeService PlayTimeService { get; }
    public SaveGameService SaveGameService { get; }
    public ProfileService ProfileService { get; }
    public SessionFlowService SessionFlowService { get; }
    public PartyService PartyService { get; }
    public InventoryService InventoryService { get; }
    public ChestService ChestService { get; }
    public CharacterService CharacterService { get; }
    public ItemUseService ItemUseService { get; }
    public RewardGrantService RewardGrantService { get; }
    public ShopService ShopService { get; }
    public EquipmentRefinementService EquipmentRefinementService { get; }
    public BattleService BattleService { get; }
    public SpecialBattleService SpecialBattleService { get; }
    public MiniGameService MiniGameService { get; }
    public WorldTriggerService WorldTriggerService { get; }
    public MapService MapService { get; }
    public StoryTimeKeyExpirationService StoryTimeKeyExpirationService { get; }
    public StoryService StoryService { get; }

    public void ReplaceState(GameState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        GameExpressionSymbols.ValidateDynamicVariables(state, StoryExecutionContext.Empty);
        State = state;
    }

    public void ReplaceProfile(GameProfile profile) => Profile = profile;

    private sealed class SharedRandomService : IRandomService
    {
        public static SharedRandomService Instance { get; } = new();
        public double NextDouble() => Random.Shared.NextDouble();
        public int Next(int minInclusive, int maxExclusive) => Random.Shared.Next(minInclusive, maxExclusive);
    }
}
