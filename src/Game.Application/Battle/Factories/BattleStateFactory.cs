using Game.Core.Abstractions;
using Game.Core.Battle;
using Game.Core.Definitions;
using Game.Core.Model;
using Game.Core.Model.Character;

namespace Game.Application;

internal sealed class BattleStateFactory
{
    private const int GridWidth = 11;
    private const int GridHeight = 4;
    private const int SpawnSearchRadius = 8;
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
            OrdinaryBattleRequest ordinary => BuildBattleState(battle, ordinary.SelectedCharacterIds),
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

    private BattleState BuildBattleState(BattleDefinition battle, IReadOnlyList<string> selectedCharacterIds)
    {
        ArgumentNullException.ThrowIfNull(battle);
        ArgumentNullException.ThrowIfNull(selectedCharacterIds);

        var battleDifficulty = State.Adventure.Difficulty;
        return BuildBattleStateCore(
            battle,
            selectedCharacterIds,
            (participant, index, slotCharacters, tempFactory) =>
                ResolveParticipantCharacter(participant, index, slotCharacters, tempFactory, battleDifficulty),
            (participant, index, tempFactory) =>
                _characterFactory.CreateRandomParticipantCharacter(participant, index, tempFactory, battleDifficulty),
            (character, _) => _characterFactory.ApplyNpcRoundPowerUp(character),
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

            var participantKey = string.IsNullOrWhiteSpace(participant.CharacterId)
                ? character.Id
                : participant.CharacterId;
            //ԭ����participant_{index}_{character.Id}
            units.Add(CreateUnit(
                $"participant_{index}_{participantKey}",
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

        
        var state = new BattleState(new BattleGrid(GridWidth, GridHeight), units, ruleSettings);

        if (!string.IsNullOrWhiteSpace(battle.BattleEventSegmentId)) { 
        var battleEventSegmentEntry = _session.BattleService.FindBattleEventScript(battle.BattleEventSegmentId);
        state.BattleMainSegment = battleEventSegmentEntry.Segment;
        }
        
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

            var definition = ContentRepository.GetCharacter(participant.CharacterId);
            var character = CharacterMapper.CreateInitial(
                $"battle_{index}_{definition.Id}",
                definition,
                tempFactory);
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
    private CharacterInstance CreateCombatantCharacter(
        BattleState state,
        string characterId,
        EquipmentInstanceFactory tempFactory)
    {
        var definition = ContentRepository.GetCharacter(characterId);
        return CharacterMapper.CreateInitial(
            $"battle_{state.Units.Count + 1}_{characterId}",
            definition,
            tempFactory);
    }

    public IReadOnlyList<BattleJoinCombatant> SpawnCombatant(
        BattleUnit actingUnit,
        BattleState state,
        IReadOnlyList<string> characterIds,
        IReadOnlyList<GridPosition> impactedPositions)
    {
        var queue = new Queue<GridPosition>(impactedPositions);
        var spawnedUnits = new List<BattleJoinCombatant>();
        foreach (var characterId in characterIds)
        {
            if (queue.Count == 0)
            {
                queue.Enqueue(actingUnit.Position);
            }

            var gridPosition = queue.Dequeue();
            var position = state.IsOccupied(gridPosition)
                ? state.FindNearestEmptyPosition(gridPosition, SpawnSearchRadius)
                : gridPosition;
            if (position is null)
            {
                break;
            }

            var combatant = new BattleJoinCombatant
            {
                CharacterId = characterId,
                Team = actingUnit.Team,
                Position = position.Value,
                Facing = actingUnit.Facing,
            };

            spawnedUnits.Add(combatant);
            CreateBattleCombatant(state, combatant);
        }

        return spawnedUnits;
    }

    private void CreateBattleCombatant(BattleState state, BattleJoinCombatant combatant)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(combatant.CharacterId);
        var tempFactory = new EquipmentInstanceFactory();
        var character = CreateCombatantCharacter(state, combatant.CharacterId, tempFactory);
        state.AddUnit(CreateUnit(
            $"participant_{state.Units.Count + 1}_{combatant.CharacterId}",
            character,
            combatant.Team,
            combatant.Position,
            combatant.Facing));
    }

    private CharacterInstance? ResolvePartyCharacter(string characterId) =>
        State.Party.TryGetCharacter(characterId, out var character) ? character : null;



    public void ApplyHpChange(BattleUnit unit, double amount, string? kind)
    {
        ArgumentNullException.ThrowIfNull(unit);
        if (!double.IsFinite(amount) || amount == 0d)
        {
            return;
        }

        var resolved = ResolveAmount(unit.MaxHp, amount, kind);
        if (resolved > 0)
        {
            unit.RestoreHp(resolved);
        }
        else if (resolved < 0)
        {
            unit.TakeDamage(-resolved);
        }
    }

    public void ApplyMpChange(BattleUnit unit, double amount, string? kind)
    {
        ArgumentNullException.ThrowIfNull(unit);
        if (!double.IsFinite(amount) || amount == 0d)
        {
            return;
        }

        var resolved = ResolveAmount(unit.MaxMp, amount, kind);
        if (resolved > 0)
        {
            unit.RestoreMp(resolved);
        }
        else if (resolved < 0)
        {
            unit.DamageMp(-resolved);
        }
    }

    private static int ResolveAmount(int max, double amount, string? kind)
    {
        if (string.IsNullOrWhiteSpace(kind) ||
            string.Equals(kind, "flat", StringComparison.OrdinalIgnoreCase))
        {
            return (int)Math.Round(amount, MidpointRounding.AwayFromZero);
        }

        if (string.Equals(kind, "ratio", StringComparison.OrdinalIgnoreCase))
        {
            return (int)Math.Round(max * amount, MidpointRounding.AwayFromZero);
        }

        throw new ArgumentException($"Unknown amount kind '{kind}'; expected 'flat' or 'ratio'.", nameof(kind));
    }

    public void UnitsHpChange(
        BattleState state,
        IReadOnlyList<string> unitIds,
        IReadOnlyList<double> amounts,
        IReadOnlyList<string>? kinds = null) =>
        ApplyUnitsChange(state, unitIds, amounts, kinds, ApplyHpChange);

    public void UnitsMpChange(
        BattleState state,
        IReadOnlyList<string> unitIds,
        IReadOnlyList<double> amounts,
        IReadOnlyList<string>? kinds = null) =>
        ApplyUnitsChange(state, unitIds, amounts, kinds, ApplyMpChange);

    private static void ApplyUnitsChange(
        BattleState state,
        IReadOnlyList<string> unitIds,
        IReadOnlyList<double> amounts,
        IReadOnlyList<string>? kinds,
        Action<BattleUnit, double, string?> apply)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(unitIds);
        ArgumentNullException.ThrowIfNull(amounts);
        ArgumentNullException.ThrowIfNull(apply);
        if (unitIds.Count != amounts.Count ||
            kinds is not null && kinds.Count != unitIds.Count)
        {
            throw new ArgumentException(
                $"Unit changes expect unitIds/amounts/kinds of equal length, got {unitIds.Count}/{amounts.Count}/{kinds?.Count.ToString() ?? "omitted"}.");
        }

        for (var index = 0; index < unitIds.Count; index++)
        {
            var unit = state.TryGetUnit(unitIds[index]);
            if (unit is null)
            {
                var available = string.Join(", ", state.Units.Select(existing => existing.Id));
                throw new InvalidOperationException(
                    $"Battle unit '{unitIds[index]}' was not found. Available unit ids: {available}.");
            }

            apply(unit, amounts[index], kinds?[index]);
        }
    }

    private static BattleUnit CreateUnit(
        string id,
        CharacterInstance character,
        int team,
        GridPosition position,
        int facing) =>
        CreateUnit(id, character, team, position, facing <= 0 ? BattleFacing.Left : BattleFacing.Right);

    private static BattleUnit CreateUnit(
        string id,
        CharacterInstance character,
        int team,
        GridPosition position,
        BattleFacing facing) =>
        new(
            id,
            character,
            team,
            position,
            facing,
            hp: character.CurrentHp,
            mp: character.CurrentMp,
            rage: character.CurrentRage);
}
