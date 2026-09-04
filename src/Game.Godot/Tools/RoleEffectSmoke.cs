using Game.Application.Mods;
using Game.Core.Battle;
using Game.Core.Model;
using Godot;

namespace Game.Godot.Tools;

/// <summary>
/// Headless check for battle auras (role_effect): equipping the 独咕求败
/// title must resolve the gh_jq aura, and its animation library must load.
/// Invoked with: godot --headless --path . -s res://src/Game.Godot/Tools/RoleEffectSmoke.cs
/// </summary>
public partial class RoleEffectSmoke : SceneTree
{
	public override void _Initialize() => CallDeferred(nameof(RunSmoke));

	private async void RunSmoke()
	{
		try
		{
			var projectRoot = ProjectSettings.GlobalizePath("res://");
			var dataRoot = ProjectDataRoot.FromPath(projectRoot);
			var mods = new ModRegistry(dataRoot).DiscoverMods();
			var loadout = new ModLoadoutResolver(mods).Resolve("xmjh", []);

			GameRuntimeBootstrap.Initialize(loadout, this);
			await ToSignal(this, SceneTree.SignalName.ProcessFrame);

			var heroId = Party.HeroCharacterId;
			Game.CharacterService.LearnTitle(heroId, "独咕求败");
			Game.CharacterService.EquipTitle(heroId, "独咕求败");
			var hero = Game.State.Party.GetMember(heroId);

			var aura = RoleEffectResolver.Resolve(hero);
			if (aura is null || aura.AnimationId != "gh_jq")
			{
				throw new InvalidOperationException(
					$"独咕求败 should resolve the gh_jq aura, got '{aura?.AnimationId}'.");
			}

			var library = Assets.AssetResolver.LoadSkillAnimation(aura.AnimationId);
			if (library is null || library.GetAnimationList().Count == 0)
			{
				throw new InvalidOperationException($"Aura animation '{aura.AnimationId}' did not load.");
			}

			GD.Print($"ROLEEFFECT_SMOKE animation={aura.AnimationId} clips={library.GetAnimationList().Count}");

			var viewScene = GD.Load<PackedScene>("res://scenes/ui/battle/battle_unit_view.tscn")
				?? throw new InvalidOperationException("battle_unit_view.tscn could not be loaded.");
			var view = (UI.Battle.BattleUnitView)viewScene.Instantiate();
			Root.AddChild(view);
			await ToSignal(this, SceneTree.SignalName.ProcessFrame);
			view.Configure(new UI.Battle.BattleBoardUnitVisual(
				"unit_1",
				hero.Name,
				new GridPosition(0, 0),
				BattleFacing.Right,
				null,
				false,
				true,
				true,
				100,
				100,
				100,
				100,
				0,
				0,
				null,
				[],
				null,
				new UI.Battle.BattleRoleEffectVisual(aura.AnimationId, aura.Transparency, aura.Order)));
			await ToSignal(this, SceneTree.SignalName.ProcessFrame);
			var auraRoot = view.FindChildren("*", "Node2D", true, false)
				.FirstOrDefault(node => node.Name == "AuraRoot");
			var auraPlayer = auraRoot?.GetNodeOrNull<AnimationPlayer>("AuraPlayer");
			if (auraRoot is null || auraPlayer is null || !auraPlayer.IsPlaying())
			{
				throw new InvalidOperationException("Aura nodes were not attached and playing.");
			}

			if (auraRoot.GetParent() != view.GetNode<Node2D>("%AnimationSlot"))
			{
				throw new InvalidOperationException("Aura must live in the animation slot space.");
			}

			var bodySprite = view.GetNode<Sprite2D>("%Sprite");
			var auraSprite = auraRoot.GetNode<Sprite2D>("Sprite");
			if (auraSprite.Centered)
			{
				throw new InvalidOperationException("Aura sprite must be uncentered like the body sprite.");
			}
			// The converted gh_jq track bakes offset (-118, -135.6): if the
			// player resolves its Sprite track, the aura sprite carries it.
			var applied = auraSprite.Offset;
			GD.Print($"ROLEEFFECT_SMOKE aura_offset={applied}");
			if (applied.DistanceTo(new Vector2(-118f, -135.6f)) > 2f)
			{
				throw new InvalidOperationException($"Aura track did not apply: offset={applied}.");
			}

			if (applied.DistanceTo(bodySprite.Offset) > 150f)
			{
				throw new InvalidOperationException("Aura is off the character.");
			}

			GD.Print("ROLEEFFECT_SMOKE attach=ok");
			Quit(0);
		}
		catch (Exception exception)
		{
			GD.PrintErr($"ROLEEFFECT_SMOKE_FAILED {exception}");
			Quit(1);
		}
	}
}
