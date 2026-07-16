using Game.Core.Battle;
using Game.Core.Definitions;
using Game.Core.Model;
using Game.Core.Model.Character;
using Game.Core.Model.Skills;
using Game.Godot.Assets;
using Game.Godot.Audio;
using Godot;
using static Godot.WebSocketPeer;
using GameRoot = Game.Godot.Game;

namespace Game.Godot.UI.Battle;

internal sealed class BattleSkillPresentationController(
    BattleScreen owner,
    BattleBoardView board,
    Control overlayRoot,
    PackedScene legendOverlayScene,
    Func<BattleEventPresenter> eventPresenter,
    Action<bool> setResolving,
    Action refreshActions,
    Action refreshAll)
{
    private const double SkillNameDelay = 0.1d;
    private const double SkillImpactDelay = 0.8d;
    private const double ImpactFloatDelay = 0.1d;
    private const BattleMovementPresentationMode MovementMode = BattleMovementPresentationMode.Step;
    private readonly BattleBoardView _board = board;
    private Presentation? _active;
    

    public bool IsResolving => _active is not null;

    public bool Schedule(BattleMessage message)
    {
        if (_active is not { } presentation) return false;
        switch (message)
        {
            case BattleFact { Kind: BattleFactKind.SkillCast } fact when
                fact.UnitId == presentation.ActingUnitId &&
                string.Equals(fact.SkillCast?.ResolvedSkillId ?? fact.Detail,
                    presentation.SkillCast.ResolvedSkillId, StringComparison.Ordinal):
                presentation.EnqueueSkillName(() => eventPresenter().PresentImmediate(fact));
                return true;
            case BattleCue { Kind: BattleCueKind.SpeechRequested } cue:
                presentation.EnqueueImpact(() => eventPresenter().PresentImmediate(cue));
                return true;
            case BattleFact { Kind: BattleFactKind.Damaged or BattleFactKind.BuffApplied or
                BattleFactKind.BuffResisted or BattleFactKind.BuffRemoved or BattleFactKind.Healed or
                BattleFactKind.Lifesteal or BattleFactKind.MpDamaged or BattleFactKind.MpRecovered or BattleFactKind.ActionGaugeChanged or
                BattleFactKind.SkillCooldownsReset or
                BattleFactKind.Rested or BattleFactKind.ItemUsed or
                BattleFactKind.ActionSkipped or BattleFactKind.SkillLeveledUp or BattleFactKind.CharacterLeveledUp } fact:
                presentation.EnqueueImpactFloat(() => eventPresenter().PresentImmediate(fact));
                return true;
            case BattleCue { Kind: BattleCueKind.FloatTextRequested } cue:
                presentation.EnqueueImpactFloat(() => eventPresenter().PresentImmediate(cue));
                return true;
            default:
                return false;
        }
    }

    public async Task PlayMoveAsync(BattleUnit unit, IReadOnlyList<GridPosition> path)
    {
        setResolving(true);
        refreshActions();
        try { await _board.PlayUnitMoveAsync(unit.Id, path, MovementMode); }
        finally { setResolving(false); }
        refreshAll();
    }

    public async Task PlaySkillAsync(
        BattleUnit unit,
        SkillInstance skill,
        BattleCommandResult<BattleActionResult> result)
    {
        var action = result.Value ?? throw new InvalidOperationException("Successful skill command has no action result.");
        _board.ApplyUnitFacing(unit.Id, unit.Facing);
        setResolving(true);
        refreshActions();
        _active = new Presentation(
            this,
            unit.Id,
            unit.Character.Name,
            unit.Character.Definition.Gender,
            AssetResolver.LoadCharacterPortrait(unit.Character),
            action.SkillCast ?? BattleSkillCastInfo.Create(skill, skill),
            action.ImpactedPositions);
        var presentationTask = _active.RunAsync();
        eventPresenter().AppendResult(result);
        try { await presentationTask; }
        finally
        {
            _active = null;
            setResolving(false);
            refreshActions();
        }
        refreshAll();
    }

    private async Task WaitAsync(double seconds)
    {
        if (seconds > 0d)
            await owner.ToSignal(owner.GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
    }

    private async Task ShowLegendOverlayAsync(string unitName, Texture2D? portrait, BattleSkillCastInfo skill)
    {
        if (legendOverlayScene.Instantiate() is not BattleLegendOverlay overlay)
            throw new InvalidOperationException("Battle legend overlay scene root must be BattleLegendOverlay.");
        overlayRoot.AddChild(overlay);
        await overlay.PlayAsync(unitName, portrait, skill, Colors.Magenta);
    }

    private sealed class Presentation(
        BattleSkillPresentationController controller,
        string actingUnitId,
        string actingUnitName,
        CharacterGender gender,
        Texture2D? portrait,
        BattleSkillCastInfo skillCast,
        IReadOnlyList<GridPosition> impacts)
    {
        private readonly List<Action> _skillNameActions = [];
        private readonly List<Action> _impactActions = [];
        private readonly List<Action> _impactFloatActions = [];
        public string ActingUnitId { get; } = actingUnitId;
        public BattleSkillCastInfo SkillCast { get; } = skillCast;
        public void EnqueueSkillName(Action action) => _skillNameActions.Add(action);
        public void EnqueueImpact(Action action) => _impactActions.Add(action);
        public void EnqueueImpactFloat(Action action) => _impactFloatActions.Add(action);

        public async Task RunAsync()
        {
            if (SkillCast.IsLegend)
            {
                PlayLegendIntroSfx(gender);
                if (!string.IsNullOrWhiteSpace(SkillCast.ScreenEffectAnimationId))
                    await controller.ShowLegendOverlayAsync(actingUnitName, portrait, SkillCast);
            }
            controller._board.PlayAttack(ActingUnitId);
            await controller.WaitAsync(SkillNameDelay);
            Flush(_skillNameActions);
            await controller.WaitAsync(SkillImpactDelay - SkillNameDelay);
            if (!string.IsNullOrWhiteSpace(SkillCast.AudioId)) AudioManager.Instance.PlaySfx(SkillCast.AudioId);
            var impactTask = controller._board.PlaySkillImpactAsync(impacts, SkillCast.ImpactAnimationId);
            Flush(_impactActions);
            await controller.WaitAsync(ImpactFloatDelay);
            Flush(_impactFloatActions);
            await impactTask;
        }

        private static void Flush(List<Action> actions)
        {
            foreach (var action in actions) action();
            actions.Clear();
        }

        private static void PlayLegendIntroSfx(CharacterGender value)
        {
            AudioManager.Instance.PlaySfx(PickRandom(value == CharacterGender.Female
                ? GameRoot.Config.LegendFemaleVoiceSfxIds : GameRoot.Config.LegendMaleVoiceSfxIds));
            AudioManager.Instance.PlaySfx(PickRandom(GameRoot.Config.LegendEffectSfxIds));
        }

        private static string PickRandom(IReadOnlyList<string> ids) => ids[Random.Shared.Next(ids.Count)];
    }
}
