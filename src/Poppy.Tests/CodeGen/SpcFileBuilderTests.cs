// SpcFileBuilderTests.cs
// Unit tests for SPC file format builder (SNES audio)

using Poppy.Core.CodeGen;
using Xunit;

namespace Poppy.Tests.CodeGen;

/// <summary>
/// Unit tests for the SPC file format builder.
/// </summary>
public class SpcFileBuilderTests {
	#region File Building Tests

	[Fact]
	public void Build_ReturnsCorrectSize() {
		var builder = new SpcFileBuilder();
		var spc = builder.Build();

		// SPC file is 65,984 bytes (256 header + 64KB RAM + 128 DSP + 64 extra)
		// 256 + 65536 + 128 + 64 = 65984 (0x101c0)
		Assert.Equal(65984, spc.Length);
	}

	[Fact]
	public void Build_HasCorrectSignature() {
		var builder = new SpcFileBuilder();
		var spc = builder.Build();

		// "SNES-SPC700 Sound File Data v0.30"
		string signature = System.Text.Encoding.ASCII.GetString(spc, 0, 33);
		Assert.Equal("SNES-SPC700 Sound File Data v0.30", signature);
	}

	[Fact]
	public void Build_HasTerminatorBytes() {
		var builder = new SpcFileBuilder();
		var spc = builder.Build();

		// Separator bytes at $21, $22 are 0x26
		Assert.Equal(0x26, spc[0x21]);
		Assert.Equal(0x26, spc[0x22]);
	}

	#endregion

	#region Register Tests

	[Fact]
	public void SetPC_StoresCorrectValue() {
		var builder = new SpcFileBuilder();
		builder.SetPC(0x0400);
		var spc = builder.Build();

		// PC at offset $25-$26
		ushort pc = (ushort)(spc[0x25] | (spc[0x26] << 8));
		Assert.Equal(0x0400, pc);
	}

	[Fact]
	public void SetRegisters_StoresCorrectValues() {
		var builder = new SpcFileBuilder();
		builder.SetRegisters(pc: 0x0200, a: 0x12, x: 0x34, y: 0x56, psw: 0x80, sp: 0xef);
		var spc = builder.Build();

		// Registers at offset $27-$2b
		Assert.Equal(0x12, spc[0x27]);  // A
		Assert.Equal(0x34, spc[0x28]);  // X
		Assert.Equal(0x56, spc[0x29]);  // Y
		Assert.Equal(0x80, spc[0x2a]);  // PSW
		Assert.Equal(0xef, spc[0x2b]);  // SP
	}

	[Fact]
	public void GetRegisters_ReturnsStoredValues() {
		var builder = new SpcFileBuilder();
		builder.SetRegisters(pc: 0x0400, a: 0xab, x: 0xcd, y: 0xef, psw: 0x23, sp: 0x01);
		var spc = builder.Build();

		var regs = SpcFileBuilder.GetRegisters(spc);

		Assert.Equal(0x0400, regs.pc);
		Assert.Equal(0xab, regs.a);
		Assert.Equal(0xcd, regs.x);
		Assert.Equal(0xef, regs.y);
		Assert.Equal(0x01, regs.sp);
		Assert.Equal(0x23, regs.psw);
	}

	[Fact]
	public void SetIndividualRegisters_StoresCorrectValues() {
		var builder = new SpcFileBuilder();
		builder.SetA(0x12);
		builder.SetX(0x34);
		builder.SetY(0x56);
		builder.SetPSW(0x80);
		builder.SetSP(0xef);
		var spc = builder.Build();

		Assert.Equal(0x12, spc[0x27]);
		Assert.Equal(0x34, spc[0x28]);
		Assert.Equal(0x56, spc[0x29]);
		Assert.Equal(0x80, spc[0x2a]);
		Assert.Equal(0xef, spc[0x2b]);
	}

	#endregion

	#region ID666 Tag Tests

	[Fact]
	public void SetSongTitle_StoresTitle() {
		var builder = new SpcFileBuilder();
		builder.SetSongTitle("Test Song");
		var spc = builder.Build();

		// Song title at offset $2e, max 32 chars
		string title = System.Text.Encoding.ASCII.GetString(spc, 0x2e, 32).TrimEnd('\0');
		Assert.Equal("Test Song", title);
	}

	[Fact]
	public void SetGameTitle_StoresTitle() {
		var builder = new SpcFileBuilder();
		builder.SetGameTitle("Test Game");
		var spc = builder.Build();

		// Game title at offset $4e, max 32 chars
		string title = System.Text.Encoding.ASCII.GetString(spc, 0x4e, 32).TrimEnd('\0');
		Assert.Equal("Test Game", title);
	}

	[Fact]
	public void SetDumperName_StoresDumper() {
		var builder = new SpcFileBuilder();
		builder.SetDumperName("Test Dumper");
		var spc = builder.Build();

		// Dumper at offset $6e, max 16 chars
		string dumper = System.Text.Encoding.ASCII.GetString(spc, 0x6e, 16).TrimEnd('\0');
		Assert.Equal("Test Dumper", dumper);
	}

	[Fact]
	public void SetComments_StoresComments() {
		var builder = new SpcFileBuilder();
		builder.SetComments("Test Comment");
		var spc = builder.Build();

		// Comments at offset $7e, max 32 chars
		string comments = System.Text.Encoding.ASCII.GetString(spc, 0x7e, 32).TrimEnd('\0');
		Assert.Equal("Test Comment", comments);
	}

	[Fact]
	public void SetDumpDate_StoresDate() {
		var builder = new SpcFileBuilder();
		builder.SetDumpDate("01/15/2026");
		var spc = builder.Build();

		// Date at offset $9e, max 11 chars
		string date = System.Text.Encoding.ASCII.GetString(spc, 0x9e, 11).TrimEnd('\0');
		Assert.Equal("01/15/2026", date);
	}

	[Fact]
	public void SetSongTitle_TruncatesLongTitle() {
		var builder = new SpcFileBuilder();
		builder.SetSongTitle("This is a very long song title that exceeds the 32 character limit");
		var spc = builder.Build();

		string title = System.Text.Encoding.ASCII.GetString(spc, 0x2e, 32);
		Assert.Equal(32, title.Length);
	}

	[Fact]
	public void GetSongTitle_ReturnsStoredTitle() {
		var builder = new SpcFileBuilder();
		builder.SetSongTitle("My Song");
		var spc = builder.Build();

		string title = SpcFileBuilder.GetSongTitle(spc);
		Assert.Equal("My Song", title);
	}

	[Fact]
	public void GetGameTitle_ReturnsStoredTitle() {
		var builder = new SpcFileBuilder();
		builder.SetGameTitle("My Game");
		var spc = builder.Build();

		string title = SpcFileBuilder.GetGameTitle(spc);
		Assert.Equal("My Game", title);
	}

	#endregion

	#region RAM Tests

	[Fact]
	public void SetRam_StoresData() {
		var builder = new SpcFileBuilder();
		var ram = new byte[0x10000];
		ram[0x0400] = 0x12;
		ram[0x0401] = 0x34;
		builder.SetRam(ram);
		var spc = builder.Build();

		// RAM starts at offset $100
		Assert.Equal(0x12, spc[0x100 + 0x0400]);
		Assert.Equal(0x34, spc[0x100 + 0x0401]);
	}

	[Fact]
	public void GetRam_ReturnsStoredData() {
		var builder = new SpcFileBuilder();
		var ram = new byte[0x10000];
		ram[0x0500] = 0xab;
		ram[0x0501] = 0xcd;
		builder.SetRam(ram);
		var spc = builder.Build();

		var extractedRam = SpcFileBuilder.GetRam(spc);

		Assert.Equal(0x10000, extractedRam.Length);
		Assert.Equal(0xab, extractedRam[0x0500]);
		Assert.Equal(0xcd, extractedRam[0x0501]);
	}

	[Fact]
	public void SetRamByte_StoresSingleByte() {
		var builder = new SpcFileBuilder();
		builder.SetRamByte(0x1234, 0xef);
		var spc = builder.Build();

		Assert.Equal(0xef, spc[0x100 + 0x1234]);
	}

	[Fact]
	public void SetRamAt_StoresRegion() {
		var builder = new SpcFileBuilder();
		var data = new byte[] { 0x01, 0x02, 0x03, 0x04 };
		builder.SetRamAt(0x0400, data);
		var spc = builder.Build();

		Assert.Equal(0x01, spc[0x100 + 0x0400]);
		Assert.Equal(0x02, spc[0x100 + 0x0401]);
		Assert.Equal(0x03, spc[0x100 + 0x0402]);
		Assert.Equal(0x04, spc[0x100 + 0x0403]);
	}

	#endregion

	#region DSP Register Tests

	[Fact]
	public void SetDspRegisters_StoresData() {
		var builder = new SpcFileBuilder();
		var dsp = new byte[128];
		dsp[0x0c] = 0x7f;  // Master volume left
		dsp[0x1c] = 0x7f;  // Master volume right
		builder.SetDspRegisters(dsp);
		var spc = builder.Build();

		// DSP registers at offset $10100
		Assert.Equal(0x7f, spc[0x10100 + 0x0c]);
		Assert.Equal(0x7f, spc[0x10100 + 0x1c]);
	}

	[Fact]
	public void GetDspRegisters_ReturnsStoredData() {
		var builder = new SpcFileBuilder();
		var dsp = new byte[128];
		dsp[0x4c] = 0x00;  // KON
		dsp[0x5c] = 0xff;  // KOF
		builder.SetDspRegisters(dsp);
		var spc = builder.Build();

		var extractedDsp = SpcFileBuilder.GetDspRegisters(spc);

		Assert.Equal(128, extractedDsp.Length);
		Assert.Equal(0x00, extractedDsp[0x4c]);
		Assert.Equal(0xff, extractedDsp[0x5c]);
	}

	[Fact]
	public void SetDspRegister_StoresSingleRegister() {
		var builder = new SpcFileBuilder();
		builder.SetDspRegister(0x6c, 0x80);  // FLG - mute, reset
		var spc = builder.Build();

		Assert.Equal(0x80, spc[0x10100 + 0x6c]);
	}

	#endregion

	#region Validation Tests

	[Fact]
	public void ValidateFile_ValidFile_ReturnsTrue() {
		var builder = new SpcFileBuilder();
		var spc = builder.Build();

		Assert.True(SpcFileBuilder.ValidateFile(spc));
	}

	[Fact]
	public void ValidateFile_WrongSize_ReturnsFalse() {
		var shortFile = new byte[0x10000];
		Assert.False(SpcFileBuilder.ValidateFile(shortFile));
	}

	[Fact]
	public void ValidateFile_WrongSignature_ReturnsFalse() {
		var badFile = new byte[0x10200];
		Array.Fill(badFile, (byte)0xff);
		Assert.False(SpcFileBuilder.ValidateFile(badFile));
	}

	[Fact]
	public void ValidateFile_Null_ReturnsFalse() {
		Assert.False(SpcFileBuilder.ValidateFile(null!));
	}

	#endregion

	#region Minimal Creation Tests

	[Fact]
	public void CreateMinimal_ReturnsValidFile() {
		var spc = SpcFileBuilder.CreateMinimal(0x0200, []);

		Assert.Equal(65984, spc.Length);
		Assert.True(SpcFileBuilder.ValidateFile(spc));
	}

	[Fact]
	public void CreateMinimal_WithProgram_LoadsProgram() {
		byte[] program = [0x00, 0xef];  // NOP, SLEEP
		var spc = SpcFileBuilder.CreateMinimal(0x0400, program);

		var regs = SpcFileBuilder.GetRegisters(spc);
		Assert.Equal(0x0400, regs.pc);

		var ram = SpcFileBuilder.GetRam(spc);
		Assert.Equal(0x00, ram[0x0400]);
		Assert.Equal(0xef, ram[0x0401]);
	}

	#endregion

	#region Builder Chaining Tests

	[Fact]
	public void Builder_SupportsChaining() {
		var spc = new SpcFileBuilder()
			.SetPC(0x0400)
			.SetRegisters(pc: 0x0400, a: 0x00, x: 0x00, y: 0x00, psw: 0x00, sp: 0xef)
			.SetSongTitle("Chained")
			.SetGameTitle("Game")
			.SetComments("Test")
			.Build();

		Assert.Equal(65984, spc.Length);
		Assert.Equal("Chained", SpcFileBuilder.GetSongTitle(spc));
	}

	#endregion

	#region ID666 Tag Indicator Tests

	[Fact]
	public void Id666Tag_IndicatorIsSetByDefault() {
		var builder = new SpcFileBuilder();
		var spc = builder.Build();

		// Offset $23 contains ID666 tag indicator (0x1a = has tags)
		Assert.Equal(0x1a, spc[0x23]);
	}

	[Fact]
	public void SetIncludeId666Tag_False_ClearsIndicator() {
		var builder = new SpcFileBuilder();
		builder.SetIncludeId666Tag(false);
		var spc = builder.Build();

		// 0x1b = no ID666 tag
		Assert.Equal(0x1b, spc[0x23]);
	}

	#endregion

	#region Fade Out Tests

	[Fact]
	public void SetFadeOut_StoresValues() {
		var builder = new SpcFileBuilder();
		builder.SetFadeOut(120, 5000);  // 2 minutes, 5 second fade
		var spc = builder.Build();

		// Fade out at offset $a9, 3 ASCII chars for seconds
		string time = System.Text.Encoding.ASCII.GetString(spc, 0xa9, 3).TrimEnd('\0');
		Assert.Equal("120", time);

		// Fade length at offset $ac, stored as ASCII - implementation pads to 5 chars
		string fade = System.Text.Encoding.ASCII.GetString(spc, 0xac, 5).TrimEnd('\0');
		Assert.Equal("0500", fade);  // 5000 padded left with 0s, truncated to 4 visible + null
	}

	#endregion

	#region Artist Tests

	[Fact]
	public void SetArtistName_StoresArtist() {
		var builder = new SpcFileBuilder();
		builder.SetArtistName("Nobuo Uematsu");
		var spc = builder.Build();

		// Artist at offset $b0, max 32 chars
		string artist = System.Text.Encoding.ASCII.GetString(spc, 0xb0, 32).TrimEnd('\0');
		Assert.Equal("Nobuo Uematsu", artist);
	}

	#endregion

	#region Emulator Code Tests

	[Fact]
	public void SetEmulatorUsed_StoresCode() {
		var builder = new SpcFileBuilder();
		builder.SetEmulatorUsed(SpcFileBuilder.EmulatorCodes.ZSNES);
		var spc = builder.Build();

		// Emulator at offset $d1
		Assert.Equal(1, spc[0xd1]);
	}

	[Fact]
	public void EmulatorCodes_HasExpectedValues() {
		Assert.Equal(0, SpcFileBuilder.EmulatorCodes.Unknown);
		Assert.Equal(1, SpcFileBuilder.EmulatorCodes.ZSNES);
		Assert.Equal(2, SpcFileBuilder.EmulatorCodes.Snes9x);
	}

	#endregion
}
