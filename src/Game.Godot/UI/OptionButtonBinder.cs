using Godot;

namespace Game.Godot.UI;

public static class OptionButtonBinder
{
	public static void PopulateEnum<TEnum>(
		OptionButton optionButton,
		IEnumerable<(string Label, TEnum Value)> options)
		where TEnum : struct, Enum
	{
		ArgumentNullException.ThrowIfNull(optionButton);
		ArgumentNullException.ThrowIfNull(options);

		optionButton.Clear();
		foreach (var (label, value) in options)
		{
			var index = optionButton.ItemCount;
			optionButton.AddItem(label);
			optionButton.SetItemMetadata(index, Convert.ToInt32(value));
		}
	}

	public static TEnum ReadSelectedEnum<TEnum>(OptionButton optionButton, TEnum fallback)
		where TEnum : struct, Enum
	{
		ArgumentNullException.ThrowIfNull(optionButton);

		var selected = optionButton.Selected;
		if (selected < 0)
		{
			return fallback;
		}

		var metadata = optionButton.GetItemMetadata(selected);
		return metadata.VariantType == Variant.Type.Int
			? (TEnum)Enum.ToObject(typeof(TEnum), metadata.AsInt32())
			: fallback;
	}

	public static void SelectEnumNoSignal<TEnum>(OptionButton optionButton, TEnum value)
		where TEnum : struct, Enum
	{
		ArgumentNullException.ThrowIfNull(optionButton);

		var expected = Convert.ToInt32(value);
		for (var index = 0; index < optionButton.ItemCount; index++)
		{
			var metadata = optionButton.GetItemMetadata(index);
			if (metadata.VariantType == Variant.Type.Int && metadata.AsInt32() == expected)
			{
				optionButton.Select(index);
				return;
			}
		}

		if (optionButton.ItemCount > 0)
		{
			optionButton.Select(0);
		}
	}
}
