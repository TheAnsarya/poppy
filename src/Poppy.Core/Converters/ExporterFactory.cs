// ============================================================================
// ExporterFactory.cs - Factory for PASM Exporters
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

namespace Poppy.Core.Converters;

/// <summary>
/// Factory for creating PASM exporters.
/// </summary>
public static class ExporterFactory {
	private static readonly Dictionary<string, Func<IPasmExporter>> _exporters = new(StringComparer.OrdinalIgnoreCase) {
		["ASAR"] = () => new PasmToAsarExporter(),
		["CA65"] = () => new PasmToCa65Exporter(),
		["XKAS"] = () => new PasmToXkasExporter(),
	};

	/// <summary>
	/// Gets a list of all supported target assemblers.
	/// </summary>
	public static IReadOnlyList<string> SupportedTargets =>
		[.. _exporters.Keys.OrderBy(k => k)];

	/// <summary>
	/// Creates an exporter for the specified target assembler.
	/// </summary>
	/// <param name="assembler">The target assembler name (e.g., "ASAR", "ca65", "xkas").</param>
	/// <returns>The PASM exporter.</returns>
	public static IPasmExporter Create(string assembler) {
		if (_exporters.TryGetValue(assembler, out var factory)) {
			return factory();
		}

		throw new ArgumentException(
			$"Unknown target assembler: {assembler}. Supported targets: {string.Join(", ", SupportedTargets)}",
			nameof(assembler));
	}

	/// <summary>
	/// Attempts to create an exporter for the specified target assembler.
	/// </summary>
	/// <param name="assembler">The target assembler name.</param>
	/// <param name="exporter">The created exporter, or null if not supported.</param>
	/// <returns>True if the exporter was created.</returns>
	public static bool TryCreate(string assembler, out IPasmExporter? exporter) {
		if (_exporters.TryGetValue(assembler, out var factory)) {
			exporter = factory();
			return true;
		}

		exporter = null;
		return false;
	}

	/// <summary>
	/// Registers a custom exporter.
	/// </summary>
	/// <param name="assembler">The target assembler name.</param>
	/// <param name="factory">The factory function to create the exporter.</param>
	public static void Register(string assembler, Func<IPasmExporter> factory) {
		_exporters[assembler] = factory;
	}

	/// <summary>
	/// Unregisters an exporter.
	/// </summary>
	/// <param name="assembler">The assembler name to remove.</param>
	/// <returns>True if the exporter was removed.</returns>
	public static bool Unregister(string assembler) {
		return _exporters.Remove(assembler);
	}
}
