using System.Text.Json;
using Game.Core.Serialization;
using Game.Core.Battle.Talents;

namespace Game.Core.Battle;

public abstract class CustomBattleEffectHandler<TParameters>
{
    public virtual bool SupportsPreview => false;

    public virtual void Validate(TParameters parameters)
    {
    }

    public abstract void Execute(BattleHookContext context, TParameters parameters);
}

public sealed class CustomBattleEffectRegistry
{
    private readonly Dictionary<string, ICustomBattleEffectHandler> _handlers = new(StringComparer.Ordinal);

    private CustomBattleEffectRegistry()
    {
    }

    public static CustomBattleEffectRegistry Default { get; } = CreateDefault();

    public void Register<TParameters>(
        string effectId,
        CustomBattleEffectHandler<TParameters> handler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(effectId);
        ArgumentNullException.ThrowIfNull(handler);

        if (!_handlers.TryAdd(effectId, new CustomBattleEffectHandlerAdapter<TParameters>(handler)))
        {
            throw new InvalidOperationException($"Custom battle effect '{effectId}' is already registered.");
        }
    }

    internal CustomBattleEffectInvocation Bind(string effectId, JsonElement parameters)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(effectId);
        if (!_handlers.TryGetValue(effectId, out var handler))
        {
            throw new InvalidOperationException($"Custom battle effect '{effectId}' is not registered.");
        }

        return handler.Bind(effectId, parameters);
    }

    private static CustomBattleEffectRegistry CreateDefault()
    {
        var registry = new CustomBattleEffectRegistry();
        registry.Register("illness", new IllnessBattleEffectHandler());
        registry.Register("zhenwu_formation_attack", new ZhenwuFormationAttackBattleEffectHandler());
        registry.Register("zhenwu_formation_intercept", new ZhenwuFormationInterceptBattleEffectHandler());
        return registry;
    }

    private interface ICustomBattleEffectHandler
    {
        CustomBattleEffectInvocation Bind(string effectId, JsonElement parameters);
    }

    private sealed class CustomBattleEffectHandlerAdapter<TParameters>(
        CustomBattleEffectHandler<TParameters> handler) : ICustomBattleEffectHandler
    {
        public CustomBattleEffectInvocation Bind(string effectId, JsonElement parameters)
        {
            TParameters parsedParameters;
            try
            {
                parsedParameters = parameters.Deserialize<TParameters>(GameJson.Default)
                    ?? throw new InvalidOperationException($"Custom battle effect '{effectId}' parameters cannot be null.");
            }
            catch (JsonException exception)
            {
                throw new InvalidOperationException(
                    $"Custom battle effect '{effectId}' parameters are invalid.",
                    exception);
            }

            handler.Validate(parsedParameters);
            return new CustomBattleEffectInvocation(
                handler.SupportsPreview,
                context => handler.Execute(context, parsedParameters));
        }
    }
}

internal sealed record CustomBattleEffectInvocation(
    bool SupportsPreview,
    Action<BattleHookContext> Execute);
