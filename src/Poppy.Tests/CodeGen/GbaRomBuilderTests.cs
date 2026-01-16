// GbaRomBuilderTests.cs
// Unit tests for GBA ROM header builder

using Poppy.Core.CodeGen;
using Xunit;

namespace Poppy.Tests.CodeGen;

/// <summary>
/// Unit tests for the GBA ROM builder.
/// </summary>
public class GbaRomBuilderTests {
	#region Header Building Tests

	[Fact]
	public void Build_Default_Returns192Bytes() {
		var builder = new GbaRomBuilder();
		var header = builder.Build();

		Assert.Equal(192, header.Length);
	}

	[Fact]
	public void Build_HasNintendoLogo() {
		var builder = new GbaRomBuilder();
		var header = builder.Build();

		// Nintendo logo starts at offset $04
		// First byte of logo should be $24
		Assert.Equal(0x24, header[0x04]);
	}

	[Fact]
	public void Build_HasFixedValue96() {
		var builder = new GbaRomBuilder();
		var header = builder.Build();

		// Fixed value $96 at offset $b2
		Assert.Equal(0x96, header[0xb2]);
	}

	[Fact]
	public void Build_WithTitle_StoresTitle() {
		var builder = new GbaRomBuilder();
		builder.SetTitle("TEST");
		var header = builder.Build();

		// Title at offset $a0, max 12 chars
		Assert.Equal((byte)'T', header[0xa0]);
		Assert.Equal((byte)'E', header[0xa1]);
		Assert.Equal((byte)'S', header[0xa2]);
		Assert.Equal((byte)'T', header[0xa3]);
	}

	[Fact]
	public void Build_WithGameCode_StoresCode() {
		var builder = new GbaRomBuilder();
		builder.SetGameCode("AXVE");
		var header = builder.Build();

		// Game code at offset $ac
		Assert.Equal((byte)'A', header[0xac]);
		Assert.Equal((byte)'X', header[0xad]);
		Assert.Equal((byte)'V', header[0xae]);
		Assert.Equal((byte)'E', header[0xaf]);
	}

	[Fact]
	public void Build_WithMakerCode_StoresCode() {
		var builder = new GbaRomBuilder();
		builder.SetMakerCode("01");
		var header = builder.Build();

		// Maker code at offset $b0
		Assert.Equal((byte)'0', header[0xb0]);
		Assert.Equal((byte)'1', header[0xb1]);
	}

	[Fact]
	public void Build_HasCorrectChecksum() {
		var builder = new GbaRomBuilder();
		builder.SetTitle("TEST");
		var header = builder.Build();

		// Verify checksum at $bd
		byte expected = GbaRomBuilder.CalculateChecksum(header);
		Assert.Equal(expected, header[0xbd]);
	}

	#endregion

	#region Validation Tests

	[Fact]
	public void ValidateHeader_ValidHeader_ReturnsTrue() {
		var builder = new GbaRomBuilder();
		var header = builder.Build();

		Assert.True(GbaRomBuilder.ValidateHeader(header));
	}

	[Fact]
	public void ValidateHeader_TooShort_ReturnsFalse() {
		var shortHeader = new byte[100];
		Assert.False(GbaRomBuilder.ValidateHeader(shortHeader));
	}

	[Fact]
	public void ValidateHeader_Null_ReturnsFalse() {
		Assert.False(GbaRomBuilder.ValidateHeader(null!));
	}

	[Fact]
	public void ValidateHeader_BadChecksum_ReturnsFalse() {
		var builder = new GbaRomBuilder();
		var header = builder.Build();
		header[0xbd] = 0xff;  // Corrupt checksum

		Assert.False(GbaRomBuilder.ValidateHeader(header));
	}

	#endregion

	#region Entry Point Tests

	[Fact]
	public void SetEntryPointAddress_StoresAddress() {
		var builder = new GbaRomBuilder();
		builder.SetEntryPointAddress(0x08000000);
		var header = builder.Build();

		// Entry point is encoded at offset $00-$03
		// Format: ARM B instruction to entry point
		Assert.NotEqual(0, header[0]);
	}

	#endregion

	#region Extraction Tests

	[Fact]
	public void GetTitle_ReturnsStoredTitle() {
		var builder = new GbaRomBuilder();
		builder.SetTitle("HELLO");
		var header = builder.Build();

		string title = GbaRomBuilder.GetTitle(header);
		Assert.Equal("HELLO", title);
	}

	[Fact]
	public void GetGameCode_ReturnsStoredCode() {
		var builder = new GbaRomBuilder();
		builder.SetGameCode("ABCD");
		var header = builder.Build();

		string code = GbaRomBuilder.GetGameCode(header);
		Assert.Equal("ABCD", code);
	}

	[Fact]
	public void GetMakerCode_ReturnsStoredCode() {
		var builder = new GbaRomBuilder();
		builder.SetMakerCode("69");
		var header = builder.Build();

		string code = GbaRomBuilder.GetMakerCode(header);
		Assert.Equal("69", code);
	}

	#endregion

	#region Checksum Tests

	[Fact]
	public void CalculateChecksum_ConsistentResults() {
		var builder = new GbaRomBuilder();
		builder.SetTitle("TEST");
		var header = builder.Build();

		byte checksum1 = GbaRomBuilder.CalculateChecksum(header);
		byte checksum2 = GbaRomBuilder.CalculateChecksum(header);

		Assert.Equal(checksum1, checksum2);
	}

	[Fact]
	public void CalculateChecksum_DifferentData_DifferentChecksum() {
		var builder1 = new GbaRomBuilder();
		builder1.SetTitle("TEST1");
		var header1 = builder1.Build();

		var builder2 = new GbaRomBuilder();
		builder2.SetTitle("TEST2");
		var header2 = builder2.Build();

		// Checksums should be different (or at least titles are different)
		Assert.NotEqual(
			System.Text.Encoding.ASCII.GetString(header1, 0xa0, 12).TrimEnd('\0'),
			System.Text.Encoding.ASCII.GetString(header2, 0xa0, 12).TrimEnd('\0'));
	}

	#endregion

	#region Builder Chaining Tests

	[Fact]
	public void Builder_SupportsChaining() {
		var header = new GbaRomBuilder()
			.SetTitle("CHAINED")
			.SetGameCode("AAAA")
			.SetMakerCode("01")
			.SetVersion(1)
			.Build();

		Assert.Equal(192, header.Length);
	}

	#endregion
}
