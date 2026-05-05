using Game.Application;
using Game.Core.Definitions;
using Game.Core.Model;
using Game.Core.Story;

namespace Game.Tests;

public sealed class StoryCommandLineServiceTests
{
	[Fact]
	public void Parse_SupportsQuotedStringsBooleansAndNumbers()
	{
		var service = CreateService(out _);

		var invocation = service.Parse("custom_cmd \"hello world\" true -3 1.5");

		Assert.Equal("custom_cmd", invocation.Name);
		Assert.Equal(4, invocation.Arguments.Count);
		Assert.Equal("hello world", invocation.Arguments[0].AsString("arg0"));
		Assert.True(invocation.Arguments[1].AsBoolean("arg1"));
		Assert.Equal(-3d, invocation.Arguments[2].AsNumber("arg2"));
		Assert.Equal(1.5d, invocation.Arguments[3].AsNumber("arg3"));
	}

	[Fact]
	public async Task ExecuteAsync_DispatchesBuiltInAndHostCommands()
	{
		var service = CreateService(out var session);

		await service.ExecuteAsync("log \"踏入江湖\"");
		await service.ExecuteAsync("map town");

		var entry = Assert.Single(session.State.Journal.Entries);
		Assert.Equal("踏入江湖", entry.Text);

		var host = Assert.IsType<RecordingRuntimeHost>(session.StoryService.Host);
		var command = Assert.Single(host.Commands);
		Assert.Equal("map", command.Name);
		Assert.Equal("town", command.Args[0].AsString("map"));
	}

	private static StoryCommandLineService CreateService(out GameSession session)
	{
		var repository = TestContentFactory.CreateRepository(
			maps:
			[
				new MapDefinition
				{
					Id = "town",
					Name = "town",
					Kind = MapKind.Small,
				},
			]);
		var host = new RecordingRuntimeHost();
		session = new GameSession(new GameState(), repository, host);
		return session.StoryService.CommandLine;
	}

	private sealed class RecordingRuntimeHost : IRuntimeHost
	{
		public List<(string Name, IReadOnlyList<ExprValue> Args)> Commands { get; } = [];

		public ValueTask DialogueAsync(DialogueContext dialogue, CancellationToken cancellationToken) =>
			ValueTask.CompletedTask;

		public ValueTask<ExprValue> GetVariableAsync(string name, CancellationToken cancellationToken) =>
			ValueTask.FromException<ExprValue>(new InvalidOperationException(name));

		public ValueTask<bool> EvaluatePredicateAsync(
			string name,
			IReadOnlyList<ExprValue> args,
			CancellationToken cancellationToken) =>
			ValueTask.FromException<bool>(new InvalidOperationException(name));

		public ValueTask ExecuteCommandAsync(
			string name,
			IReadOnlyList<ExprValue> args,
			CancellationToken cancellationToken)
		{
			Commands.Add((name, args));
			return ValueTask.CompletedTask;
		}

		public ValueTask<int> ChooseOptionAsync(ChoiceContext choice, CancellationToken cancellationToken) =>
			ValueTask.FromResult(0);

		public ValueTask<BattleOutcome> ResolveBattleAsync(BattleContext battle, CancellationToken cancellationToken) =>
			ValueTask.FromResult(BattleOutcome.Win);
	}
}
