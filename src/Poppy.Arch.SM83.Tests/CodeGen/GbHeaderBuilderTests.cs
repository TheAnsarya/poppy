// ============================================================================
// GbHeaderBuilderTests.cs - Unit Tests for Game Boy ROM Header Builder
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Poppy.Core.CodeGen;
using Xunit;

namespace Poppy.Arch.SM83.Tests.CodeGen;

/// <summary>
/// Tests for the Game Boy ROM header builder.
/// </summary>
public sealed class GbHeaderBuilderTests {
	#region Header Size and Structure

	[Fact]
	public void Build_Default_Returns80Bytes() {
		var header = new GbHeaderBuilder().Build();
		Assert.Equal(80, header.Length);
	}

	[Fact]
	public void Build_Default_HasEntryPoint() {
		var header = new GbHeaderBuilder().Build();

		// Entry point: nop ($00) + jp $0150 ($c3 $50 $01)
		Assert.Equal(0x00, header[0]); // nop
		Assert.Equal(0xc3, header[1]); // jp
		Assert.Equal(0x50, header[2]); // low byte of $0150
		Assert.Equal(0x01, header[3]); // high byte of $0150
	}

	[Fact]
	public void Build_Default_HasNintendoLogo() {
		var header = new GbHeaderBuilder().Build();

		// Nintendo logo starts at offset 4, 48 bytes
		// First bytes of the logo: $ce $ed $66 $66
		Assert.Equal(0xce, header[4]);
		Assert.Equal(0xed, header[5]);
		Assert.Equal(0x66, header[6]);
		Assert.Equal(0x66, header[7]);

		// Last bytes of the logo: $bb $b9 $33 $3e
		Assert.Equal(0xbb, header[48]);
		Assert.Equal(0xb9, header[49]);
		Assert.Equal(0x33, header[50]);
		Assert.Equal(0x3e, header[51]);
	}

	#endregion

	#region Title

	[Fact]
	public void Build_WithTitle_EncodesAtOffset52() {
		var header = new GbHeaderBuilder()
			.SetTitle("TEST")
			.Build();

		Assert.Equal((byte)'T', header[52]);
		Assert.Equal((byte)'E', header[53]);
		Assert.Equal((byte)'S', header[54]);
		Assert.Equal((byte)'T', header[55]);
		// Remaining title bytes should be zero-padded
		Assert.Equal(0, header[56]);
	}

	[Fact]
	public void Build_WithLongTitle_TruncatesTo16() {
		var header = new GbHeaderBuilder()
			.SetTitle("ABCDEFGHIJKLMNOPQRSTUVWXYZ")
			.Build();

		// Should only have first 16 characters written at offsets 52-67
		Assert.Equal((byte)'A', header[52]);
		Assert.Equal((byte)'O', header[66]); // 15th character at offset 52+14=66
		// Note: offset 67 ($0143) is the CGB flag, which overwrites the 16th title byte
		// This is correct GB header behavior — CGB-aware ROMs have 15-char titles
		Assert.Equal((byte)GbCgbMode.DmgOnly, header[67]);
	}

	[Fact]
	public void Build_WithEmptyTitle_AllZero() {
		var header = new GbHeaderBuilder()
			.SetTitle("")
			.Build();

		for (int i = 52; i < 68; i++) {
			// Title bytes (skipping CGB flag at 67)
			if (i == 67) continue; // CGB flag
			Assert.Equal(0, header[i]);
		}
	}

	#endregion

	#region CGB Mode

	[Fact]
	public void Build_Default_CgbModeIsDmgOnly() {
		var header = new GbHeaderBuilder().Build();
		Assert.Equal((byte)GbCgbMode.DmgOnly, header[67]);
	}

	[Fact]
	public void Build_CgbCompatible_SetsFlag() {
		var header = new GbHeaderBuilder()
			.SetCgbMode(GbCgbMode.CgbCompatible)
			.Build();

		Assert.Equal(0x80, header[67]);
	}

	[Fact]
	public void Build_CgbOnly_SetsFlag() {
		var header = new GbHeaderBuilder()
			.SetCgbMode(GbCgbMode.CgbOnly)
			.Build();

		Assert.Equal(0xc0, header[67]);
	}

	#endregion

	#region Licensee Code

	[Fact]
	public void Build_Default_NewLicenseeCode00() {
		var header = new GbHeaderBuilder().Build();

		// New licensee code "00" at offsets 68-69
		Assert.Equal(0x30, header[68]); // '0'
		Assert.Equal(0x30, header[69]); // '0'
	}

	[Fact]
	public void Build_Default_OldLicenseeCode33() {
		var header = new GbHeaderBuilder().Build();

		// Old licensee code 0x33 = "use new licensee code"
		Assert.Equal(0x33, header[75]);
	}

	#endregion

	#region SGB Flag

	[Fact]
	public void Build_Default_SgbDisabled() {
		var header = new GbHeaderBuilder().Build();
		Assert.Equal(0x00, header[70]);
	}

	[Fact]
	public void Build_SgbEnabled_SetsFlag() {
		var header = new GbHeaderBuilder()
			.SetSgbEnabled(true)
			.Build();

		Assert.Equal(0x03, header[70]);
	}

	[Fact]
	public void Build_SgbExplicitlyDisabled_ClearsFlag() {
		var header = new GbHeaderBuilder()
			.SetSgbEnabled(false)
			.Build();

		Assert.Equal(0x00, header[70]);
	}

	#endregion

	#region Cartridge Type

	[Fact]
	public void Build_Default_CartridgeTypeRomOnly() {
		var header = new GbHeaderBuilder().Build();
		Assert.Equal(0x00, header[71]);
	}

	[Theory]
	[InlineData(GbCartridgeType.RomOnly, 0x00)]
	[InlineData(GbCartridgeType.Mbc1, 0x01)]
	[InlineData(GbCartridgeType.Mbc1Ram, 0x02)]
	[InlineData(GbCartridgeType.Mbc1RamBattery, 0x03)]
	[InlineData(GbCartridgeType.Mbc2, 0x05)]
	[InlineData(GbCartridgeType.Mbc3, 0x11)]
	[InlineData(GbCartridgeType.Mbc3RamBattery, 0x13)]
	[InlineData(GbCartridgeType.Mbc5, 0x19)]
	[InlineData(GbCartridgeType.Mbc5RamBattery, 0x1b)]
	[InlineData(GbCartridgeType.Mbc5Rumble, 0x1c)]
	public void Build_CartridgeType_SetsCorrectByte(GbCartridgeType type, byte expected) {
		var header = new GbHeaderBuilder()
			.SetCartridgeType(type)
			.Build();

		Assert.Equal(expected, header[71]);
	}

	#endregion

	#region ROM Size Code

	[Theory]
	[InlineData(32, 0x00)]    // 32KB = code 0
	[InlineData(64, 0x01)]    // 64KB = code 1
	[InlineData(128, 0x02)]   // 128KB = code 2
	[InlineData(256, 0x03)]   // 256KB = code 3
	[InlineData(512, 0x04)]   // 512KB = code 4
	[InlineData(1024, 0x05)]  // 1MB = code 5
	[InlineData(2048, 0x06)]  // 2MB = code 6
	[InlineData(4096, 0x07)]  // 4MB = code 7
	[InlineData(8192, 0x08)]  // 8MB = code 8
	public void Build_RomSize_CalculatesCorrectCode(int sizeKb, byte expectedCode) {
		var header = new GbHeaderBuilder()
			.SetRomSize(sizeKb)
			.Build();

		Assert.Equal(expectedCode, header[72]);
	}

	[Fact]
	public void Build_RomSizeSmallerThan32Kb_ReturnsCodeZero() {
		var header = new GbHeaderBuilder()
			.SetRomSize(16)
			.Build();

		Assert.Equal(0x00, header[72]);
	}

	#endregion

	#region RAM Size Code

	[Theory]
	[InlineData(0, 0x00)]     // No RAM
	[InlineData(2, 0x01)]     // 2KB (unused in practice)
	[InlineData(8, 0x02)]     // 8KB (1 bank)
	[InlineData(32, 0x03)]    // 32KB (4 banks)
	[InlineData(128, 0x04)]   // 128KB (16 banks)
	[InlineData(64, 0x05)]    // 64KB (8 banks)
	public void Build_RamSize_CalculatesCorrectCode(int sizeKb, byte expectedCode) {
		var header = new GbHeaderBuilder()
			.SetRamSize(sizeKb)
			.Build();

		Assert.Equal(expectedCode, header[73]);
	}

	[Fact]
	public void Build_RamSizeUnknown_ReturnsCodeZero() {
		var header = new GbHeaderBuilder()
			.SetRamSize(99) // Unknown size
			.Build();

		Assert.Equal(0x00, header[73]);
	}

	#endregion

	#region Region

	[Fact]
	public void Build_Default_RegionIsJapan() {
		var header = new GbHeaderBuilder().Build();
		Assert.Equal(0x00, header[74]);
	}

	[Fact]
	public void Build_RegionInternational_SetsCode() {
		var header = new GbHeaderBuilder()
			.SetRegion(GbRegion.International)
			.Build();

		Assert.Equal(0x01, header[74]);
	}

	#endregion

	#region Version

	[Fact]
	public void Build_Default_VersionZero() {
		var header = new GbHeaderBuilder().Build();
		Assert.Equal(0x00, header[76]);
	}

	[Fact]
	public void Build_CustomVersion_SetsCorrectly() {
		var header = new GbHeaderBuilder()
			.SetVersion(0x05)
			.Build();

		Assert.Equal(0x05, header[76]);
	}

	#endregion

	#region Header Checksum

	[Fact]
	public void Build_Default_CalculatesHeaderChecksum() {
		var header = new GbHeaderBuilder().Build();

		// Verify checksum manually: sum of bytes $0134-$014c (offsets 52-76)
		// checksum = 0 - header[52] - 1 - header[53] - 1 - ... - header[76] - 1
		int expected = 0;
		for (int i = 52; i <= 76; i++) {
			expected = expected - header[i] - 1;
		}
		expected &= 0xff;

		Assert.Equal((byte)expected, header[77]);
	}

	[Fact]
	public void Build_DifferentSettings_ChecksumChanges() {
		var defaultHeader = new GbHeaderBuilder().Build();
		var customHeader = new GbHeaderBuilder()
			.SetTitle("POKEMON")
			.SetCgbMode(GbCgbMode.CgbCompatible)
			.SetCartridgeType(GbCartridgeType.Mbc3RamBattery)
			.SetRomSize(512)
			.SetRamSize(32)
			.Build();

		// Checksums should differ
		Assert.NotEqual(defaultHeader[77], customHeader[77]);
	}

	#endregion

	#region Global Checksum

	[Fact]
	public void Build_GlobalChecksum_IsZero() {
		var header = new GbHeaderBuilder().Build();

		// Global checksum is set to 0 (calculated by emulator)
		Assert.Equal(0x00, header[78]);
		Assert.Equal(0x00, header[79]);
	}

	#endregion

	#region Fluent Builder Pattern

	[Fact]
	public void Builder_Methods_ReturnSameInstance() {
		var builder = new GbHeaderBuilder();

		Assert.Same(builder, builder.SetTitle("TEST"));
		Assert.Same(builder, builder.SetCgbMode(GbCgbMode.CgbOnly));
		Assert.Same(builder, builder.SetSgbEnabled(true));
		Assert.Same(builder, builder.SetCartridgeType(GbCartridgeType.Mbc5));
		Assert.Same(builder, builder.SetRomSize(256));
		Assert.Same(builder, builder.SetRamSize(8));
		Assert.Same(builder, builder.SetRegion(GbRegion.International));
		Assert.Same(builder, builder.SetVersion(1));
	}

	[Fact]
	public void Builder_FullChain_ProducesValidHeader() {
		var header = new GbHeaderBuilder()
			.SetTitle("ZELDA")
			.SetCgbMode(GbCgbMode.CgbCompatible)
			.SetSgbEnabled(true)
			.SetCartridgeType(GbCartridgeType.Mbc5RamBattery)
			.SetRomSize(1024)
			.SetRamSize(32)
			.SetRegion(GbRegion.International)
			.SetVersion(0)
			.Build();

		Assert.Equal(80, header.Length);
		Assert.Equal((byte)'Z', header[52]);
		Assert.Equal(0x80, header[67]); // CGB compatible
		Assert.Equal(0x03, header[70]); // SGB
		Assert.Equal(0x1b, header[71]); // MBC5+RAM+Battery
		Assert.Equal(0x05, header[72]); // 1024KB ROM
		Assert.Equal(0x03, header[73]); // 32KB RAM
		Assert.Equal(0x01, header[74]); // International
	}

	#endregion

	#region Enum Values

	[Fact]
	public void GbCgbMode_HasExpectedValues() {
		Assert.Equal(0x00, (byte)GbCgbMode.DmgOnly);
		Assert.Equal(0x80, (byte)GbCgbMode.CgbCompatible);
		Assert.Equal(0xc0, (byte)GbCgbMode.CgbOnly);
	}

	[Fact]
	public void GbRegion_HasExpectedValues() {
		Assert.Equal(0x00, (byte)GbRegion.Japan);
		Assert.Equal(0x01, (byte)GbRegion.International);
	}

	[Fact]
	public void GbCartridgeType_HasExpectedValues() {
		Assert.Equal(0x00, (byte)GbCartridgeType.RomOnly);
		Assert.Equal(0x01, (byte)GbCartridgeType.Mbc1);
		Assert.Equal(0x05, (byte)GbCartridgeType.Mbc2);
		Assert.Equal(0x11, (byte)GbCartridgeType.Mbc3);
		Assert.Equal(0x19, (byte)GbCartridgeType.Mbc5);
		Assert.Equal(0x1e, (byte)GbCartridgeType.Mbc5RumbleRamBattery);
	}

	#endregion
}
