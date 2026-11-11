using Game.Core.Story;

namespace Game.Application;

public sealed class StoryCommandDispatcher
{
    private readonly GameSession _session;
    private readonly StoryExecutionContext _context;
    private readonly GameExpressionEnvironment _expressions;
    private readonly ExpressionEvaluator _evaluator = new();

    public StoryCommandDispatcher(
        GameSession session,
        IRuntimeHost host,
        StoryExecutionContext? context = null,
        bool includeDebugCommands = true)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(host);
        _session = session;
        _context = context ?? StoryExecutionContext.Empty;
        _expressions = new GameExpressionEnvironment(session);
        VariableMutations = new StoryVariableMutationService(session, _context);

        var builder = new AsyncExpressionCallRegistryBuilder<StoryCommandResult>(StoryCommandResult.None)
            .AddLibrary<StoryCommandAttribute>(new InventoryCurrencyStoryCommands(session))
            .AddLibrary<StoryCommandAttribute>(new AdventureStoryCommands(session))
            .AddLibrary<StoryCommandAttribute>(new StoryStateCommands(session, VariableMutations))
            .AddLibrary<StoryCommandAttribute>(new CharacterGrowthStoryCommands(session))
            .AddLibrary<StoryCommandAttribute>(new PartyLearningStoryCommands(session))
            .AddLibrary<StoryCommandAttribute>(new SpecialFlowStoryCommands(session, host))
            .AddLibrary<StoryCommandAttribute>(host);
        if (includeDebugCommands)
        {
            builder.AddLibrary<DebugCommandAttribute>(host);
        }

        Registry = builder.Build();
    }

    public AsyncExpressionCallRegistry<StoryCommandResult> Registry { get; }

    internal StoryVariableMutationService VariableMutations { get; }

    public async ValueTask<StoryCommandResult> ExecuteCommandAsync(
        string name,
        IReadOnlyList<ExpressionValue> args,
        CancellationToken cancellationToken = default)
    {
        var state = _session.State;
        var result = await Registry.InvokeAsync(name, args, cancellationToken);
        _session.Execution.ObserveResult(result, state);
        return result;
    }

    public async ValueTask<StoryCommandResult> ExecuteCallAsync(
        ParsedCall call,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await ExecuteCommandAsync(
                call.Root.Name,
                _evaluator.EvaluateArguments(call, _expressions.Create(_context)),
                cancellationToken);
        }
        catch (ExpressionException exception)
        {
            throw ExpressionException.WithLocation(exception, call.SourceName, call.Root.Span);
        }
    }
}
