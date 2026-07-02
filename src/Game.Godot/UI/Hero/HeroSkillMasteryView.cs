using Game.Core.Model.Skills;
using Godot;

namespace Game.Godot.UI;

internal sealed class HeroSkillMasteryView
{
	private readonly GridContainer _gridContainer;
	private readonly PackedScene _skillBoxScene;
	private readonly HeroSkillMasteryPresenter _presenter;
	private readonly JyButton _allButton;
	private readonly JyButton _quanzhangButton;
	private readonly JyButton _jianfaButton;
	private readonly JyButton _daofaButton;
	private readonly JyButton _qimenButton;
	private readonly JyButton _neigongButton;
	private readonly CheckBox _previewHardMaxCheckBox;

	private MasterySkillFilter _filter = MasterySkillFilter.All;

	public HeroSkillMasteryView(
		GridContainer gridContainer,
		PackedScene skillBoxScene,
		HeroSkillMasteryPresenter presenter,
		JyButton allButton,
		JyButton quanzhangButton,
		JyButton jianfaButton,
		JyButton daofaButton,
		JyButton qimenButton,
		JyButton neigongButton,
		CheckBox previewHardMaxCheckBox)
	{
		_gridContainer = gridContainer ?? throw new ArgumentNullException(nameof(gridContainer));
		_skillBoxScene = skillBoxScene ?? throw new ArgumentNullException(nameof(skillBoxScene));
		_presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
		_allButton = allButton ?? throw new ArgumentNullException(nameof(allButton));
		_quanzhangButton = quanzhangButton ?? throw new ArgumentNullException(nameof(quanzhangButton));
		_jianfaButton = jianfaButton ?? throw new ArgumentNullException(nameof(jianfaButton));
		_daofaButton = daofaButton ?? throw new ArgumentNullException(nameof(daofaButton));
		_qimenButton = qimenButton ?? throw new ArgumentNullException(nameof(qimenButton));
		_neigongButton = neigongButton ?? throw new ArgumentNullException(nameof(neigongButton));
		_previewHardMaxCheckBox = previewHardMaxCheckBox ?? throw new ArgumentNullException(nameof(previewHardMaxCheckBox));
	}

	public void Initialize()
	{
		_allButton.Pressed += () => SetFilter(MasterySkillFilter.All);
		_quanzhangButton.Pressed += () => SetFilter(MasterySkillFilter.Quanzhang);
		_jianfaButton.Pressed += () => SetFilter(MasterySkillFilter.Jianfa);
		_daofaButton.Pressed += () => SetFilter(MasterySkillFilter.Daofa);
		_qimenButton.Pressed += () => SetFilter(MasterySkillFilter.Qimen);
		_neigongButton.Pressed += () => SetFilter(MasterySkillFilter.Internal);
		_previewHardMaxCheckBox.Toggled += _ => Render();
		Render();
	}

	private void SetFilter(MasterySkillFilter filter)
	{
		if (_filter == filter)
		{
			return;
		}

		_filter = filter;
		Render();
	}

	private void Render()
	{
		ClearGrid();
		foreach (var skill in _presenter.GetSkills(_filter, _previewHardMaxCheckBox.ButtonPressed))
		{
			_gridContainer.AddChild(CreateSkillBox(skill));
		}
	}

	private SkillBox CreateSkillBox(SkillInstance skill)
	{
		var instance = _skillBoxScene.Instantiate();
		if (instance is not SkillBox skillBox)
		{
			instance.QueueFree();
			throw new InvalidOperationException("SkillBox scene root must be SkillBox.");
		}

		skillBox.Setup(skill, isInteractive: false, showToggleButton: false);
		skillBox.DetailRequested += OnSkillDetailRequested;
		return skillBox;
	}

	private static void OnSkillDetailRequested(SkillInstance skill)
	{
		UIRoot.Instance.ShowSkillDetailPanel(skill);
	}

	private void ClearGrid()
	{
		foreach (var child in _gridContainer.GetChildren())
		{
			child.QueueFree();
		}
	}
}
