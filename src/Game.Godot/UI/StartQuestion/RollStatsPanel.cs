using Game.Core.Model;
using Godot;

namespace Game.Godot.UI;

public partial class RollStatsPanel : Control
{
	private readonly TaskCompletionSource _completion = new();
	private string _characterId = string.Empty;
	private Dictionary<StatType, int> _originStats = [];
	private CharacterPanel _characterPanel = null!;
	private TextureButton _rollButton = null!;
	private TextureButton _ackButton = null!;

	public override void _Ready()
	{
		_characterPanel = GetNode<CharacterPanel>("%CharacterPanel");
		_rollButton = GetNode<TextureButton>("%RollButton");
		_ackButton = GetNode<TextureButton>("%AckButton");
		_rollButton.Pressed += RollAndSync;
		_ackButton.Pressed += Submit;
		_characterPanel.GetNode<Control>("%CloseButton").Hide();
		_characterPanel.GetNode<Control>("%PrevCharacterButton").Hide();
		_characterPanel.GetNode<Control>("%NextCharacterButton").Hide();
	}

	public async Task AwaitRollAsync(string characterId, CancellationToken cancellationToken = default)
	{
		_characterId = characterId;
		var character = Game.State.Party.GetMember(characterId);
		_originStats = character.BaseStats.ToDictionary();
		_characterPanel.CharacterId = characterId;
		RollAndSync();

		using var registration = cancellationToken.CanBeCanceled
			? cancellationToken.Register(() =>
			{
				if (_completion.TrySetCanceled(cancellationToken) && GodotObject.IsInstanceValid(this))
				{
					QueueFree();
				}
			})
			: default;

		await _completion.Task;
	}

	public override void _ExitTree()
	{
		if (!_completion.Task.IsCompleted)
		{
			_completion.TrySetCanceled();
		}
	}

	private void RollAndSync()
	{
		var stats = new Dictionary<StatType, int>(_originStats);
		for (var index = 0; index < 3; index += 1)
		{
			AddRandomStat(stats, 10);
		}

		for (var index = 0; index < 10; index += 1)
		{
			AddRandomStat(stats, 1);
		}

		Game.CharacterService.ReplaceBaseStats(_characterId, stats);
	}

	private static void AddRandomStat(Dictionary<StatType, int> stats, int value)
	{
		var stat = StatCatalog.TenDimensionStats[Random.Shared.Next(StatCatalog.TenDimensionStats.Count)];
		stats[stat] = stats.GetValueOrDefault(stat) + value;
	}

	private void Submit()
	{
		if (_completion.TrySetResult())
		{
			QueueFree();
		}
	}
}
