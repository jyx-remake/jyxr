using System.Diagnostics.CodeAnalysis;
using Godot;

namespace Game.Godot.Resources;

internal sealed class TextureCache : IDisposable
{
	private const int PruneInterval = 64;
	private readonly Dictionary<string, TextureEntry> _sources = new(StringComparer.Ordinal);
	private readonly Dictionary<ulong, LinkedListNode<TextureGroup>> _retainedGroups = [];
	private readonly LinkedList<TextureGroup> _recentGroups = [];
	private int _loadsSincePrune;

	public TextureCache(long budgetBytes)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(budgetBytes);
		BudgetBytes = budgetBytes;
	}

	public long BudgetBytes { get; }
	public long RetainedBytes { get; private set; }
	public int RetainedGroupCount => _retainedGroups.Count;
	public int SourceCount => _sources.Count;
	public long Hits { get; private set; }
	public long WeakHits { get; private set; }
	public long Loads { get; private set; }
	public long Evictions { get; private set; }

	public bool TryGet(string source, [NotNullWhen(true)] out Texture2D? texture)
	{
		texture = null;
		if (!_sources.TryGetValue(source, out var entry))
		{
			return false;
		}

		if (_retainedGroups.TryGetValue(entry.RootId, out var node) &&
			node.Value.Textures.TryGetValue(source, out texture))
		{
			Touch(node);
			Hits++;
			return true;
		}

		texture = entry.Reference.GetRef().AsGodotObject() as Texture2D;
		if (texture is null)
		{
			entry.Reference.Dispose();
			_sources.Remove(source);
			return false;
		}

		WeakHits++;
		Retain(source, texture, entry.RootId, entry.EstimatedBytes);
		return true;
	}

	public void Add(string source, Texture2D texture)
	{
		var root = texture;
		while (root is AtlasTexture { Atlas: { } atlas })
		{
			root = atlas;
		}

		var rootId = root.GetInstanceId();
		var estimatedBytes = EstimateBytes(root);
		var entry = new TextureEntry(GodotObject.WeakRef(texture)!, rootId, estimatedBytes);
		_sources.Add(source, entry);
		Retain(source, texture, rootId, estimatedBytes);
		Loads++;
		if (++_loadsSincePrune >= PruneInterval)
		{
			PruneExpired();
			_loadsSincePrune = 0;
		}
	}

	private void Retain(string source, Texture2D texture, ulong rootId, long estimatedBytes)
	{
		if (estimatedBytes > BudgetBytes)
		{
			return;
		}

		if (!_retainedGroups.TryGetValue(rootId, out var node))
		{
			var group = new TextureGroup(rootId, estimatedBytes);
			node = _recentGroups.AddLast(group);
			_retainedGroups.Add(rootId, node);
			RetainedBytes += estimatedBytes;
		}

		node.Value.Textures[source] = texture;
		Touch(node);
		while (RetainedBytes > BudgetBytes && _recentGroups.First is { } oldest)
		{
			_recentGroups.RemoveFirst();
			_retainedGroups.Remove(oldest.Value.RootId);
			RetainedBytes -= oldest.Value.EstimatedBytes;
			Evictions++;
		}
	}

	private void Touch(LinkedListNode<TextureGroup> node)
	{
		_recentGroups.Remove(node);
		_recentGroups.AddLast(node);
	}

	private void PruneExpired()
	{
		foreach (var (source, entry) in _sources.ToArray())
		{
			if (entry.Reference.GetRef().AsGodotObject() is null)
			{
				entry.Reference.Dispose();
				_sources.Remove(source);
			}
		}
	}

	private static long EstimateBytes(Texture2D texture)
	{
		// Account for common GPU padding of three-component formats without reading pixels back.
		var (blockWidth, blockHeight, bytesPerBlock) = texture.GetFormat() switch
		{
			Image.Format.L8 or Image.Format.R8 => (1, 1, 1),
			Image.Format.La8 or Image.Format.Rg8 or Image.Format.Rgba4444 or Image.Format.Rgb565 or
				Image.Format.Rh or Image.Format.R16 or Image.Format.R16I => (1, 1, 2),
			Image.Format.Rgb8 or Image.Format.Rgba8 or Image.Format.Rf or Image.Format.Rgh or
				Image.Format.Rgbe9995 or Image.Format.Rg16 or Image.Format.Rg16I => (1, 1, 4),
			Image.Format.Rgf or Image.Format.Rgbh or Image.Format.Rgbah or Image.Format.Rgb16 or
				Image.Format.Rgba16 or Image.Format.Rgb16I or Image.Format.Rgba16I => (1, 1, 8),
			Image.Format.Dxt1 or Image.Format.RgtcR or Image.Format.Etc or Image.Format.Etc2R11 or
				Image.Format.Etc2R11S or Image.Format.Etc2Rgb8 or Image.Format.Etc2Rgb8A1 => (4, 4, 8),
			Image.Format.Dxt3 or Image.Format.Dxt5 or Image.Format.RgtcRg or Image.Format.BptcRgba or
				Image.Format.BptcRgbf or Image.Format.BptcRgbfu or Image.Format.Etc2Rg11 or
				Image.Format.Etc2Rg11S or Image.Format.Etc2Rgba8 or Image.Format.Etc2RaAsRg or
				Image.Format.Dxt5RaAsRg or Image.Format.Astc4X4 or Image.Format.Astc4X4Hdr => (4, 4, 16),
			Image.Format.Astc8X8 or Image.Format.Astc8X8Hdr => (8, 8, 16),
			_ => (1, 1, 16),
		};
		var width = Math.Max(1, texture.GetWidth());
		var height = Math.Max(1, texture.GetHeight());
		return ((long)width + blockWidth - 1) / blockWidth *
			((long)height + blockHeight - 1) / blockHeight * bytesPerBlock;
	}

	public void Dispose()
	{
		_retainedGroups.Clear();
		_recentGroups.Clear();
		foreach (var entry in _sources.Values)
		{
			entry.Reference.Dispose();
		}

		_sources.Clear();
		RetainedBytes = 0;
	}

	private sealed record TextureEntry(WeakRef Reference, ulong RootId, long EstimatedBytes);

	private sealed class TextureGroup(ulong rootId, long estimatedBytes)
	{
		public ulong RootId { get; } = rootId;
		public long EstimatedBytes { get; } = estimatedBytes;
		public Dictionary<string, Texture2D> Textures { get; } = new(StringComparer.Ordinal);
	}
}
