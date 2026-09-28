using Game.Core.Definitions;
using Game.Godot.Resources;
using Godot;

namespace Game.Godot.Map;

internal static class MapEntityPresentation
{
	private const string OverflowResourceTag = "map-marker-overflow";

	public static string ResolveLocationName(MapLocationDefinition location) =>
		location.Name ?? AssetResolver.ResolveCharacterName(location.Id);

	public static MapEntityAvatarPresentation ResolveAvatar(
		Texture2D? defaultTexture,
		MapLocationDefinition location,
		MapEventDefinition? mapEvent)
	{
		if (mapEvent is null)
		{
			if (location.NoEventImage is null)
			{
				return new MapEntityAvatarPresentation(defaultTexture, false);
			}

			var texture = AssetResolver.LoadTexture(location.NoEventImage);
			return texture is null
				? new MapEntityAvatarPresentation(defaultTexture, false)
				: new MapEntityAvatarPresentation(texture, HasOverflowTag(location.NoEventImage));
		}

		var image = mapEvent.Image ?? location.Picture;
		if (image is not null)
		{
			var texture = AssetResolver.LoadTexture(image);
			return texture is null
				? new MapEntityAvatarPresentation(defaultTexture, false)
				: new MapEntityAvatarPresentation(texture, HasOverflowTag(image));
		}

		var portraitReference = AssetResolver.ResolveCharacterPortraitReferenceByCharacterId(location.Id);
		return new MapEntityAvatarPresentation(
			AssetResolver.LoadTexture(portraitReference) ?? defaultTexture,
			false);
	}

	private static bool HasOverflowTag(string resourceId)
	{
		var normalizedResourceId = resourceId.Trim();
		return !normalizedResourceId.Contains('/') &&
			Game.ContentRepository.TryGetResource(normalizedResourceId, out var resource) &&
			resource.Tags.Contains(OverflowResourceTag, StringComparer.Ordinal);
	}

	public static string BuildTooltipText(
		(string MapId, MapLocationDefinition Location, MapEventDefinition? Event) location)
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

internal readonly record struct MapEntityAvatarPresentation(Texture2D? Texture, bool UseOverflow);
