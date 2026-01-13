using Poppy.Core.CodeGen;
using Xunit;

namespace Poppy.Tests.CodeGen;

public class INesHeaderBuilderTests {
	[Fact]
	public void Build_DefaultHeader_ReturnsValidINes2Header() {
		// arrange
		var builder = new INesHeaderBuilder();

		// act
		var header = builder.Build();

		// assert
		Assert.Equal(16, header.Length);
		Assert.Equal(0x4e, header[0]);  // 'N'
		Assert.Equal(0x45, header[1]);  // 'E'
		Assert.Equal(0x53, header[2]);  // 'S'
		Assert.Equal(0x1a, header[3]);  // MS-DOS EOF
		Assert.Equal(2, header[4]);     // 32KB PRG ROM (2 * 16KB)
		Assert.Equal(1, header[5]);     // 8KB CHR ROM
		Assert.Equal(0x01, header[6]);  // flags 6: vertical mirroring
		Assert.Equal(0x08, header[7]);  // flags 7: iNES 2.0 identifier
	}

	[Fact]
	public void Build_INes1Format_ReturnsValidINes1Header() {
		// arrange
		var builder = new INesHeaderBuilder()
			.SetINes2(false)
			.SetPrgRomSize(1)
			.SetChrRomSize(1)
			.SetMapper(0);

		// act
		var header = builder.Build();

		// assert
		Assert.Equal(16, header.Length);
		Assert.Equal(0x4e, header[0]);  // 'N'
		Assert.Equal(0x45, header[1]);  // 'E'
		Assert.Equal(0x53, header[2]);  // 'S'
		Assert.Equal(0x1a, header[3]);  // MS-DOS EOF
		Assert.Equal(1, header[4]);     // 16KB PRG ROM
		Assert.Equal(1, header[5]);     // 8KB CHR ROM
		Assert.Equal(0x01, header[6]);  // flags 6: vertical mirroring
		Assert.Equal(0x00, header[7]);  // flags 7: no iNES 2.0 identifier
	}

	[Fact]
	public void Build_WithMapper1_SetsMapperBitsCorrectly() {
		// arrange - MMC1 mapper
		var builder = new INesHeaderBuilder()
			.SetMapper(1);

		// act
		var header = builder.Build();

		// assert
		Assert.Equal(0x11, header[6]);  // flags 6: vertical mirroring + mapper low nybble
		Assert.Equal(0x08, header[7]);  // flags 7: iNES 2.0 identifier + mapper high nybble
		Assert.Equal(0x00, header[8]);  // mapper variant: submapper 0, mapper bits 8-11
	}

	[Fact]
	public void Build_WithMapper4_SetsMapperBitsCorrectly() {
		// arrange - MMC3 mapper
		var builder = new INesHeaderBuilder()
			.SetMapper(4);

		// act
		var header = builder.Build();

		// assert
		Assert.Equal(0x41, header[6]);  // flags 6: vertical mirroring + mapper low nybble (4)
		Assert.Equal(0x08, header[7]);  // flags 7: iNES 2.0 identifier
		Assert.Equal(0x00, header[8]);  // mapper variant: submapper 0
	}

	[Fact]
	public void Build_WithHighMapper_SetsExtendedMapperBits() {
		// arrange - mapper 256 (extended mapper number)
		var builder = new INesHeaderBuilder()
			.SetMapper(256);

		// act
		var header = builder.Build();

		// assert
		Assert.Equal(0x01, header[6]);  // flags 6: vertical mirroring + mapper low nybble (0)
		Assert.Equal(0x08, header[7]);  // flags 7: iNES 2.0 identifier + mapper high nybble (0)
		Assert.Equal(0x10, header[8]);  // mapper variant: bit 8 of mapper number
	}

	[Fact]
	public void Build_WithSubmapper_SetsSubmapperBits() {
		// arrange
		var builder = new INesHeaderBuilder()
			.SetMapper(1)
			.SetSubmapper(5);

		// act
		var header = builder.Build();

		// assert
		Assert.Equal(0x05, header[8] & 0x0f);   // submapper is in low nybble of byte 8
	}

	[Fact]
	public void Build_HorizontalMirroring_ClearsMirroringBit() {
		// arrange
		var builder = new INesHeaderBuilder()
			.SetMirroring(false);   // horizontal mirroring

		// act
		var header = builder.Build();

		// assert
		Assert.Equal(0x00, header[6] & 0x01);   // mirroring bit should be clear
	}

	[Fact]
	public void Build_FourScreenMode_SetsFourScreenBit() {
		// arrange
		var builder = new INesHeaderBuilder()
			.SetFourScreen(true);

		// act
		var header = builder.Build();

		// assert
		Assert.Equal(0x08, header[6] & 0x08);   // four-screen bit should be set
	}

	[Fact]
	public void Build_BatteryBacked_SetsBatteryBit() {
		// arrange
		var builder = new INesHeaderBuilder()
			.SetBatteryBacked(true);

		// act
		var header = builder.Build();

		// assert
		Assert.Equal(0x02, header[6] & 0x02);   // battery bit should be set
	}

	[Fact]
	public void Build_WithTrainer_SetsTrainerBit() {
		// arrange
		var builder = new INesHeaderBuilder()
			.SetTrainer(true);

		// act
		var header = builder.Build();

		// assert
		Assert.Equal(0x04, header[6] & 0x04);   // trainer bit should be set
	}

	[Fact]
	public void Build_PalSystem_SetsPalBit() {
		// arrange
		var builder = new INesHeaderBuilder()
			.SetPal(true);

		// act
		var header = builder.Build();

		// assert
		Assert.Equal(1, header[12]);    // byte 12: CPU/PPU timing (1 = PAL)
	}

	[Fact]
	public void Build_LargePrgRom_SetsExtendedSizeBits() {
		// arrange - 512KB PRG ROM (32 * 16KB units)
		var builder = new INesHeaderBuilder()
			.SetPrgRomSize(32);

		// act
		var header = builder.Build();

		// assert
		Assert.Equal(32, header[4]);            // LSB of PRG ROM size
		Assert.Equal(0x00, header[9] & 0xf0);   // MSB of PRG ROM size (should be 0 for size < 256)
	}

	[Fact]
	public void Build_LargeChrRom_SetsExtendedSizeBits() {
		// arrange - 128KB CHR ROM (16 * 8KB units)
		var builder = new INesHeaderBuilder()
			.SetChrRomSize(16);

		// act
		var header = builder.Build();

		// assert
		Assert.Equal(16, header[5]);            // LSB of CHR ROM size
		Assert.Equal(0x00, header[9] & 0x0f);   // MSB of CHR ROM size (should be 0 for size < 256)
	}

	[Fact]
	public void Build_FluentInterface_ChainsCorrectly() {
		// arrange & act
		var header = new INesHeaderBuilder()
			.SetPrgRomSize(2)
			.SetChrRomSize(1)
			.SetMapper(4)
			.SetSubmapper(0)
			.SetMirroring(false)
			.SetBatteryBacked(true)
			.SetPal(false)
			.Build();

		// assert
		Assert.Equal(16, header.Length);
		Assert.Equal(2, header[4]);     // PRG ROM size
		Assert.Equal(1, header[5]);     // CHR ROM size
		Assert.Equal(0x42, header[6]);  // flags 6: horizontal mirroring, battery, mapper 4 low
		Assert.Equal(0x08, header[7]);  // flags 7: iNES 2.0
		Assert.Equal(0, header[12]);    // NTSC
	}
}
