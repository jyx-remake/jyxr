using Game.Core.Battle;
using Game.Core.Model;
using Game.Core.Model.Character;
using Game.Core.Model.Skills;
using Game.Presentation.Battle;
using GameRoot = Game.Godot.Game;

namespace Game.Godot.UI.Battle;

internal sealed class BattleFlowContext : IBattleFlowContext
{
	private readonly BattleScreen _screen;
	private readonly IBattleAgent _battleAgent;
	private bool _autoBattleEnabled;
	private bool _surrenderRequested;

	public BattleFlowContext(BattleScreen screen, BattleState state)
	{
		_screen = screen ?? throw new ArgumentNullException(nameof(screen));
		State = state ?? throw new ArgumentNullException(nameof(state));
		Engine = new BattleEngine(
			buffResolver: buffId => GameRoot.ContentRepository.GetBuff(buffId),
			legendSkillsProvider: () => GameRoot.ContentRepository.GetLegendSkills(),
			skillMaxLevelResolver: GameRoot.SkillMaxLevelPolicy.GetMaxLevel,
			characterGrowTemplateResolver: character =>
				GameRoot.ContentRepository.GetGrowTemplate(
					character.GrowTemplateId ?? CharacterExperienceProgression.DefaultGrowTemplateId),
			characterMaxLevelResolver: _ => GameRoot.Config.MaxLevel,
			battleExperienceEligibilityResolver: unit => unit.Team == PlayerTeam);
		_battleAgent = new BasicEnemyBattleAgent(
			new BattleTurnCandidateGenerator(Engine),
			new BattleAiPolicyResolver(GameRoot.SkillMaxLevelPolicy.GetMaxLevel));
		_screen.BindState(State);
	}

	public BattleState State { get; }
	public BattleEngine Engine { get; }
	public bool IsAutoBattleEnabled => _autoBattleEnabled;
	public bool IsSurrenderRequested => _surrenderRequested;
	public int PlayerTeam => GameRoot.Config.BattlePlayerTeam;

	public BattleUnit? ActingUnit => BattlePresenter.TryGetActingUnit(State);

	public void RenderInteraction(BattleInteractionState interaction) =>
		_screen.RenderInteraction(interaction);

	public void CommitBattleStateToView(BattleInteractionState interaction) =>
		_screen.CommitBattleStateToView(interaction);

	public void BeginActionPresentation(BattleInteractionState interaction) =>
		_screen.BeginActionPresentation(interaction);

	public void SetAutoBattleEnabled(bool enabled)
	{
		_autoBattleEnabled = enabled;
	}

	public void ToggleAutoBattle()
	{
		_autoBattleEnabled = !_autoBattleEnabled;
		_screen.PresentAutoBattleSetting(_autoBattleEnabled);
	}

	public void RequestSurrender()
	{
		if (_surrenderRequested)
		{
			return;
		}

		_surrenderRequested = true;
		_autoBattleEnabled = false;
		_screen.AppendLog("我方选择投降。");
		_screen.RefreshGlobalControls();
	}

	public IReadOnlyDictionary<GridPosition, int> GetReachablePositions()
	{
		var actingUnit = ActingUnit;
		return actingUnit is null
			? new Dictionary<GridPosition, int>()
			: Engine.GetReachablePositions(State, actingUnit.Id);
	}

	public BattleUnit AdvanceTimelineToNextAction()
	{
		var result = Engine.AdvanceUntilNextAction(State);
		_screen.AppendMessages(result.Messages);
		return result.Value ?? throw new InvalidOperationException("Timeline command returned no acting unit.");
	}

	public BattleTurnPlan DecideAutomatedTurn(BattleUnit actingUnit) =>
		_battleAgent.Decide(State, actingUnit.Id);

	public void AppendTurnLog(BattleUnit unit, bool isAutomated)
	{
		var text = isAutomated && unit.Team == PlayerTeam
			? $"轮到 {unit.Character.Name} 自动行动。"
			: $"轮到 {unit.Character.Name} 行动。";
		_screen.AppendLog(text);
	}

	public void AppendLog(string text) => _screen.AppendLog(text);

	public async Task<bool> ExecuteMoveAsync(BattleUnit actingUnit, GridPosition destination)
	{
		var result = Engine.MoveTo(State, actingUnit.Id, destination);
		var movementPath = result.Success && State.CurrentAction is not null
			? State.CurrentAction.MovementTrace.ToArray()
			: Array.Empty<GridPosition>();
		_screen.AppendResult(result);
		if (!result.Success)
		{
			return false;
		}

		await _screen.PlayMoveAsync(actingUnit, movementPath);
		return true;
	}

	public async Task<bool> ExecuteSkillAsync(BattleUnit actingUnit, SkillInstance skill, GridPosition target)
	{
		var result = Engine.CastSkill(State, actingUnit.Id, skill, target);
		if (!result.Success)
		{
			_screen.AppendResult(result);
			return false;
		}

		await _screen.PlaySkillAsync(actingUnit, skill, result);
		return true;
	}

	public bool ExecuteItem(BattleUnit actingUnit, InventoryEntry item, string targetUnitId)
	{
		var result = Engine.UseItem(State, actingUnit.Id, item.Definition, targetUnitId);
		if (result.Success)
		{
			_screen.ApplyActingUnitFacing(actingUnit);
		}
		_screen.AppendResult(result);
		if (!result.Success)
		{
			return false;
		}

		GameRoot.InventoryService.RemoveItem(item.Definition);
		return true;
	}

	public bool ExecuteRest(BattleUnit actingUnit)
	{
		var result = Engine.Rest(State, actingUnit.Id);
		_screen.AppendResult(result);
		return result.Success;
	}

	public bool ExecuteEndAction(BattleUnit actingUnit)
	{
		var result = Engine.EndAction(State, actingUnit.Id);
		_screen.AppendResult(result);
		return result.Success;
	}

	public async Task<bool> RollbackMoveAsync(BattleUnit actingUnit)
	{
		if (State.CurrentAction is not
			{ ActingUnitId: var actingUnitId, HasMoved: true, HasCommittedMainAction: false } ||
			!string.Equals(actingUnitId, actingUnit.Id, StringComparison.Ordinal))
		{
			return false;
		}

		var result = Engine.RollbackMove(State, actingUnit.Id);
		_screen.AppendResult(result);
		if (!result.Success)
		{
			return false;
		}

		await _screen.PlayMoveRollbackAsync(actingUnit);
		return true;
	}

	public SkillInstance? ResolveDefaultSkill(BattleUnit actingUnit)
	{
		var options = CreateSkillOptions(actingUnit);
		if (!string.IsNullOrWhiteSpace(actingUnit.LastUsedSkillId))
		{
			var lastUsed = options.FirstOrDefault(view =>
				view.IsAvailable && view.Skill.Id == actingUnit.LastUsedSkillId);
			if (lastUsed is not null)
			{
				return lastUsed.Skill;
			}
		}

		return options.FirstOrDefault(static view => view.IsAvailable)?.Skill;
	}

	public IReadOnlyList<BattleSkillOptionView> CreateSkillOptions(BattleUnit actingUnit) =>
		_screen.Presenter.CreateSkillList(State, Engine, actingUnit);

	public SkillInstance? ResolveSkill(BattleUnit actingUnit, string? skillId)
	{
		if (string.IsNullOrWhiteSpace(skillId))
		{
			return null;
		}

		return BattleSkillCatalog.CollectSelectableSkills(actingUnit)
			.FirstOrDefault(skill => string.Equals(skill.Id, skillId, StringComparison.Ordinal));
	}

	public async Task<InventoryEntry?> ShowItemPanelAsync() =>
		await _screen.ShowItemPanelAsync();

	public void ShowStatusPanel() => _screen.ShowStatusPanel();

	public bool TryGetBattleResult(out bool isWin)
	{
		if (_surrenderRequested)
		{
			isWin = false;
			return true;
		}

		var outcome = BattleOutcomeEvaluator.Evaluate(State);
		if (outcome.Kind == BattleOutcomeKind.Ongoing)
		{
			isWin = false;
			return false;
		}

		isWin = outcome.Kind == BattleOutcomeKind.Winner && outcome.WinningTeam == PlayerTeam;
		return true;
	}

	public Task CompleteBattleAsync(bool isWin) => _screen.ShowBattleEndedAsync(isWin);
}
