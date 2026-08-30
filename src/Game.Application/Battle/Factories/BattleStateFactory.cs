using Game.Core.Abstractions;
using Game.Core.Battle;
using Game.Core.Definitions;
using Game.Core.Model;
using Game.Core.Model.Character;

namespace Game.Application;

internal sealed class BattleStateFactory
{
    // A couple of the legacy battle tables refer to dynamically named sect
    // champions that are not present in roles.xml.  Keep their intended
    // presentation by resolving to the closest canonical champion definition.
    private static readonly IReadOnlyDictionary<string, string> LegacyCharacterAliases =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["日月首席"] = "吸星首席",
            ["武当首席9"] = "武当首席",
        };

    private readonly GameSession _session;
    private readonly ProceduralBattleCharacterFactory _characterFactory;
    private readonly ZhenlongqijuBattleFactory _zhenlongqijuFactory;

    public BattleStateFactory(
        GameSession session,
        ProceduralBattleCharacterFactory characterFactory,
        ZhenlongqijuBattleFactory zhenlongqijuFactory)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(characterFactory);
        ArgumentNullException.ThrowIfNull(zhenlongqijuFactory);
        _session = session;
        _characterFactory = characterFactory;
        _zhenlongqijuFactory = zhenlongqijuFactory;
    }

    private GameState State => _session.State;
    private IContentRepository ContentRepository => _session.ContentRepository;
    private int PlayerTeam => _session.Config.BattlePlayerTeam;
    private GameConfig Config => _session.Config;

    public BattleState BuildBattleState(SpecialBattleRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var battle = ContentRepository.GetBattle(request.BattleId);
        return request switch
        {
            OrdinaryBattleRequest ordinary => BuildBattleState(battle, ordinary.SelectedCharacterIds, ordinary.BattleLevel),
            ArenaBattleRequest arena => BuildArenaBattleState(battle, arena.SelectedCharacterIds, arena.HardLevel),
            ZhenlongqijuBattleRequest zhenlongqiju => BuildZhenlongqijuBattleState(
                battle,
                zhenlongqiju.SelectedCharacterIds,
                zhenlongqiju.Level),
            _ => throw new InvalidOperationException(
                $"Unsupported special battle request type '{request.GetType().Name}'."),
        };
    }

    private BattleState BuildArenaBattleState(
        BattleDefinition battle,
        IReadOnlyList<string> selectedCharacterIds,
        int hardLevel)
    {
        ArgumentNullException.ThrowIfNull(battle);
        ArgumentNullException.ThrowIfNull(selectedCharacterIds);
        ArgumentOutOfRangeException.ThrowIfLessThan(hardLevel, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(hardLevel, 6);

        var battleDifficulty = State.Adventure.Difficulty;
        return BuildBattleStateCore(
            battle,
            selectedCharacterIds,
            (participant, index, slotCharacters, tempFactory) =>
                participant.Team == PlayerTeam
                    ? ResolveParticipantCharacter(participant, index, slotCharacters, tempFactory, battleDifficulty)
                    : _characterFactory.CreateArenaOpponentCharacter(hardLevel, index, tempFactory),
            (_, index, tempFactory) =>
                _characterFactory.CreateArenaOpponentCharacter(hardLevel, index + battle.Participants.Count, tempFactory),
            (character, _) => _characterFactory.ApplyNpcRoundPowerUp(character),
            CreateBattleRuleSettings(battleDifficulty, enableRoundEnemyAttackDefenceScaling: true));
    }

    private BattleState BuildZhenlongqijuBattleState(
        BattleDefinition battle,
        IReadOnlyList<string> selectedCharacterIds,
        int level)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(level);

        const GameDifficulty battleDifficulty = GameDifficulty.Crazy;
        return BuildBattleStateCore(
            battle,
            selectedCharacterIds,
            (participant, index, slotCharacters, tempFactory) =>
                ResolveParticipantCharacter(participant, index, slotCharacters, tempFactory, battleDifficulty),
            (participant, index, tempFactory) =>
                _characterFactory.CreateRandomParticipantCharacter(participant, index, tempFactory, battleDifficulty),
            (character, tempFactory) => _zhenlongqijuFactory.PowerUpEnemy(character, level, tempFactory),
            CreateBattleRuleSettings(battleDifficulty, enableRoundEnemyAttackDefenceScaling: false));
    }

    private BattleState BuildBattleState(
        BattleDefinition battle,
        IReadOnlyList<string> selectedCharacterIds,
        int battleLevel = 0)
    {
        ArgumentNullException.ThrowIfNull(battle);
        ArgumentNullException.ThrowIfNull(selectedCharacterIds);
        ArgumentOutOfRangeException.ThrowIfNegative(battleLevel);

        var selectedSet = selectedCharacterIds.ToHashSet(StringComparer.Ordinal);
        foreach (var requiredId in battle.RequiredCharacterIds.Where(id => !string.IsNullOrWhiteSpace(id)))
        {
            if (!selectedSet.Contains(requiredId))
                throw new InvalidOperationException($"Battle '{battle.Id}' requires character '{requiredId}' to be selected.");
        }
        foreach (var excludedId in battle.ExcludedCharacterIds.Where(id => !string.IsNullOrWhiteSpace(id)))
        {
            if (selectedSet.Contains(excludedId))
                throw new InvalidOperationException($"Battle '{battle.Id}' forbids character '{excludedId}' from being selected.");
        }

        var battleDifficulty = State.Adventure.Difficulty;
        return BuildBattleStateCore(
            battle,
            selectedCharacterIds,
            (participant, index, slotCharacters, tempFactory) =>
                ResolveParticipantCharacter(participant, index, slotCharacters, tempFactory, battleDifficulty),
            (participant, index, tempFactory) =>
                _characterFactory.CreateRandomParticipantCharacter(participant, index, tempFactory, battleDifficulty),
            (character, equipmentFactory) =>
            {
                if (battleLevel > 0)
                {
                    _zhenlongqijuFactory.PowerUpEnemy(character, battleLevel, equipmentFactory);
                }
                else
                {
                    _characterFactory.ApplyNpcRoundPowerUp(character);
                }
            },
            CreateBattleRuleSettings(battleDifficulty, enableRoundEnemyAttackDefenceScaling: true));
    }

    private BattleState BuildBattleStateCore(
        BattleDefinition battle,
        IReadOnlyList<string> selectedCharacterIds,
        Func<BattleParticipantDefinition, int, IReadOnlyList<CharacterInstance?>, EquipmentInstanceFactory, CharacterInstance?> participantResolver,
        Func<BattleRandomParticipantDefinition, int, EquipmentInstanceFactory, CharacterInstance> randomParticipantResolver,
        Action<CharacterInstance, EquipmentInstanceFactory> npcCharacterProcessor,
        BattleRuleSettings ruleSettings)
    {
        ArgumentNullException.ThrowIfNull(battle);
        ArgumentNullException.ThrowIfNull(selectedCharacterIds);
        ArgumentNullException.ThrowIfNull(participantResolver);
        ArgumentNullException.ThrowIfNull(randomParticipantResolver);
        ArgumentNullException.ThrowIfNull(npcCharacterProcessor);

        var units = new List<BattleUnit>();
        var tempFactory = new EquipmentInstanceFactory();
        var slotCharacters = selectedCharacterIds
            .Select(ResolvePartyCharacter)
            .ToArray();

        foreach (var (participant, index) in battle.Participants.Select(static (participant, index) => (participant, index)))
        {
            var character = participantResolver(participant, index, slotCharacters, tempFactory);
            if (character is null)
            {
                continue;
            }

            if (!IsPartyCharacterInstance(character))
            {
                npcCharacterProcessor(character, tempFactory);
            }

            units.Add(CreateUnit(
                $"participant_{index}_{character.Id}",
                character,
                participant.Team,
                participant.Position,
                participant.Facing));
        }

        foreach (var (participant, index) in battle.RandomParticipants.Select(static (participant, index) => (participant, index)))
        {
            var character = randomParticipantResolver(participant, index, tempFactory);
            npcCharacterProcessor(character, tempFactory);
            units.Add(CreateUnit(
                $"random_{index}_{character.Id}",
                character,
                participant.Team,
                participant.Position,
                participant.Facing));
        }

        var state = new BattleState(new BattleGrid(Config.BattleGridWidth, Config.BattleGridHeight), units, ruleSettings);
        if (!state.Units.Any(unit => unit.Team == PlayerTeam))
        {
            throw new InvalidOperationException($"Battle '{battle.Id}' must contain at least one player team unit.");
        }

        return state;
    }

    private CharacterInstance? ResolveParticipantCharacter(
        BattleParticipantDefinition participant,
        int index,
        IReadOnlyList<CharacterInstance?> slotCharacters,
        EquipmentInstanceFactory tempFactory,
        GameDifficulty battleDifficulty)
    {
        if (!string.IsNullOrWhiteSpace(participant.CharacterId))
        {
            if (participant.Team == PlayerTeam &&
                State.Party.TryGetCharacter(participant.CharacterId, out var partyCharacter))
            {
                return partyCharacter;
            }

            var requestedCharacterId = participant.CharacterId;
            var resolvedCharacterId = LegacyCharacterAliases.TryGetValue(
                requestedCharacterId,
                out var alias)
                ? alias
                : requestedCharacterId;
            if (!ContentRepository.TryGetCharacter(resolvedCharacterId, out var definition))
            {
                // Battle XML occasionally contains a one-off NPC key without
                // a matching role record.  Use the ordinary low-tier NPC as a
                // safe runtime shell instead of aborting the whole battle;
                // retain the legacy key as the displayed name and log it for
                // later asset/content cleanup.
                _session.DiagnosticLogger.Warning(
                    $"Battle participant character '{requestedCharacterId}' is not defined; using fallback NPC data.");
                definition = ContentRepository.GetCharacter("小混混");
                resolvedCharacterId = definition.Id;
            }
            var character = CharacterMapper.CreateInitial(
                $"battle_{index}_{definition.Id}",
                definition,
                tempFactory);
            if (!string.Equals(requestedCharacterId, resolvedCharacterId, StringComparison.Ordinal))
            {
                character.Name = requestedCharacterId;
            }
            if (participant.Team != PlayerTeam)
            {
                _characterFactory.ApplyDifficultyRandomTalents(character, battleDifficulty);
                character.RebuildSnapshot();
            }

            return character;
        }

        if (participant.PartyIndex is not { } partyIndex ||
            partyIndex < 0 ||
            partyIndex >= slotCharacters.Count)
        {
            return null;
        }

        return slotCharacters[partyIndex];
    }

    private bool IsPartyCharacterInstance(CharacterInstance character) =>
        State.Party.GetAllCharacters().Any(partyCharacter => ReferenceEquals(partyCharacter, character));

    private BattleRuleSettings CreateBattleRuleSettings(
        GameDifficulty battleDifficulty,
        bool enableRoundEnemyAttackDefenceScaling)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(State.Adventure.Round, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(Config.RoundEnemyAttackAddRatio);
        ArgumentOutOfRangeException.ThrowIfNegative(Config.RoundEnemyDefenceAddRatio);

        return new BattleRuleSettings
        {
            Difficulty = battleDifficulty,
            Round = State.Adventure.Round,
            PlayerTeam = PlayerTeam,
            RoundEnemyAttackAddRatio = Config.RoundEnemyAttackAddRatio,
            RoundEnemyDefenceAddRatio = Config.RoundEnemyDefenceAddRatio,
            EnableRoundEnemyAttackDefenceScaling = enableRoundEnemyAttackDefenceScaling,
            EnableDifficultyDamageScaling = true,
            EnableDifficultyItemCooldownRules = true,
        };
    }

    private CharacterInstance? ResolvePartyCharacter(string characterId) =>
        State.Party.TryGetCharacter(characterId, out var character) ? character : null;

    private static BattleUnit CreateUnit(
        string id,
        CharacterInstance character,
        int team,
        GridPosition position,
        int facing) =>
        new(
            id,
            character,
            team,
            position,
            facing <= 0 ? BattleFacing.Left : BattleFacing.Right,
            hp: character.CurrentHp,
            mp: character.CurrentMp,
            rage: character.CurrentRage);
}
