using Game.Core.Model;

namespace Game.Godot.Map;

internal static class MapTimeLighting
{
	private static readonly float[] LightOpacities =
	[
		0.4f, 0.4f, 0.5f, 0.5f, 0.6f, 0.7f,
		1.0f, 1.0f, 1.0f, 0.8f, 0.6f, 0.4f,
	];

	public static float GetDimAlpha(TimeSlot timeSlot)
	{
		var index = (int)timeSlot;
		if (index < 0 || index >= LightOpacities.Length)
		{
			throw new ArgumentOutOfRangeException(nameof(timeSlot), timeSlot, "Unsupported time slot.");
		}

		return 1f - LightOpacities[index];
	}

	/// <summary>
	/// Ambient opacity of a lit backdrop for the time slot. This is the legacy
	/// <c>timeOpacity[hour / 2]</c> table: story backdrops are tinted toward
	/// black at night by lowering the backdrop image's own alpha.
	/// </summary>
	public static float GetAmbientOpacity(TimeSlot timeSlot)
	{
		var index = (int)timeSlot;
		if (index < 0 || index >= LightOpacities.Length)
		{
			throw new ArgumentOutOfRangeException(nameof(timeSlot), timeSlot, "Unsupported time slot.");
		}

		return LightOpacities[index];
	}
}
