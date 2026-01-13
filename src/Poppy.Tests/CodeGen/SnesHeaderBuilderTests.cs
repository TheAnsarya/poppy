// ============================================================================
// SnesHeaderBuilderTests.cs - Unit Tests for SNES Header Generation
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Poppy.Core.CodeGen;

namespace Poppy.Tests.CodeGen;

/// <summary>
/// Tests for SNES ROM header generation.
/// </summary>
public class SnesHeaderBuilderTests {
	[Fact]
	public void Build_DefaultHeader_HasCorrectSize() {
		// Arrange
		var builder = new SnesHeaderBuilder();

		// Act
		var header = builder.Build();

		// Assert
		Assert.Equal(64, header.Length);
	}

	[Fact]
	public void Build_WithTitle_SetsTitle() {
		// Arrange
		var builder = new SnesHeaderBuilder()
			.SetTitle("SUPER MARIO WORLD");

		// Act
		var header = builder.Build();

		// Assert - Title should be padded to 21 characters
		var title = System.Text.Encoding.ASCII.GetString(header, 0, 21).TrimEnd();
		Assert.Equal("SUPER MARIO WORLD", title);
	}

	[Fact]
	public void Build_LongTitle_TruncatesTo21Characters() {
		// Arrange
		var builder = new SnesHeaderBuilder()
			.SetTitle("THIS TITLE IS WAY TOO LONG FOR SNES");

		// Act
		var header = builder.Build();

		// Assert
		var title = System.Text.Encoding.ASCII.GetString(header, 0, 21);
		Assert.Equal("THIS TITLE IS WAY TOO", title);
	}

	[Fact]
	public void Build_ShortTitle_PaddedWithSpaces() {
		// Arrange
		var builder = new SnesHeaderBuilder()
			.SetTitle("SHORT");

		// Act
		var header = builder.Build();

		// Assert
		var title = System.Text.Encoding.ASCII.GetString(header, 0, 21);
		Assert.Equal("SHORT                ", title);
	}

	[Fact]
	public void Build_LoRomMode_SetsCorrectMapByte() {
		// Arrange
		var builder = new SnesHeaderBuilder()
			.SetMapMode(SnesMapMode.LoRom);

		// Act
		var header = builder.Build();

		// Assert - Map mode at offset $15
		Assert.Equal(0x20, header[0x15]);
	}

	[Fact]
	public void Build_HiRomMode_SetsCorrectMapByte() {
		// Arrange
		var builder = new SnesHeaderBuilder()
			.SetMapMode(SnesMapMode.HiRom);

		// Act
		var header = builder.Build();

		// Assert
		Assert.Equal(0x21, header[0x15]);
	}

	[Fact]
	public void Build_FastRom_SetsBit() {
		// Arrange
		var builder = new SnesHeaderBuilder()
			.SetMapMode(SnesMapMode.LoRom)
			.SetFastRom(true);

		// Act
		var header = builder.Build();

		// Assert - Fast ROM bit is $10
		Assert.Equal(0x30, header[0x15]); // $20 | $10
	}

	[Fact]
	public void Build_CartridgeType_SetsCorrectly() {
		// Arrange
		var builder = new SnesHeaderBuilder()
			.SetCartridgeType(SnesCartridgeType.RomRamBattery);

		// Act
		var header = builder.Build();

		// Assert - Cartridge type at offset $16
		Assert.Equal(0x02, header[0x16]);
	}

	[Fact]
	public void Build_SuperFx_SetsCartridgeType() {
		// Arrange
		var builder = new SnesHeaderBuilder()
			.SetCartridgeType(SnesCartridgeType.RomSuperFxRamBattery);

		// Act
		var header = builder.Build();

		// Assert
		Assert.Equal(0x15, header[0x16]);
	}

	[Fact]
	public void Build_RomSize256K_SetsCorrectCode() {
		// Arrange
		var builder = new SnesHeaderBuilder()
			.SetRomSize(256);

		// Act
		var header = builder.Build();

		// Assert - 256KB = 2^8 KB, code = 8
		Assert.Equal(8, header[0x17]);
	}

	[Fact]
	public void Build_RomSize1M_SetsCorrectCode() {
		// Arrange
		var builder = new SnesHeaderBuilder()
			.SetRomSize(1024);

		// Act
		var header = builder.Build();

		// Assert - 1024KB = 2^10 KB, code = 10
		Assert.Equal(10, header[0x17]);
	}

	[Fact]
	public void Build_RomSize4M_SetsCorrectCode() {
		// Arrange
		var builder = new SnesHeaderBuilder()
			.SetRomSize(4096);

		// Act
		var header = builder.Build();

		// Assert - 4096KB = 2^12 KB, code = 12
		Assert.Equal(12, header[0x17]);
	}

	[Fact]
	public void Build_RamSize0_SetsZero() {
		// Arrange
		var builder = new SnesHeaderBuilder()
			.SetRamSize(0);

		// Act
		var header = builder.Build();

		// Assert
		Assert.Equal(0, header[0x18]);
	}

	[Fact]
	public void Build_RamSize8K_SetsCorrectCode() {
		// Arrange
		var builder = new SnesHeaderBuilder()
			.SetRamSize(8);

		// Act
		var header = builder.Build();

		// Assert - 8KB = 2^3 KB, code = 3
		Assert.Equal(3, header[0x18]);
	}

	[Fact]
	public void Build_RegionJapan_SetsCode() {
		// Arrange
		var builder = new SnesHeaderBuilder()
			.SetRegion(SnesRegion.Japan);

		// Act
		var header = builder.Build();

		// Assert
		Assert.Equal(0x00, header[0x19]);
	}

	[Fact]
	public void Build_RegionNorthAmerica_SetsCode() {
		// Arrange
		var builder = new SnesHeaderBuilder()
			.SetRegion(SnesRegion.NorthAmerica);

		// Act
		var header = builder.Build();

		// Assert
		Assert.Equal(0x01, header[0x19]);
	}

	[Fact]
	public void Build_RegionEurope_SetsCode() {
		// Arrange
		var builder = new SnesHeaderBuilder()
			.SetRegion(SnesRegion.Europe);

		// Act
		var header = builder.Build();

		// Assert
		Assert.Equal(0x02, header[0x19]);
	}

	[Fact]
	public void Build_DeveloperId_SetsCorrectly() {
		// Arrange
		var builder = new SnesHeaderBuilder()
			.SetDeveloperId(0x33);

		// Act
		var header = builder.Build();

		// Assert
		Assert.Equal(0x33, header[0x1a]);
	}

	[Fact]
	public void Build_Version_SetsCorrectly() {
		// Arrange
		var builder = new SnesHeaderBuilder()
			.SetVersion(0x01);

		// Act
		var header = builder.Build();

		// Assert
		Assert.Equal(0x01, header[0x1b]);
	}

	[Fact]
	public void Build_DefaultChecksums_ArePlaceholders() {
		// Arrange
		var builder = new SnesHeaderBuilder();

		// Act
		var header = builder.Build();

		// Assert - Checksum complement should be $ffff, checksum $0000
		Assert.Equal(0xff, header[0x1c]);
		Assert.Equal(0xff, header[0x1d]);
		Assert.Equal(0x00, header[0x1e]);
		Assert.Equal(0x00, header[0x1f]);
	}

	[Fact]
	public void Build_EmulationResetVector_SetsCorrectly() {
		// Arrange
		var builder = new SnesHeaderBuilder()
			.SetEmulationReset(0x8000);

		// Act
		var header = builder.Build();

		// Assert - RESET vector at offset $3c (little-endian)
		Assert.Equal(0x00, header[0x3c]); // Low byte
		Assert.Equal(0x80, header[0x3d]); // High byte
	}

	[Fact]
	public void Build_NativeNmiVector_SetsCorrectly() {
		// Arrange
		var builder = new SnesHeaderBuilder()
			.SetNativeNmi(0x8100);

		// Act
		var header = builder.Build();

		// Assert - Native NMI at offset $2a
		Assert.Equal(0x00, header[0x2a]);
		Assert.Equal(0x81, header[0x2b]);
	}

	[Fact]
	public void Build_EmulationNmiVector_SetsCorrectly() {
		// Arrange
		var builder = new SnesHeaderBuilder()
			.SetEmulationNmi(0x8200);

		// Act
		var header = builder.Build();

		// Assert - Emulation NMI at offset $3a
		Assert.Equal(0x00, header[0x3a]);
		Assert.Equal(0x82, header[0x3b]);
	}

	[Fact]
	public void GetHeaderOffset_LoRom_Returns0x7fc0() {
		// Arrange
		var builder = new SnesHeaderBuilder()
			.SetMapMode(SnesMapMode.LoRom);

		// Act
		var offset = builder.GetHeaderOffset();

		// Assert
		Assert.Equal(0x7fc0, offset);
	}

	[Fact]
	public void GetHeaderOffset_HiRom_Returns0xffc0() {
		// Arrange
		var builder = new SnesHeaderBuilder()
			.SetMapMode(SnesMapMode.HiRom);

		// Act
		var offset = builder.GetHeaderOffset();

		// Assert
		Assert.Equal(0xffc0, offset);
	}

	[Fact]
	public void CalculateChecksum_ZeroFilled_ReturnsCorrectSum() {
		// Arrange
		var rom = new byte[0x8000]; // 32KB

		// Act
		var checksum = SnesHeaderBuilder.CalculateChecksum(rom);

		// Assert - All zeros = 0
		Assert.Equal(0x0000, checksum);
	}

	[Fact]
	public void CalculateChecksum_AllOnes_ReturnsCorrectSum() {
		// Arrange
		var rom = new byte[256]; // Small test
		for (int i = 0; i < rom.Length; i++) {
			rom[i] = 0xff;
		}

		// Act
		var checksum = SnesHeaderBuilder.CalculateChecksum(rom);

		// Assert - 256 * $ff = $ff00 (low 16 bits)
		Assert.Equal(0xff00, checksum);
	}

	[Fact]
	public void UpdateChecksum_ModifiesRomInPlace() {
		// Arrange
		var rom = new byte[0x8000]; // 32KB
		var builder = new SnesHeaderBuilder()
			.SetMapMode(SnesMapMode.LoRom);
		var headerOffset = builder.GetHeaderOffset();

		// Fill with some data
		for (int i = 0; i < rom.Length; i++) {
			rom[i] = (byte)(i & 0xff);
		}

		// Act
		SnesHeaderBuilder.UpdateChecksum(rom, headerOffset);

		// Assert - Checksum and complement should be valid
		ushort checksum = (ushort)(rom[headerOffset + 0x1e] | (rom[headerOffset + 0x1f] << 8));
		ushort complement = (ushort)(rom[headerOffset + 0x1c] | (rom[headerOffset + 0x1d] << 8));
		Assert.Equal((ushort)(checksum ^ 0xffff), complement);
	}

	[Fact]
	public void CreateSmcHeader_Returns512Bytes() {
		// Act
		var header = SnesHeaderBuilder.CreateSmcHeader(1024);

		// Assert
		Assert.Equal(512, header.Length);
	}

	[Fact]
	public void CreateSmcHeader_SetsRomSizeInUnits() {
		// Arrange & Act
		var header = SnesHeaderBuilder.CreateSmcHeader(1024); // 1024KB = 128 8KB units

		// Assert
		int units = header[0] | (header[1] << 8);
		Assert.Equal(128, units);
	}

	[Fact]
	public void Build_CompleteHeader_AllFieldsSet() {
		// Arrange
		var builder = new SnesHeaderBuilder()
			.SetTitle("FINAL FANTASY III")
			.SetMapMode(SnesMapMode.HiRom)
			.SetFastRom(true)
			.SetCartridgeType(SnesCartridgeType.RomRamBattery)
			.SetRomSize(3072) // 3 MB (rounds up to 4MB)
			.SetRamSize(64)
			.SetRegion(SnesRegion.Japan)
			.SetDeveloperId(0xc3)
			.SetVersion(0x00)
			.SetEmulationReset(0x8000)
			.SetNativeNmi(0x00c0);

		// Act
		var header = builder.Build();

		// Assert
		Assert.Equal(64, header.Length);
		Assert.Equal(0x31, header[0x15]); // HiROM + FastROM
		Assert.Equal(0x02, header[0x16]); // ROM+RAM+Battery
		Assert.Equal(0xc3, header[0x1a]); // Developer ID
	}

	[Fact]
	public void Build_AllVectors_SetCorrectly() {
		// Arrange
		var builder = new SnesHeaderBuilder()
			.SetNativeCop(0x1000)
			.SetNativeBrk(0x1100)
			.SetNativeAbort(0x1200)
			.SetNativeNmi(0x1300)
			.SetNativeIrq(0x1400)
			.SetEmulationCop(0x2000)
			.SetEmulationAbort(0x2100)
			.SetEmulationNmi(0x2200)
			.SetEmulationReset(0x2300)
			.SetEmulationIrq(0x2400);

		// Act
		var header = builder.Build();

		// Assert native vectors
		Assert.Equal(0x00, header[0x24]); // COP low
		Assert.Equal(0x10, header[0x25]); // COP high
		Assert.Equal(0x00, header[0x26]); // BRK low
		Assert.Equal(0x11, header[0x27]); // BRK high
		Assert.Equal(0x00, header[0x28]); // ABORT low
		Assert.Equal(0x12, header[0x29]); // ABORT high
		Assert.Equal(0x00, header[0x2a]); // NMI low
		Assert.Equal(0x13, header[0x2b]); // NMI high
		Assert.Equal(0x00, header[0x2e]); // IRQ low
		Assert.Equal(0x14, header[0x2f]); // IRQ high

		// Assert emulation vectors
		Assert.Equal(0x00, header[0x34]); // COP low
		Assert.Equal(0x20, header[0x35]); // COP high
		Assert.Equal(0x00, header[0x38]); // ABORT low
		Assert.Equal(0x21, header[0x39]); // ABORT high
		Assert.Equal(0x00, header[0x3a]); // NMI low
		Assert.Equal(0x22, header[0x3b]); // NMI high
		Assert.Equal(0x00, header[0x3c]); // RESET low
		Assert.Equal(0x23, header[0x3d]); // RESET high
		Assert.Equal(0x00, header[0x3e]); // IRQ low
		Assert.Equal(0x24, header[0x3f]); // IRQ high
	}
}
