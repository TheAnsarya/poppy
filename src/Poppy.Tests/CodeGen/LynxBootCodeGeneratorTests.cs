// ============================================================================
// LynxBootCodeGeneratorTests.cs - Unit Tests for Atari Lynx Boot Code Generation
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Poppy.Core.CodeGen;
using Xunit;

namespace Poppy.Tests.CodeGen;

/// <summary>
/// Tests for the Atari Lynx boot code generator.
/// </summary>
public sealed class LynxBootCodeGeneratorTests {
	#region Constants

	[Fact]
	public void LoadAddress_Is0x0200() {
		Assert.Equal(0x0200, LynxBootCodeGenerator.LoadAddress);
	}

	[Fact]
	public void MinimalBootCodeSize_Is8() {
		Assert.Equal(8, LynxBootCodeGenerator.MinimalBootCodeSize);
	}

	[Fact]
	public void StandardBootCodeSize_IsPositive() {
		Assert.True(LynxBootCodeGenerator.StandardBootCodeSize > 0);
	}

	[Fact]
	public void StandardBootCodeSize_IsGreaterThanMinimal() {
		Assert.True(LynxBootCodeGenerator.StandardBootCodeSize > LynxBootCodeGenerator.MinimalBootCodeSize);
	}

	#endregion

	#region GenerateBootCode - Standard Sequence

	[Fact]
	public void GenerateBootCode_StartsWithSei() {
		var code = LynxBootCodeGenerator.GenerateBootCode(0x0300);
		Assert.Equal(0x78, code[0]); // sei
	}

	[Fact]
	public void GenerateBootCode_SecondByteCld() {
		var code = LynxBootCodeGenerator.GenerateBootCode(0x0300);
		Assert.Equal(0xd8, code[1]); // cld
	}

	[Fact]
	public void GenerateBootCode_InitializesStackPointer() {
		var code = LynxBootCodeGenerator.GenerateBootCode(0x0300);
		Assert.Equal(0xa2, code[2]); // ldx #$ff
		Assert.Equal(0xff, code[3]);
		Assert.Equal(0x9a, code[4]); // txs
	}

	[Fact]
	public void GenerateBootCode_InitializesIoDir() {
		var code = LynxBootCodeGenerator.GenerateBootCode(0x0300);

		// lda #$00; sta $fd8a; sta $fd8b
		Assert.Equal(0xa9, code[5]); // lda
		Assert.Equal(0x00, code[6]); // #$00

		Assert.Equal(0x8d, code[7]);  // sta abs
		Assert.Equal(0x8a, code[8]);  // low byte of $fd8a
		Assert.Equal(0xfd, code[9]);  // high byte of $fd8a

		Assert.Equal(0x8d, code[10]); // sta abs
		Assert.Equal(0x8b, code[11]); // low byte of $fd8b
		Assert.Equal(0xfd, code[12]); // high byte of $fd8b
	}

	[Fact]
	public void GenerateBootCode_EnablesDisplay() {
		var code = LynxBootCodeGenerator.GenerateBootCode(0x0300);

		// lda #$08; sta $fd92
		Assert.Equal(0xa9, code[13]); // lda
		Assert.Equal(0x08, code[14]); // #$08

		Assert.Equal(0x8d, code[15]); // sta abs
		Assert.Equal(0x92, code[16]); // low byte of $fd92
		Assert.Equal(0xfd, code[17]); // high byte of $fd92
	}

	[Fact]
	public void GenerateBootCode_InitializesSpriteSystem() {
		var code = LynxBootCodeGenerator.GenerateBootCode(0x0300);

		// lda #$01; sta $fc92; sta $fc94
		Assert.Equal(0xa9, code[18]); // lda
		Assert.Equal(0x01, code[19]); // #$01

		Assert.Equal(0x8d, code[20]); // sta abs (SPRINIT)
		Assert.Equal(0x92, code[21]); // low byte of $fc92
		Assert.Equal(0xfc, code[22]); // high byte of $fc92

		Assert.Equal(0x8d, code[23]); // sta abs (SPRSYS)
		Assert.Equal(0x94, code[24]); // low byte of $fc94
		Assert.Equal(0xfc, code[25]); // high byte of $fc94
	}

	[Fact]
	public void GenerateBootCode_ClearsMapCtl() {
		var code = LynxBootCodeGenerator.GenerateBootCode(0x0300);

		// stz $fff9 (65SC02 STZ absolute)
		Assert.Equal(0x9c, code[26]); // stz abs
		Assert.Equal(0xf9, code[27]); // low byte of $fff9
		Assert.Equal(0xff, code[28]); // high byte of $fff9
	}

	[Fact]
	public void GenerateBootCode_EndsWithJmpToEntryPoint() {
		var code = LynxBootCodeGenerator.GenerateBootCode(0x0300);

		// jmp $0300
		var jmpIndex = code.Length - 3;
		Assert.Equal(0x4c, code[jmpIndex]);     // jmp
		Assert.Equal(0x00, code[jmpIndex + 1]); // low byte of $0300
		Assert.Equal(0x03, code[jmpIndex + 2]); // high byte of $0300
	}

	[Fact]
	public void GenerateBootCode_CustomEntryPoint_EncodesCorrectly() {
		var code = LynxBootCodeGenerator.GenerateBootCode(0x1234);

		var jmpIndex = code.Length - 3;
		Assert.Equal(0x4c, code[jmpIndex]);
		Assert.Equal(0x34, code[jmpIndex + 1]); // low byte
		Assert.Equal(0x12, code[jmpIndex + 2]); // high byte
	}

	[Fact]
	public void GenerateBootCode_SizeMatchesStandardBootCodeSize() {
		var code = LynxBootCodeGenerator.GenerateBootCode(0x0300);
		Assert.Equal(LynxBootCodeGenerator.StandardBootCodeSize, code.Length);
	}

	#endregion

	#region GenerateBootCode - Entry Point at LoadAddress

	[Fact]
	public void GenerateBootCode_EntryAtLoadAddress_AdjustsTarget() {
		// When entry point == LoadAddress ($0200), it adjusts to LoadAddress + StandardBootCodeSize
		var code = LynxBootCodeGenerator.GenerateBootCode(0x0200);
		var expectedEntry = 0x0200 + LynxBootCodeGenerator.StandardBootCodeSize;

		var jmpIndex = code.Length - 3;
		Assert.Equal(0x4c, code[jmpIndex]);
		Assert.Equal((byte)(expectedEntry & 0xff), code[jmpIndex + 1]);
		Assert.Equal((byte)((expectedEntry >> 8) & 0xff), code[jmpIndex + 2]);
	}

	[Fact]
	public void GenerateBootCode_DefaultParam_AdjustsEntryPoint() {
		// Default parameter is 0x0200 which triggers adjustment
		var code = LynxBootCodeGenerator.GenerateBootCode();
		var expectedEntry = 0x0200 + LynxBootCodeGenerator.StandardBootCodeSize;

		var jmpIndex = code.Length - 3;
		Assert.Equal((byte)(expectedEntry & 0xff), code[jmpIndex + 1]);
		Assert.Equal((byte)((expectedEntry >> 8) & 0xff), code[jmpIndex + 2]);
	}

	#endregion

	#region GenerateMinimalBootCode

	[Fact]
	public void GenerateMinimalBootCode_HasCorrectSize() {
		var code = LynxBootCodeGenerator.GenerateMinimalBootCode(0x0300);
		Assert.Equal(8, code.Length);
		Assert.Equal(LynxBootCodeGenerator.MinimalBootCodeSize, code.Length);
	}

	[Fact]
	public void GenerateMinimalBootCode_StartsWithSeiCld() {
		var code = LynxBootCodeGenerator.GenerateMinimalBootCode(0x0300);
		Assert.Equal(0x78, code[0]); // sei
		Assert.Equal(0xd8, code[1]); // cld
	}

	[Fact]
	public void GenerateMinimalBootCode_InitializesStack() {
		var code = LynxBootCodeGenerator.GenerateMinimalBootCode(0x0300);
		Assert.Equal(0xa2, code[2]); // ldx
		Assert.Equal(0xff, code[3]); // #$ff
		Assert.Equal(0x9a, code[4]); // txs
	}

	[Fact]
	public void GenerateMinimalBootCode_JumpsToEntryPoint() {
		var code = LynxBootCodeGenerator.GenerateMinimalBootCode(0x0300);
		Assert.Equal(0x4c, code[5]); // jmp
		Assert.Equal(0x00, code[6]); // low byte of $0300
		Assert.Equal(0x03, code[7]); // high byte of $0300
	}

	[Fact]
	public void GenerateMinimalBootCode_CustomEntryPoint() {
		var code = LynxBootCodeGenerator.GenerateMinimalBootCode(0xabcd);
		Assert.Equal(0xcd, code[6]); // low byte
		Assert.Equal(0xab, code[7]); // high byte
	}

	[Fact]
	public void GenerateMinimalBootCode_NoHardwareInit() {
		var code = LynxBootCodeGenerator.GenerateMinimalBootCode(0x0300);

		// Should NOT contain any STA absolute instructions (0x8d) — only sei, cld, ldx, txs, jmp
		for (int i = 0; i < code.Length; i++) {
			Assert.NotEqual(0x8d, code[i]);
		}
	}

	#endregion

	#region GenerateBootCodeSource

	[Fact]
	public void GenerateBootCodeSource_ContainsLynxBootLabel() {
		var source = LynxBootCodeGenerator.GenerateBootCodeSource("main");
		Assert.Contains("lynx_boot:", source);
	}

	[Fact]
	public void GenerateBootCodeSource_ContainsSei() {
		var source = LynxBootCodeGenerator.GenerateBootCodeSource("main");
		Assert.Contains("sei", source);
	}

	[Fact]
	public void GenerateBootCodeSource_ContainsCld() {
		var source = LynxBootCodeGenerator.GenerateBootCodeSource("main");
		Assert.Contains("cld", source);
	}

	[Fact]
	public void GenerateBootCodeSource_ContainsStackInit() {
		var source = LynxBootCodeGenerator.GenerateBootCodeSource("main");
		Assert.Contains("ldx #$ff", source);
		Assert.Contains("txs", source);
	}

	[Fact]
	public void GenerateBootCodeSource_ContainsIoInit() {
		var source = LynxBootCodeGenerator.GenerateBootCodeSource("main");
		Assert.Contains("$fd8a", source); // IODIR
		Assert.Contains("$fd8b", source); // IODAT
	}

	[Fact]
	public void GenerateBootCodeSource_ContainsDisplayInit() {
		var source = LynxBootCodeGenerator.GenerateBootCodeSource("main");
		Assert.Contains("$fd92", source); // DISPCTL
	}

	[Fact]
	public void GenerateBootCodeSource_ContainsSpriteInit() {
		var source = LynxBootCodeGenerator.GenerateBootCodeSource("main");
		Assert.Contains("$fc92", source); // SPRINIT
		Assert.Contains("$fc94", source); // SPRSYS
	}

	[Fact]
	public void GenerateBootCodeSource_ContainsMapCtlClear() {
		var source = LynxBootCodeGenerator.GenerateBootCodeSource("main");
		Assert.Contains("stz $fff9", source); // MAPCTL
	}

	[Fact]
	public void GenerateBootCodeSource_ContainsJmpToEntryPoint() {
		var source = LynxBootCodeGenerator.GenerateBootCodeSource("main");
		Assert.Contains("jmp main", source);
	}

	[Fact]
	public void GenerateBootCodeSource_CustomEntryPoint() {
		var source = LynxBootCodeGenerator.GenerateBootCodeSource("my_entry_point");
		Assert.Contains("jmp my_entry_point", source);
	}

	[Fact]
	public void GenerateBootCodeSource_ContainsComments() {
		var source = LynxBootCodeGenerator.GenerateBootCodeSource("main");

		// Should have helpful comments
		Assert.Contains("Disable interrupts", source);
		Assert.Contains("Clear decimal mode", source);
		Assert.Contains("Initialize stack pointer", source);
	}

	[Fact]
	public void GenerateBootCodeSource_ContainsHeader() {
		var source = LynxBootCodeGenerator.GenerateBootCodeSource("main");
		Assert.Contains("Lynx Standard Boot Code", source);
		Assert.Contains("Generated by Poppy", source);
	}

	#endregion

	#region Standard vs Minimal Code Comparison

	[Fact]
	public void StandardCode_IsLongerThanMinimal() {
		var standard = LynxBootCodeGenerator.GenerateBootCode(0x0300);
		var minimal = LynxBootCodeGenerator.GenerateMinimalBootCode(0x0300);

		Assert.True(standard.Length > minimal.Length);
	}

	[Fact]
	public void StandardAndMinimal_BothStartWithSeiCld() {
		var standard = LynxBootCodeGenerator.GenerateBootCode(0x0300);
		var minimal = LynxBootCodeGenerator.GenerateMinimalBootCode(0x0300);

		// Both start with sei; cld; ldx #$ff; txs
		Assert.Equal(standard[0], minimal[0]); // sei
		Assert.Equal(standard[1], minimal[1]); // cld
		Assert.Equal(standard[2], minimal[2]); // ldx
		Assert.Equal(standard[3], minimal[3]); // #$ff
		Assert.Equal(standard[4], minimal[4]); // txs
	}

	[Fact]
	public void StandardAndMinimal_BothEndWithJmp() {
		var standard = LynxBootCodeGenerator.GenerateBootCode(0x0300);
		var minimal = LynxBootCodeGenerator.GenerateMinimalBootCode(0x0300);

		// Both end with jmp $0300
		Assert.Equal(0x4c, standard[^3]);
		Assert.Equal(0x4c, minimal[^3]);
		Assert.Equal(standard[^2], minimal[^2]); // same entry point low
		Assert.Equal(standard[^1], minimal[^1]); // same entry point high
	}

	#endregion
}
