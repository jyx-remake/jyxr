using Game.Core.Definitions;
using Game.Godot.Assets;
using Godot;

namespace Game.Godot.Map;

internal static class MapEntityPresentation
{
	private const string OverflowResourceTag = "map-marker-overflow";
	private const string NativeTownPrefix = "town.native.";
	private const string CityTownPrefix = "town.city.";
	private const string TownPrefix = "town.";

	public static string ResolveLocationName(MapLocationDefinition location) =>
		location.Name ?? AssetResolver.ResolveCharacterName(location.Id);

	public static MapEntityAvatarPresentation ResolveAvatar(
		Texture2D? defaultTexture,
		MapLocationDefinition location,
		MapEventDefinition? mapEvent)
	{
		var image = mapEvent is null
			? location.NoEventImage
			: mapEvent.Image ?? location.Picture;
		if (image is not null)
		{
			var texture = AssetResolver.LoadTexture(image);
			if (texture is not null)
			{
				return new MapEntityAvatarPresentation(
					texture,
					HasOverflowTag(image),
					UsesNativeSize(image),
					UsesCompactTownSize(image));
			}
		}

		var portraitReference = AssetResolver.ResolveCharacterPortraitReferenceByCharacterId(location.Id);
		var portrait = AssetResolver.LoadTexture(portraitReference);
		if (portrait is not null)
		{
			return new MapEntityAvatarPresentation(portrait, false, false, false);
		}

		return new MapEntityAvatarPresentation(
			defaultTexture,
			false,
			false,
			true);
	}

	private static bool UsesNativeSize(string resourceId)
	{
		var normalized = resourceId.Trim();
		return normalized.StartsWith(NativeTownPrefix, StringComparison.OrdinalIgnoreCase) ||
			normalized.StartsWith(CityTownPrefix, StringComparison.OrdinalIgnoreCase);
	}

	private static bool UsesCompactTownSize(string resourceId)
	{
		var normalized = resourceId.Trim();
		return normalized.StartsWith(TownPrefix, StringComparison.OrdinalIgnoreCase) &&
			!UsesNativeSize(normalized);
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

internal readonly record struct MapEntityAvatarPresentation(
	Texture2D? Texture,
	bool UseOverflow,
	bool UseNativeSize,
	bool UseCompactTownSize);
