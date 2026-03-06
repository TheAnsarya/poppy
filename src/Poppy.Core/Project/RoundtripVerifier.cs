// ============================================================================
// RoundtripVerifier.cs - Verify Assembled Output Matches Original ROM
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using System.Buffers.Binary;
using StreamHash.Core;

namespace Poppy.Core.Project;

/// <summary>
/// A single byte mismatch between assembled output and original ROM.
/// </summary>
/// <param name="Offset">Byte offset of the mismatch.</param>
/// <param name="Expected">Expected byte value (from original ROM).</param>
/// <param name="Actual">Actual byte value (from assembled output).</param>
public readonly record struct ByteMismatch(int Offset, byte Expected, byte Actual);

/// <summary>
/// Result of a roundtrip verification comparing assembled output to an original ROM.
/// </summary>
public sealed class VerificationResult {
	/// <summary>Whether the verification passed (all bytes match).</summary>
	public bool Passed { get; init; }

	/// <summary>CRC32 of the assembled output.</summary>
	public uint OutputCrc32 { get; init; }

	/// <summary>CRC32 of the original ROM.</summary>
	public uint OriginalCrc32 { get; init; }

	/// <summary>Size of the assembled output in bytes.</summary>
	public int OutputSize { get; init; }

	/// <summary>Size of the original ROM in bytes.</summary>
	public int OriginalSize { get; init; }

	/// <summary>Byte mismatches (empty on pass, limited to first N on fail).</summary>
	public IReadOnlyList<ByteMismatch> Mismatches { get; init; } = [];

	/// <summary>Total number of mismatching bytes (may exceed Mismatches.Count).</summary>
	public int TotalMismatches { get; init; }

	/// <summary>Error message if verification could not be performed.</summary>
	public string? Error { get; init; }
}

/// <summary>
/// Verifies that assembled output matches an original ROM file byte-for-byte.
/// Used for roundtrip verification in disassemble → assemble workflows.
/// </summary>
public static class RoundtripVerifier {
	/// <summary>Maximum number of individual byte mismatches to report.</summary>
	private const int MaxReportedMismatches = 16;

	/// <summary>
	/// Verify assembled output bytes against an original ROM file.
	/// </summary>
	/// <param name="assembledBytes">The assembled output bytes.</param>
	/// <param name="originalRomPath">Path to the original ROM file.</param>
	/// <returns>Verification result with pass/fail and mismatch details.</returns>
	public static VerificationResult Verify(byte[] assembledBytes, string originalRomPath) {
		if (!File.Exists(originalRomPath)) {
			return new VerificationResult {
				Passed = false,
				Error = $"Original ROM not found: {originalRomPath}"
			};
		}

		byte[] originalBytes;
		try {
			originalBytes = File.ReadAllBytes(originalRomPath);
		} catch (Exception ex) {
			return new VerificationResult {
				Passed = false,
				Error = $"Failed to read original ROM: {ex.Message}"
			};
		}

		return Verify(assembledBytes, originalBytes);
	}

	/// <summary>
	/// Verify assembled output bytes against original ROM bytes.
	/// </summary>
	/// <param name="assembledBytes">The assembled output bytes.</param>
	/// <param name="originalBytes">The original ROM bytes.</param>
	/// <returns>Verification result with pass/fail and mismatch details.</returns>
	public static VerificationResult Verify(byte[] assembledBytes, byte[] originalBytes) {
		var outputCrc = ComputeCrc32(assembledBytes);
		var originalCrc = ComputeCrc32(originalBytes);

		// Quick CRC check first
		if (outputCrc == originalCrc && assembledBytes.Length == originalBytes.Length) {
			// CRC match — verify byte-for-byte to be certain
			if (assembledBytes.AsSpan().SequenceEqual(originalBytes.AsSpan())) {
				return new VerificationResult {
					Passed = true,
					OutputCrc32 = outputCrc,
					OriginalCrc32 = originalCrc,
					OutputSize = assembledBytes.Length,
					OriginalSize = originalBytes.Length
				};
			}
		}

		// Find mismatches
		var mismatches = new List<ByteMismatch>();
		int totalMismatches = 0;
		int compareLength = Math.Min(assembledBytes.Length, originalBytes.Length);

		for (int i = 0; i < compareLength; i++) {
			if (assembledBytes[i] != originalBytes[i]) {
				totalMismatches++;
				if (mismatches.Count < MaxReportedMismatches) {
					mismatches.Add(new ByteMismatch(i, originalBytes[i], assembledBytes[i]));
				}
			}
		}

		// Count size difference as mismatches
		totalMismatches += Math.Abs(assembledBytes.Length - originalBytes.Length);

		return new VerificationResult {
			Passed = false,
			OutputCrc32 = outputCrc,
			OriginalCrc32 = originalCrc,
			OutputSize = assembledBytes.Length,
			OriginalSize = originalBytes.Length,
			Mismatches = mismatches,
			TotalMismatches = totalMismatches
		};
	}

	/// <summary>
	/// Verify using a Peony project configuration.
	/// Resolves the ROM path from peony.json and optionally validates CRC32.
	/// </summary>
	/// <param name="assembledBytes">The assembled output bytes.</param>
	/// <param name="project">The Peony project configuration.</param>
	/// <returns>Verification result.</returns>
	public static VerificationResult Verify(byte[] assembledBytes, PeonyProject project) {
		var romPath = project.ResolvedRomPath;
		if (romPath is null) {
			return new VerificationResult {
				Passed = false,
				Error = "Peony project has no ROM path specified"
			};
		}

		var result = Verify(assembledBytes, romPath);

		// If peony.json has a CRC32, validate it matches the original ROM
		if (result.Error is null && project.Rom?.Crc32 is not null) {
			if (uint.TryParse(project.Rom.Crc32, System.Globalization.NumberStyles.HexNumber, null, out var expectedCrc)) {
				if (result.OriginalCrc32 != expectedCrc) {
					return new VerificationResult {
						Passed = false,
						OutputCrc32 = result.OutputCrc32,
						OriginalCrc32 = result.OriginalCrc32,
						OutputSize = result.OutputSize,
						OriginalSize = result.OriginalSize,
						Error = $"Original ROM CRC32 ({result.OriginalCrc32:x8}) does not match peony.json expected CRC ({expectedCrc:x8}) — ROM file may be wrong"
					};
				}
			}
		}

		return result;
	}

	/// <summary>
	/// Compute CRC32 of a byte array.
	/// </summary>
	private static uint ComputeCrc32(byte[] data) {
		return BitConverter.ToUInt32(HashFacade.ComputeCrc32(data));
	}
}
