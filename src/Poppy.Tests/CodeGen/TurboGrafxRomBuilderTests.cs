// TurboGrafxRomBuilderTests.cs
// Unit tests for TurboGrafx-16 / PC Engine ROM builder

using Poppy.Core.CodeGen;
using Xunit;

namespace Poppy.Tests.CodeGen;

/// <summary>
/// Unit tests for the TurboGrafx-16 ROM builder.
/// </summary>
public class TurboGrafxRomBuilderTests {
	#region ROM Building Tests

	[Fact]
	public void Build_Default_ReturnsCorrectSize() {
		var builder = new TurboGrafxRomBuilder();
		var rom = builder.Build();

		Assert.Equal(0x8000, rom.Length);  // 32KB default
	}

	[Fact]
	public void Build_CustomSize_ReturnsCorrectSize() {
		var builder = new TurboGrafxRomBuilder();
		builder.SetRomSize(0x10000);  // 64KB
		var rom = builder.Build();

		Assert.Equal(0x10000, rom.Length);
	}

	[Fact]
	public void Build_HasResetVector() {
		var builder = new TurboGrafxRomBuilder();
		builder.SetEntryPoint(0xe000);
		var rom = builder.Build();

		// Reset vector at end of ROM (last 2 bytes)
		ushort resetVector = (ushort)(rom[rom.Length - 2] | (rom[rom.Length - 1] << 8));
		Assert.Equal(0xe000, resetVector);
	}

	[Fact]
	public void Build_WithProgramData_IncludesData() {
		var builder = new TurboGrafxRomBuilder();
		var program = new byte[] { 0x78, 0xd8, 0xa2, 0xff };
		builder.SetProgramData(program);
		var rom = builder.Build();

		// Check program data at start of ROM
		Assert.Equal(0x78, rom[0]);
		Assert.Equal(0xd8, rom[1]);
		Assert.Equal(0xa2, rom[2]);
		Assert.Equal(0xff, rom[3]);
	}

	#endregion

	#region Size Validation Tests

	[Theory]
	[InlineData(0x2000)]   // 8KB
	[InlineData(0x4000)]   // 16KB
	[InlineData(0x8000)]   // 32KB
	[InlineData(0x10000)]  // 64KB
	[InlineData(0x20000)]  // 128KB
	[InlineData(0x40000)]  // 256KB
	[InlineData(0x80000)]  // 512KB
	[InlineData(0x100000)] // 1MB
	public void SetRomSize_ValidSizes_Succeeds(int size) {
		var builder = new TurboGrafxRomBuilder();
		builder.SetRomSize(size);
		var rom = builder.Build();

		Assert.Equal(size, rom.Length);
	}

	[Fact]
	public void SetRomSize_TooSmall_ThrowsException() {
		var builder = new TurboGrafxRomBuilder();
		Assert.Throws<ArgumentOutOfRangeException>(() => builder.SetRomSize(0x1000));  // 4KB - too small
	}

	[Fact]
	public void SetRomSize_TooLarge_ThrowsException() {
		var builder = new TurboGrafxRomBuilder();
		Assert.Throws<ArgumentOutOfRangeException>(() => builder.SetRomSize(0x200000));  // 2MB - too large
	}

	[Fact]
	public void SetRomSize_NotPowerOf2_ThrowsException() {
		var builder = new TurboGrafxRomBuilder();
		Assert.Throws<ArgumentException>(() => builder.SetRomSize(0x9000));  // Not a power of 2
	}

	#endregion

	#region Minimal ROM Tests

	[Fact]
	public void CreateMinimalRom_ReturnsValidRom() {
		var rom = TurboGrafxRomBuilder.CreateMinimalRom();

		Assert.Equal(0x8000, rom.Length);
		Assert.True(TurboGrafxRomBuilder.ValidateRom(rom));
	}

	[Fact]
	public void CreateMinimalRom_CustomSize_ReturnsCorrectSize() {
		var rom = TurboGrafxRomBuilder.CreateMinimalRom(0x10000);

		Assert.Equal(0x10000, rom.Length);
	}

	[Fact]
	public void CreateMinimalRom_ContainsStartupCode() {
		var rom = TurboGrafxRomBuilder.CreateMinimalRom();

		// First instruction should be SEI ($78)
		Assert.Equal(0x78, rom[0]);
	}

	#endregion

	#region Validation Tests

	[Fact]
	public void ValidateRom_ValidRom_ReturnsTrue() {
		var builder = new TurboGrafxRomBuilder();
		var rom = builder.Build();

		Assert.True(TurboGrafxRomBuilder.ValidateRom(rom));
	}

	[Fact]
	public void ValidateRom_TooShort_ReturnsFalse() {
		var shortRom = new byte[0x1000];
		Assert.False(TurboGrafxRomBuilder.ValidateRom(shortRom));
	}

	[Fact]
	public void ValidateRom_Null_ReturnsFalse() {
		Assert.False(TurboGrafxRomBuilder.ValidateRom(null!));
	}

	[Fact]
	public void ValidateRom_NotPowerOf2_ReturnsFalse() {
		var oddSizeRom = new byte[0x9000];
		Assert.False(TurboGrafxRomBuilder.ValidateRom(oddSizeRom));
	}

	#endregion

	#region Bank Count Tests

	[Theory]
	[InlineData(0x2000, 1)]
	[InlineData(0x4000, 2)]
	[InlineData(0x8000, 4)]
	[InlineData(0x10000, 8)]
	[InlineData(0x100000, 128)]
	public void GetBankCount_ReturnsCorrectCount(int romSize, int expected) {
		Assert.Equal(expected, TurboGrafxRomBuilder.GetBankCount(romSize));
	}

	#endregion

	#region Checksum Tests

	[Fact]
	public void CalculateChecksum_EmptyRom_ReturnsZero() {
		var rom = new byte[0x8000];
		var checksum = TurboGrafxRomBuilder.CalculateChecksum(rom);

		Assert.Equal(0, checksum);
	}

	[Fact]
	public void CalculateChecksum_WithData_ReturnsSum() {
		var rom = new byte[] { 0x01, 0x02, 0x03, 0x04 };
		var checksum = TurboGrafxRomBuilder.CalculateChecksum(rom);

		Assert.Equal(10, checksum);  // 1+2+3+4 = 10
	}

	[Fact]
	public void CalculateChecksum_Null_ReturnsZero() {
		var checksum = TurboGrafxRomBuilder.CalculateChecksum(null!);
		Assert.Equal(0, checksum);
	}

	#endregion

	#region System Type Tests

	[Fact]
	public void GetRequiredSystem_Standard_ReturnsStandard() {
		var rom = TurboGrafxRomBuilder.CreateMinimalRom();
		var system = TurboGrafxRomBuilder.GetRequiredSystem(rom);

		Assert.Equal(TurboGrafxRomBuilder.SystemType.Standard, system);
	}

	[Fact]
	public void GetRequiredSystem_SuperGrafx_ReturnsSuperGrafx() {
		var rom = TurboGrafxRomBuilder.CreateMinimalRom();
		var system = TurboGrafxRomBuilder.GetRequiredSystem(rom, isSupergrafxCompatible: true);

		Assert.Equal(TurboGrafxRomBuilder.SystemType.SuperGrafx, system);
	}

	#endregion

	#region I/O Address Constants Tests

	[Fact]
	public void IoAddresses_HasCorrectValues() {
		Assert.Equal(0x0000, TurboGrafxRomBuilder.IoAddresses.VdcAddressStatus);
		Assert.Equal(0x0002, TurboGrafxRomBuilder.IoAddresses.VdcDataLow);
		Assert.Equal(0x1000, TurboGrafxRomBuilder.IoAddresses.JoypadPort);
	}

	#endregion

	#region Builder Chaining Tests

	[Fact]
	public void Builder_SupportsChaining() {
		var rom = new TurboGrafxRomBuilder()
			.SetRomSize(0x10000)
			.SetTitle("CHAINED")
			.SetEntryPoint(0xe000)
			.SetSupergrafxCompatible(false)
			.Build();

		Assert.Equal(0x10000, rom.Length);
	}

	#endregion
}
