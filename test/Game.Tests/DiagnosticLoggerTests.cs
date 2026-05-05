using Game.Application;
using Game.Core.Definitions;
using Game.Core.Definitions.Skills;
using Game.Core.Model;
using Game.Core.Model.Character;

namespace Game.Tests;

public sealed class DiagnosticLoggerTests
{
    [Fact]
    public void SaveGameService_WritesCreateAndLoadLogs()
    {
        var logger = new CollectingDiagnosticLogger();

        var basicAttack = TestContentFactory.CreateExternalSkill("basic_attack");
        var definition = TestContentFactory.CreateCharacterDefinition(
            "hero_knight",
            externalSkills: [new InitialExternalSkillEntryDefinition(basicAttack)]);

        var repository = TestContentFactory.CreateRepository(
            characters: [definition],
            externalSkills: [basicAttack]);

        var character = TestContentFactory.CreateCharacterInstance("char_001", definition);
        var party = new Party();
        party.AddMember(character);

        var state = new GameState();
        state.SetParty(party);
        var session = new GameSession(state, repository, logger);
        var service = session.SaveGameService;

        var saveGame = service.CreateSave();
        service.LoadSave(saveGame);

        Assert.Collection(
            logger.Entries,
            entry =>
            {
                Assert.Equal(DiagnosticLogLevel.Info, entry.Level);
                Assert.Contains("Created save game", entry.Message, StringComparison.Ordinal);
            },
            entry =>
            {
                Assert.Equal(DiagnosticLogLevel.Info, entry.Level);
                Assert.Contains("Loaded save game", entry.Message, StringComparison.Ordinal);
            });
    }

    private sealed class CollectingDiagnosticLogger : IDiagnosticLogger
    {
        private readonly List<(DiagnosticLogLevel Level, string Message)> _entries = [];

        public IReadOnlyList<(DiagnosticLogLevel Level, string Message)> Entries => _entries;

        public void Log(DiagnosticLogLevel level, string message, Exception? exception = null)
        {
            _entries.Add((level, message));
        }
    }
}
