using Game.Core.Definitions;
using Game.Godot.Assets;
using Godot;

namespace Game.Godot.Map;

internal static class MapEntityPresentation
{
	public static string ResolveLocationName(MapLocationDefinition location) =>
		location.Name ?? AssetResolver.ResolveCharacterName(location.Id);

	public static Texture2D? ResolveAvatarTexture(
		Texture2D? defaultTexture,
		MapLocationDefinition location,
		MapEventDefinition? mapEvent)
	{
		if (mapEvent is null)
		{
			return defaultTexture;
		}

		var image = mapEvent.Image ?? location.Picture;
		if (image is not null)
		{
			return AssetResolver.LoadTextureResource(image) ?? defaultTexture;
		}

		return AssetResolver.LoadCharacterPortraitByCharacterId(location.Id) ?? defaultTexture;
	}

	public static string BuildTooltipText(
		(string MapId, MapLocationDefinition Location, MapEventDefinition? Event, int EventIndex) location)
	{
		var description = !string.IsNullOrWhiteSpace(location.Event?.Description)
			? location.Event.Description
			: location.Location.Description ?? string.Empty;
		var consumedTimeSlots = Game.MapService.PreviewInteractionConsumedTimeSlots(location);
		if (consumedTimeSlots <= 0)
		{
			return description;
		}

		var costLine = $"[color=red]耗时：{FormatConsumedTimeSlots(consumedTimeSlots)}[/color]";
		return string.IsNullOrWhiteSpace(description)
			? costLine
			: $"{description}\n{costLine}";
	}

	private static string FormatConsumedTimeSlots(int timeSlots)
	{
		var days = timeSlots / 12;
		var remainingTimeSlots = timeSlots % 12;
		if (days <= 0)
		{
			return $"{remainingTimeSlots}个时辰";
		}

		return remainingTimeSlots <= 0
			? $"{days}天"
			: $"{days}天{remainingTimeSlots}个时辰";
	}
}
