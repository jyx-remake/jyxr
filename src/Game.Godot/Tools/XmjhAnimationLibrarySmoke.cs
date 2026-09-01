using Godot;
using global::Game.Godot.Assets;

namespace Game.Godot.Tools;

/// <summary>
/// Headless validation for the generated XMJH AnimationLibrary resources.
/// Invoked with: godot --headless --path . -s res://src/Game.Godot/Tools/XmjhAnimationLibrarySmoke.cs
/// </summary>
public partial class XmjhAnimationLibrarySmoke : SceneTree
{
	private const string RootPath = "res://mods/xmjh/resources/converted/AnimationLibraries";
	private const string PckPath = "res://mods/xmjh/resources/pck/xmjh.pck";
	private static readonly string[] Categories = ["combatant", "skill"];

	public override void _Initialize()
	{
		CallDeferred(nameof(LoadPackAndValidate));
	}

	private void LoadPackAndValidate()
	{
		var absolutePckPath = ProjectSettings.GlobalizePath(PckPath);
		if (!global::Godot.FileAccess.FileExists(absolutePckPath))
		{
			GD.PrintErr($"XMJH_ANIMATION_SMOKE_ERROR Missing PCK: {absolutePckPath}");
			Quit(1);
			return;
		}

		if (!ProjectSettings.LoadResourcePack(absolutePckPath, replaceFiles: true))
		{
			GD.PrintErr($"XMJH_ANIMATION_SMOKE_ERROR Could not load PCK: {absolutePckPath}");
			Quit(1);
			return;
		}

		RunValidation();
	}

	private void RunValidation()
	{
		var libraryCount = 0;
		var animationCount = 0;
		var trackCount = 0;
		var keyCount = 0;
		var errors = new List<string>();

		foreach (var category in Categories)
		{
			var directoryPath = $"{RootPath}/{category}";
			using var directory = DirAccess.Open(directoryPath);
			if (directory is null)
			{
				errors.Add($"Missing animation directory: {directoryPath}");
				continue;
			}

			foreach (var fileName in directory.GetFiles())
			{
				if (!fileName.EndsWith(".tres", StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				var resourcePath = $"{directoryPath}/{fileName}";
				if (!ResourceLoader.Exists(resourcePath))
				{
					errors.Add($"ResourceLoader.Exists returned false: {resourcePath}");
					continue;
				}

				var library = ResourceLoader.Load<AnimationLibrary>(resourcePath);
				if (library is null)
				{
					errors.Add($"Could not load AnimationLibrary: {resourcePath}");
					continue;
				}

				libraryCount++;
				var animations = library.GetAnimationList();
				if (animations.Count == 0)
				{
					errors.Add($"AnimationLibrary has no animations: {resourcePath}");
					continue;
				}

				foreach (var animationName in animations)
				{
					var animation = library.GetAnimation(animationName);
					if (animation is null || animation.Length <= 0 || !double.IsFinite(animation.Length))
					{
						errors.Add($"Invalid animation '{animationName}' in {resourcePath}");
						continue;
					}

					animationCount++;
					var animationTracks = animation.GetTrackCount();
					if (animationTracks == 0)
					{
						errors.Add($"Animation '{animationName}' has no tracks in {resourcePath}");
						continue;
					}

					for (var trackIndex = 0; trackIndex < animationTracks; trackIndex++)
					{
						trackCount++;
						var keys = animation.TrackGetKeyCount(trackIndex);
						if (keys == 0)
						{
							errors.Add($"Animation '{animationName}' track {trackIndex} has no keys in {resourcePath}");
							continue;
						}

						keyCount += keys;
					}
				}
			}
		}

		var representativeCombatant = ResourceLoader.Load<AnimationLibrary>(
			$"{RootPath}/combatant/baihu.tres");
		if (representativeCombatant is null)
		{
			errors.Add("Could not load representative combatant animation: baihu");
		}
		else if (!representativeCombatant.HasMeta("hide_system_shadow") ||
			!representativeCombatant.GetMeta("hide_system_shadow").AsBool())
		{
			errors.Add("Representative combatant animation did not preserve shadow=true metadata: baihu");
		}

		if (ResourceLoader.Load<AnimationLibrary>($"{RootPath}/skill/jn1.tres") is null)
		{
			errors.Add("Could not load representative skill animation: jn1");
		}

		GD.Print($"XMJH_ANIMATION_SMOKE libraries={libraryCount} animations={animationCount} tracks={trackCount} keys={keyCount} errors={errors.Count}");
		foreach (var error in errors.Take(20))
		{
			GD.PrintErr($"XMJH_ANIMATION_SMOKE_ERROR {error}");
		}

		Quit(errors.Count == 0 ? 0 : 1);
	}
}
