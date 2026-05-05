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
        GameConfig? config = null)
        : this(initialState, contentRepository, NullRuntimeHost.Instance, logger, initialProfile, config)
    {
    }

    public GameSession(
        GameState initialState,
        IContentRepository contentRepository,
        Game.Core.Story.IRuntimeHost storyRuntimeHost,
        IDiagnosticLogger? logger = null,
        GameProfile? initialProfile = null,
        GameConfig? config = null)
    {
        ArgumentNullException.ThrowIfNull(initialState);
        ArgumentNullException.ThrowIfNull(contentRepository);
        ArgumentNullException.ThrowIfNull(storyRuntimeHost);
        State = initialState;
        Profile = initialProfile ?? new GameProfile();
        Config = config ?? new GameConfig();
        ContentRepository = contentRepository;
        SaveGameService = new SaveGameService(this, logger);
        ProfileService = new ProfileService(this, logger);
        SessionFlowService = new SessionFlowService(this);
        PartyService = new PartyService(this);
        InventoryService = new InventoryService(this);
        ChestService = new ChestService(this);
        CharacterService = new CharacterService(this);
        ItemUseService = new ItemUseService(this);
        ShopService = new ShopService(this);
        MapService = new MapService(this);
        StoryTimeKeyExpirationService = new StoryTimeKeyExpirationService(this);
        StoryService = new StoryService(this, storyRuntimeHost);
    }

    public SessionEvents Events { get; } = new();
    public GameState State { get; private set; }
    public GameProfile Profile { get; private set; }
    public GameConfig Config { get; }
    public IContentRepository ContentRepository { get; }
    public SaveGameService SaveGameService { get; }
    public ProfileService ProfileService { get; }
    public SessionFlowService SessionFlowService { get; }
    public PartyService PartyService { get; }
    public InventoryService InventoryService { get; }
    public ChestService ChestService { get; }
    public CharacterService CharacterService { get; }
    public ItemUseService ItemUseService { get; }
    public ShopService ShopService { get; }
    public MapService MapService { get; }
    public StoryTimeKeyExpirationService StoryTimeKeyExpirationService { get; }
    public StoryService StoryService { get; }

    public void ReplaceState(GameState state) => State = state;

    public void ReplaceProfile(GameProfile profile) => Profile = profile;
}
