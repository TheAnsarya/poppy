// ============================================================================
// RoundtripVerifierTests.cs - Unit Tests for Roundtrip Verification
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Poppy.Core.Project;

namespace Poppy.Tests.Project;

/// <summary>
/// Tests for roundtrip verification comparing assembled output to original ROM.
/// </summary>
public class RoundtripVerifierTests : IDisposable {
	private readonly string _tempDir;

	public RoundtripVerifierTests() {
		_tempDir = Path.Combine(Path.GetTempPath(), "PoppyTests", Guid.NewGuid().ToString());
		Directory.CreateDirectory(_tempDir);
	}

	[Fact]
	public void Verify_IdenticalBytes_ReturnsPass() {
		// Arrange
		var bytes = new byte[] { 0xa9, 0x00, 0x8d, 0x00, 0x20, 0x60 };

		// Act
		var result = RoundtripVerifier.Verify(bytes, bytes);

		// Assert
		Assert.True(result.Passed);
		Assert.Equal(0, result.TotalMismatches);
		Assert.Empty(result.Mismatches);
		Assert.Equal(result.OutputCrc32, result.OriginalCrc32);
		Assert.Equal(6, result.OutputSize);
		Assert.Equal(6, result.OriginalSize);
		Assert.Null(result.Error);
	}

	[Fact]
	public void Verify_DifferentBytes_ReturnsFail() {
		// Arrange
		var assembled = new byte[] { 0xa9, 0x00, 0x8d, 0x00 };
		var original = new byte[] { 0xa9, 0xff, 0x8d, 0x00 };

		// Act
		var result = RoundtripVerifier.Verify(assembled, original);

		// Assert
		Assert.False(result.Passed);
		Assert.Equal(1, result.TotalMismatches);
		Assert.Single(result.Mismatches);
		Assert.Equal(1, result.Mismatches[0].Offset);
		Assert.Equal(0xff, result.Mismatches[0].Expected);
		Assert.Equal(0x00, result.Mismatches[0].Actual);
	}

	[Fact]
	public void Verify_DifferentSizes_ReturnsFail() {
		// Arrange
		var assembled = new byte[] { 0xa9, 0x00 };
		var original = new byte[] { 0xa9, 0x00, 0x60 };

		// Act
		var result = RoundtripVerifier.Verify(assembled, original);

		// Assert
		Assert.False(result.Passed);
		Assert.Equal(2, result.OutputSize);
		Assert.Equal(3, result.OriginalSize);
		Assert.True(result.TotalMismatches >= 1);
	}

	[Fact]
	public void Verify_MultipleMismatches_ReportsAll() {
		// Arrange
		var assembled = new byte[] { 0x00, 0x00, 0x00, 0x00 };
		var original = new byte[] { 0xff, 0xff, 0xff, 0xff };

		// Act
		var result = RoundtripVerifier.Verify(assembled, original);

		// Assert
		Assert.False(result.Passed);
		Assert.Equal(4, result.TotalMismatches);
		Assert.Equal(4, result.Mismatches.Count);
	}

	[Fact]
	public void Verify_EmptyArrays_ReturnsPass() {
		// Arrange
		var bytes = Array.Empty<byte>();

		// Act
		var result = RoundtripVerifier.Verify(bytes, bytes);

		// Assert
		Assert.True(result.Passed);
	}

	[Fact]
	public void Verify_CrcValuesPopulated() {
		// Arrange
		var bytes = new byte[] { 0x01, 0x02, 0x03 };

		// Act
		var result = RoundtripVerifier.Verify(bytes, bytes);

		// Assert
		Assert.True(result.Passed);
		Assert.NotEqual(0u, result.OutputCrc32);
		Assert.Equal(result.OutputCrc32, result.OriginalCrc32);
	}

	[Fact]
	public void Verify_FromFile_ReturnsPass() {
		// Arrange
		var romBytes = new byte[] { 0xa9, 0x42, 0x60 };
		var romPath = Path.Combine(_tempDir, "test.nes");
		File.WriteAllBytes(romPath, romBytes);

		// Act
		var result = RoundtripVerifier.Verify(romBytes, romPath);

		// Assert
		Assert.True(result.Passed);
		Assert.Null(result.Error);
	}

	[Fact]
	public void Verify_FromFile_MissingFile_ReturnsError() {
		// Arrange
		var bytes = new byte[] { 0xa9, 0x42 };

		// Act
		var result = RoundtripVerifier.Verify(bytes, Path.Combine(_tempDir, "nonexistent.nes"));

		// Assert
		Assert.False(result.Passed);
		Assert.NotNull(result.Error);
		Assert.Contains("not found", result.Error);
	}

	[Fact]
	public void Verify_FromPeonyProject_ReturnsPass() {
		// Arrange
		var romBytes = new byte[] { 0xa9, 0x42, 0x60 };
		var romPath = Path.Combine(_tempDir, "rom", "game.nes");
		Directory.CreateDirectory(Path.GetDirectoryName(romPath)!);
		File.WriteAllBytes(romPath, romBytes);

		var json = """
			{
				"version": "1.0",
				"platform": "nes",
				"rom": { "path": "rom/game.nes" }
			}
			""";
		var project = PeonyProjectReader.LoadFromString(json, _tempDir);

		// Act
		var result = RoundtripVerifier.Verify(romBytes, project);

		// Assert
		Assert.True(result.Passed);
		Assert.Null(result.Error);
	}

	[Fact]
	public void Verify_FromPeonyProject_NullRomPath_ReturnsError() {
		// Arrange
		var json = """{ "version": "1.0", "platform": "nes" }""";
		var project = PeonyProjectReader.LoadFromString(json, _tempDir);

		// Act
		var result = RoundtripVerifier.Verify(new byte[] { 0x00 }, project);

		// Assert
		Assert.False(result.Passed);
		Assert.Contains("no ROM path", result.Error!);
	}

	[Fact]
	public void Verify_FromPeonyProject_CrcMismatch_ReturnsError() {
		// Arrange
		var romBytes = new byte[] { 0xa9, 0x42, 0x60 };
		var romPath = Path.Combine(_tempDir, "game.nes");
		File.WriteAllBytes(romPath, romBytes);

		var json = """
			{
				"version": "1.0",
				"platform": "nes",
				"rom": { "path": "game.nes", "crc32": "00000000" }
			}
			""";
		var project = PeonyProjectReader.LoadFromString(json, _tempDir);

		// Act
		var result = RoundtripVerifier.Verify(romBytes, project);

		// Assert
		Assert.False(result.Passed);
		Assert.Contains("does not match", result.Error!);
	}

	[Fact]
	public void Verify_FromPeonyProject_ValidCrc_ReturnsPass() {
		// Arrange
		var romBytes = new byte[] { 0xa9, 0x42, 0x60 };
		var romPath = Path.Combine(_tempDir, "game.nes");
		File.WriteAllBytes(romPath, romBytes);

		// Compute the actual CRC32 for this data
		uint crcValue = 0xffffffff;
		foreach (byte b in romBytes) {
			crcValue ^= b;
			for (int j = 0; j < 8; j++)
				crcValue = (crcValue & 1) != 0 ? (crcValue >> 1) ^ 0xedb88320 : crcValue >> 1;
		}
		crcValue ^= 0xffffffff;

		var json = $$"""
			{
				"version": "1.0",
				"platform": "nes",
				"rom": { "path": "game.nes", "crc32": "{{crcValue:x8}}" }
			}
			""";
		var project = PeonyProjectReader.LoadFromString(json, _tempDir);

		// Act
		var result = RoundtripVerifier.Verify(romBytes, project);

		// Assert
		Assert.True(result.Passed);
		Assert.Null(result.Error);
	}

	[Fact]
	public void Verify_LargeFile_MismatchesCapped() {
		// Arrange — create arrays with many differences
		var assembled = new byte[256];
		var original = new byte[256];
		for (int i = 0; i < 256; i++) {
			assembled[i] = (byte)i;
			original[i] = (byte)(255 - i);
		}

		// Act
		var result = RoundtripVerifier.Verify(assembled, original);

		// Assert
		Assert.False(result.Passed);
		Assert.True(result.Mismatches.Count <= 16); // MaxReportedMismatches
		Assert.True(result.TotalMismatches > result.Mismatches.Count);
	}

	public void Dispose() {
		try {
			if (Directory.Exists(_tempDir))
				Directory.Delete(_tempDir, true);
		} catch {
			// Cleanup is best-effort
		}
	}
}
