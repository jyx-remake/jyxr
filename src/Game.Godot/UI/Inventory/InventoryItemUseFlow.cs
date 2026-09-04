using Game.Application;
using Game.Application.Formatters;
using Game.Core.Definitions;
using Game.Core.Model;
using Godot;

namespace Game.Godot.UI;

/// <summary>
/// Shared out-of-inventory item use flow: validates the target, confirms
/// partially applicable effects, plays the story-presentation handoff for
/// run_story props, and surfaces the result. Returns true when the item was
/// actually used; false when the use was rejected or failed (the error is
/// already surfaced). Used by the target selection panel and by the
/// auto-target path (role_key pinned or inventory-level effects), so both
/// entry points behave identically.
/// </summary>
internal static class InventoryItemUseFlow
{
	public static async Task<bool> UseAsync(InventoryEntry entry, string characterId, Action? onStoryStarted)
	{
		ArgumentNullException.ThrowIfNull(entry);
		ArgumentException.ThrowIfNullOrWhiteSpace(characterId);

		var character = Game.State.Party.GetMember(characterId);
		var candidate = Game.ItemUseService.AnalyzeTarget(entry, character);
		if (!candidate.CanUse)
		{
			UIRoot.Instance.ShowSuggestion(candidate.Reason);
			return false;
		}

		var acceptPartialEffects = false;
		if (candidate.RequiresConfirmation)
		{
			var skippedEffectLines = candidate.SkippedEffects
				.Select(effect => ItemUseEffectFormatter.FormatCn(effect, Game.ContentRepository)
					.Replace('\n', ' '));
			var confirmationText =
				$"以下效果不会生效：\n{string.Join("\n", skippedEffectLines.Select(line => $"• {line}"))}\n\n仍要使用【{entry.Definition.Name}】吗？";
			acceptPartialEffects = await UIRoot.Instance.ShowConfirmAsync(
				confirmationText,
				ConfirmDialogTone.Warning);
			if (!acceptPartialEffects)
			{
				return false;
			}
		}

		var runsStory = entry.Definition.UseEffects is [RunStoryItemUseEffectDefinition];
		if (runsStory)
		{
			onStoryStarted?.Invoke();
			UIRoot.Instance.SetStoryPresentationActive(true);
		}

		try
		{
			var result = acceptPartialEffects
				? await Game.ItemUseService.UseAsync(entry, characterId, true)
				: await Game.ItemUseService.UseAsync(entry, characterId);
			if (!result.Success)
			{
				UIRoot.Instance.ShowSuggestion(result.Message);
				return false;
			}

			if (!result.Message.IsWhiteSpace())
			{
				UIRoot.Instance.ShowToast(result.Message);
			}
			return true;
		}
		catch (Exception exception)
		{
			Game.Logger.Error("Using inventory item failed.", exception);
			UIRoot.Instance.ShowSuggestion(exception.Message);
			return false;
		}
		finally
		{
			if (runsStory && GodotObject.IsInstanceValid(UIRoot.Instance))
			{
				UIRoot.Instance.SetStoryPresentationActive(false);
			}
		}
	}
}
