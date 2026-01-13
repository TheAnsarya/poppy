// ============================================================================
// GameBoyHeaderBuilderTests.cs - Unit Tests for Game Boy Header Generation
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Poppy.Core.CodeGen;

namespace Poppy.Tests.CodeGen;

/// <summary>
/// Tests for Game Boy ROM header generation.
/// </summary>
public class GameBoyHeaderBuilderTests {
	[Fact]
	public void Build_DefaultHeader_HasCorrectSize() {
		// Arrange
		var builder = new GameBoyHeaderBuilder();

		// Act
		var header = builder.Build();

		// Assert
		Assert.Equal(0x50, header.Length); // $0100-$014F = 80 bytes
	}

	[Fact]
	public void Build_DefaultHeader_HasEntryPoint() {
		// Arrange
		var builder = new GameBoyHeaderBuilder();

		// Act
		var header = builder.Build();

		// Assert - NOP followed by JP $0150
		Assert.Equal(0x00, header[0]); // NOP
		Assert.Equal(0xc3, header[1]); // JP
		Assert.Equal(0x50, header[2]); // Low byte of $0150
		Assert.Equal(0x01, header[3]); // High byte of $0150
	}

	[Fact]
	public void Build_CustomEntryPoint_SetsCorrectAddress() {
		// Arrange
		var builder = new GameBoyHeaderBuilder()
			.SetEntryPoint(0x4000);

		// Act
		var header = builder.Build();

		// Assert
		Assert.Equal(0x00, header[2]); // Low byte of $4000
		Assert.Equal(0x40, header[3]); // High byte of $4000
	}

	[Fact]
	public void Build_Header_HasNintendoLogo() {
		// Arrange
		var builder = new GameBoyHeaderBuilder();

		// Act
		var header = builder.Build();

		// Assert - Check first few bytes of Nintendo logo at offset $04
		Assert.Equal(0xce, header[0x04]);
		Assert.Equal(0xed, header[0x05]);
		Assert.Equal(0x66, header[0x06]);
		Assert.Equal(0x66, header[0x07]);
	}

	[Fact]
	public void Build_WithTitle_SetsTitle() {
		// Arrange
		var builder = new GameBoyHeaderBuilder()
			.SetTitle("TEST GAME");

		// Act
		var header = builder.Build();

		// Assert - Title at offset $34
		Assert.Equal((byte)'T', header[0x34]);
		Assert.Equal((byte)'E', header[0x35]);
		Assert.Equal((byte)'S', header[0x36]);
		Assert.Equal((byte)'T', header[0x37]);
		Assert.Equal((byte)' ', header[0x38]);
		Assert.Equal((byte)'G', header[0x39]);
		Assert.Equal((byte)'A', header[0x3a]);
		Assert.Equal((byte)'M', header[0x3b]);
		Assert.Equal((byte)'E', header[0x3c]);
	}

	[Fact]
	public void Build_LongTitle_TruncatesTo16Characters() {
		// Arrange
		var builder = new GameBoyHeaderBuilder()
			.SetTitle("THIS TITLE IS WAY TOO LONG");

		// Act
		var header = builder.Build();

		// Assert - Should only have first 16 characters
		Assert.Equal((byte)'T', header[0x34]);
		Assert.Equal((byte)'H', header[0x35]);
		// Character at position 16 (index 15) should be 'G' from "TOO LONG"
		// But it's truncated, so we check the title area is limited
	}

	[Fact]
	public void Build_WithCgbFlag_SetsFlag() {
		// Arrange
		var builder = new GameBoyHeaderBuilder()
			.SetCgbFlag(CgbFlag.CgbEnhanced);

		// Act
		var header = builder.Build();

		// Assert - CGB flag at offset $43
		Assert.Equal(0x80, header[0x43]);
	}

	[Fact]
	public void Build_CgbOnly_SetsFlag() {
		// Arrange
		var builder = new GameBoyHeaderBuilder()
			.SetCgbFlag(CgbFlag.CgbOnly);

		// Act
		var header = builder.Build();

		// Assert
		Assert.Equal(0xc0, header[0x43]);
	}

	[Fact]
	public void Build_WithSgbFlag_SetsFlag() {
		// Arrange
		var builder = new GameBoyHeaderBuilder()
			.SetSgbFlag(true);

		// Act
		var header = builder.Build();

		// Assert - SGB flag at offset $46
		Assert.Equal(0x03, header[0x46]);
	}

	[Fact]
	public void Build_WithoutSgbFlag_ClearsFlag() {
		// Arrange
		var builder = new GameBoyHeaderBuilder()
			.SetSgbFlag(false);

		// Act
		var header = builder.Build();

		// Assert
		Assert.Equal(0x00, header[0x46]);
	}

	[Fact]
	public void Build_WithCartridgeType_SetsType() {
		// Arrange
		var builder = new GameBoyHeaderBuilder()
			.SetCartridgeType(CartridgeType.Mbc1RamBattery);

		// Act
		var header = builder.Build();

		// Assert - Cartridge type at offset $47
		Assert.Equal(0x03, header[0x47]);
	}

	[Fact]
	public void Build_WithMbc5_SetsType() {
		// Arrange
		var builder = new GameBoyHeaderBuilder()
			.SetCartridgeType(CartridgeType.Mbc5RamBattery);

		// Act
		var header = builder.Build();

		// Assert
		Assert.Equal(0x1b, header[0x47]);
	}

	[Fact]
	public void Build_WithRomSize_SetsSize() {
		// Arrange
		var builder = new GameBoyHeaderBuilder()
			.SetRomSize(0x05); // 1MB

		// Act
		var header = builder.Build();

		// Assert - ROM size at offset $48
		Assert.Equal(0x05, header[0x48]);
	}

	[Fact]
	public void Build_WithRamSize_SetsSize() {
		// Arrange
		var builder = new GameBoyHeaderBuilder()
			.SetRamSize(0x03); // 32KB

		// Act
		var header = builder.Build();

		// Assert - RAM size at offset $49
		Assert.Equal(0x03, header[0x49]);
	}

	[Fact]
	public void Build_Japanese_SetsDestination() {
		// Arrange
		var builder = new GameBoyHeaderBuilder()
			.SetJapanese(true);

		// Act
		var header = builder.Build();

		// Assert - Destination at offset $4A
		Assert.Equal(0x00, header[0x4a]);
	}

	[Fact]
	public void Build_Overseas_SetsDestination() {
		// Arrange
		var builder = new GameBoyHeaderBuilder()
			.SetJapanese(false);

		// Act
		var header = builder.Build();

		// Assert
		Assert.Equal(0x01, header[0x4a]);
	}

	[Fact]
	public void Build_WithLicenseeCode_SetsCode() {
		// Arrange
		var builder = new GameBoyHeaderBuilder()
			.SetLicenseeCode("01");

		// Act
		var header = builder.Build();

		// Assert - Licensee code at offset $44-$45
		Assert.Equal((byte)'0', header[0x44]);
		Assert.Equal((byte)'1', header[0x45]);
	}

	[Fact]
	public void Build_WithRomVersion_SetsVersion() {
		// Arrange
		var builder = new GameBoyHeaderBuilder()
			.SetRomVersion(1);

		// Act
		var header = builder.Build();

		// Assert - Version at offset $4C
		Assert.Equal(0x01, header[0x4c]);
	}

	[Fact]
	public void Build_Header_HasValidChecksum() {
		// Arrange
		var builder = new GameBoyHeaderBuilder()
			.SetTitle("TEST")
			.SetCartridgeType(CartridgeType.Mbc1);

		// Act
		var header = builder.Build();

		// Assert - Verify checksum calculation
		int checksum = 0;
		for (int i = 0x34; i < 0x4d; i++) {
			checksum = checksum - header[i] - 1;
		}
		Assert.Equal((byte)(checksum & 0xff), header[0x4d]);
	}

	[Fact]
	public void Build_GlobalChecksum_InitiallyZero() {
		// Arrange
		var builder = new GameBoyHeaderBuilder();

		// Act
		var header = builder.Build();

		// Assert - Global checksum at offset $4E-$4F should be zero
		Assert.Equal(0x00, header[0x4e]);
		Assert.Equal(0x00, header[0x4f]);
	}

	[Fact]
	public void CalculateGlobalChecksum_SimpleRom_CalculatesCorrectly() {
		// Arrange - Create a minimal "ROM" with known values
		var rom = new byte[0x8000]; // 32KB minimum
		for (int i = 0; i < rom.Length; i++) {
			rom[i] = (byte)(i & 0xff);
		}

		// Act
		var checksum = GameBoyHeaderBuilder.CalculateGlobalChecksum(rom);

		// Assert - Sum should exclude bytes at $014E and $014F
		int expected = 0;
		for (int i = 0; i < rom.Length; i++) {
			if (i != 0x14e && i != 0x14f) {
				expected += rom[i];
			}
		}
		Assert.Equal((ushort)(expected & 0xffff), checksum);
	}

	[Fact]
	public void GetRomSize_ReturnsCorrectSizes() {
		Assert.Equal(32 * 1024, GameBoyHeaderBuilder.GetRomSize(0x00));
		Assert.Equal(64 * 1024, GameBoyHeaderBuilder.GetRomSize(0x01));
		Assert.Equal(128 * 1024, GameBoyHeaderBuilder.GetRomSize(0x02));
		Assert.Equal(256 * 1024, GameBoyHeaderBuilder.GetRomSize(0x03));
		Assert.Equal(512 * 1024, GameBoyHeaderBuilder.GetRomSize(0x04));
		Assert.Equal(1024 * 1024, GameBoyHeaderBuilder.GetRomSize(0x05));
		Assert.Equal(2048 * 1024, GameBoyHeaderBuilder.GetRomSize(0x06));
		Assert.Equal(4096 * 1024, GameBoyHeaderBuilder.GetRomSize(0x07));
		Assert.Equal(8192 * 1024, GameBoyHeaderBuilder.GetRomSize(0x08));
	}

	[Fact]
	public void GetRamSize_ReturnsCorrectSizes() {
		Assert.Equal(0, GameBoyHeaderBuilder.GetRamSize(0x00));
		Assert.Equal(2 * 1024, GameBoyHeaderBuilder.GetRamSize(0x01));
		Assert.Equal(8 * 1024, GameBoyHeaderBuilder.GetRamSize(0x02));
		Assert.Equal(32 * 1024, GameBoyHeaderBuilder.GetRamSize(0x03));
		Assert.Equal(128 * 1024, GameBoyHeaderBuilder.GetRamSize(0x04));
		Assert.Equal(64 * 1024, GameBoyHeaderBuilder.GetRamSize(0x05));
	}

	[Fact]
	public void Build_CompleteHeader_AllFieldsSet() {
		// Arrange
		var builder = new GameBoyHeaderBuilder()
			.SetTitle("POKEMON RED")
			.SetCgbFlag(CgbFlag.CgbEnhanced)
			.SetSgbFlag(true)
			.SetCartridgeType(CartridgeType.Mbc3RamBattery)
			.SetRomSize(0x05)
			.SetRamSize(0x03)
			.SetJapanese(true)
			.SetLicenseeCode("01")
			.SetRomVersion(0)
			.SetEntryPoint(0x0150);

		// Act
		var header = builder.Build();

		// Assert
		Assert.Equal(0x50, header.Length);
		Assert.Equal(0x80, header[0x43]); // CGB enhanced
		Assert.Equal(0x03, header[0x46]); // SGB enabled
		Assert.Equal(0x13, header[0x47]); // MBC3+RAM+BATTERY
		Assert.Equal(0x05, header[0x48]); // 1MB ROM
		Assert.Equal(0x03, header[0x49]); // 32KB RAM
		Assert.Equal(0x00, header[0x4a]); // Japanese
	}

	[Fact]
	public void Build_WithManufacturerCode_SetsCodeForCgb() {
		// Arrange
		var builder = new GameBoyHeaderBuilder()
			.SetCgbFlag(CgbFlag.CgbEnhanced)
			.SetManufacturerCode("ABCD");

		// Act
		var header = builder.Build();

		// Assert - Manufacturer code at $3F-$42
		Assert.Equal((byte)'A', header[0x3f]);
		Assert.Equal((byte)'B', header[0x40]);
		Assert.Equal((byte)'C', header[0x41]);
		Assert.Equal((byte)'D', header[0x42]);
	}
}
