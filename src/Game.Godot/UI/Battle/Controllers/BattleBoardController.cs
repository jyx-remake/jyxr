using Game.Core.Battle;
using Game.Presentation.Battle;
using Game.Core.Affix;
using Game.Core.Model;
using Game.Core.Model.Skills;
using Game.Godot.Assets;
using Godot;

namespace Game.Godot.UI.Battle;

internal sealed class BattleBoardController(
    BattleBoardView board,
    BattlePresenter presenter,
    int playerTeam,
    Func<BattleState?> stateProvider,
    Func<IReadOnlyDictionary<GridPosition, int>> reachablePositionsProvider)
{
    private const int CellWidth = 144;
    private const int CellHeight = 144;
    private static readonly Color DefaultCellColor = new(0.2f, 0.2f, 0.2f, 0.2f);
    private static readonly Color MoveColor = new(0.2f, 0.6f, 1f, 0.35f);
    private static readonly Color ActingColor = new(1f, 1f, 0.2f, 0.5f);
    private static readonly Color PlayerColor = new(0.2f, 1f, 0.2f, 0.25f);
    private static readonly Color EnemyColor = new(1f, 0.2f, 0.2f, 0.25f);
    private static readonly Color TargetColor = new(1f, 0.95f, 0.55f, 0.8f);
    private static readonly Color PossibleImpactColor = new(0.2f, 0.2f, 0.2f, 0.5f);
    private static readonly Color ActualImpactColor = new(1f, 0.3f, 0.2f, 1f);
    public void Commit(BattleInteractionState interaction)
    {
        var state = stateProvider();
        if (state is null) return;

        RenderInteraction(interaction);
        RefreshUnits(state);
    }

    public void RenderInteraction(BattleInteractionState interaction)
    {
        var state = stateProvider();
        if (state is null) return;
        var highlights = ResolveHighlights(state, interaction);
        var cells = presenter.CreateCells(state)
            .Select(cell => new BattleBoardCellVisual(
                cell.Position,
                cell.Label,
                ResolveCellColor(cell, highlights),
                CanClick(cell.Position, highlights),
                HasOverlay(cell, highlights)))
            .ToArray();
        board.RenderGrid(state.Grid.Width, state.Grid.Height, CellWidth, CellHeight, 6, cells);
    }

    private void RefreshUnits(BattleState state)
    {
        var actingUnitId = state.CurrentAction?.ActingUnitId;
        board.RenderUnits(state.Units.Select(unit => new BattleBoardUnitVisual(
            unit.Id,
            unit.Character.Name,
            unit.Position,
            unit.Facing,
            AssetResolver.LoadCombatantAnimation(unit.Character),
            unit.Id == actingUnitId,
            unit.IsAlive,
            unit.Team == playerTeam,
            unit.Hp,
            unit.MaxHp,
            unit.Mp,
            unit.MaxMp,
            unit.Rage,
            (int)Math.Round(unit.ActionGauge, MidpointRounding.AwayFromZero),
            AssetResolver.LoadCharacterPortrait(unit.Character),
            unit.GetActiveBuffs().Select(static buff => new BattleBoardBuffVisual(
                buff.Definition.Name, buff.Definition.IsDebuff, buff.Level, buff.RemainingTurns)).ToArray()))
            .ToArray());
    }

    private Highlights ResolveHighlights(BattleState state, BattleInteractionState interaction)
    {
        var actingUnit = BattlePresenter.TryGetActingUnit(state);
        if (actingUnit is null) return Highlights.Empty;
        return interaction.Kind switch
        {
            BattleFlowStateKind.SelectingMove => new Highlights(
                move: reachablePositionsProvider().Keys.ToHashSet()),
            BattleFlowStateKind.SelectingSkillTarget when interaction.SelectedSkill is { } skill =>
                ResolveSkillHighlights(state, actingUnit, skill, interaction.HoveredPosition),
            BattleFlowStateKind.SelectingItemTarget => new Highlights(items: ResolveItemTargets(state, actingUnit)),
            _ => Highlights.Empty,
        };
    }

    private static Highlights ResolveSkillHighlights(
        BattleState state,
        BattleUnit unit,
        SkillInstance skill,
        GridPosition? hoveredPosition)
    {
        var castSize = BattleSkillTargeting.ResolveEffectiveCastSize(unit, skill);
        var impactSize = BattleSkillTargeting.ResolveEffectiveImpactSize(unit, skill);
        var targets = BattleSkillTargeting.EnumerateCastTargets(unit.Position, castSize, state.Grid);
        var possible = targets.SelectMany(target => BattleEngine.GetImpactPositions(
                unit.Position, target, skill.ImpactType, impactSize).Where(state.Grid.Contains))
            .ToHashSet();
        var actual = hoveredPosition is { } hovered && targets.Contains(hovered)
            ? BattleEngine.GetImpactPositions(unit.Position, hovered, skill.ImpactType, impactSize)
                .Where(state.Grid.Contains).ToHashSet()
            : [];
        return new Highlights(skill: targets, possible: possible, actual: actual);
    }

    private static IReadOnlySet<GridPosition> ResolveItemTargets(BattleState state, BattleUnit unit) =>
        !unit.HasTrait(TraitId.CanUseItemOnAlly)
            ? new HashSet<GridPosition> { unit.Position }
            : state.Units.Where(candidate => candidate.IsAlive && candidate.Team == unit.Team &&
                    candidate.Position.ManhattanDistanceTo(unit.Position) <= 2)
                .Select(static candidate => candidate.Position).ToHashSet();

    private static bool CanClick(GridPosition position, Highlights value) =>
        value.Move.Contains(position) || value.Skill.Contains(position) || value.Items.Contains(position);

    private static Color ResolveCellColor(BattleCellView cell, Highlights value)
    {
        if (value.Actual.Contains(cell.Position)) return ActualImpactColor;
        if (value.Skill.Contains(cell.Position)) return TargetColor;
        if (cell.IsActing) return ActingColor;
        if (value.Move.Contains(cell.Position)) return MoveColor;
        if (value.Items.Contains(cell.Position)) return TargetColor;
        if (cell.HasUnit) return cell.IsPlayerUnit ? PlayerColor : EnemyColor;
        return value.Possible.Contains(cell.Position) ? PossibleImpactColor : DefaultCellColor;
    }

    private static bool HasOverlay(BattleCellView cell, Highlights value) =>
        cell.HasUnit || cell.IsActing || value.Move.Contains(cell.Position) || value.Items.Contains(cell.Position) ||
        value.Skill.Contains(cell.Position) || value.Possible.Contains(cell.Position) || value.Actual.Contains(cell.Position);

    private sealed record Highlights(
        IReadOnlySet<GridPosition>? move = null,
        IReadOnlySet<GridPosition>? skill = null,
        IReadOnlySet<GridPosition>? possible = null,
        IReadOnlySet<GridPosition>? actual = null,
        IReadOnlySet<GridPosition>? items = null)
    {
        public static Highlights Empty { get; } = new();
        public IReadOnlySet<GridPosition> Move { get; } = move ?? new HashSet<GridPosition>();
        public IReadOnlySet<GridPosition> Skill { get; } = skill ?? new HashSet<GridPosition>();
        public IReadOnlySet<GridPosition> Possible { get; } = possible ?? new HashSet<GridPosition>();
        public IReadOnlySet<GridPosition> Actual { get; } = actual ?? new HashSet<GridPosition>();
        public IReadOnlySet<GridPosition> Items { get; } = items ?? new HashSet<GridPosition>();
    }
}
