// ============================================================================
// Atari2600CodeGeneratorTests.cs - Atari 2600 Code Generator Tests
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Poppy.Core.CodeGen;
using Poppy.Core.Semantics;
using Xunit;

using PoppyLexer = Poppy.Core.Lexer.Lexer;
using PoppyParser = Poppy.Core.Parser.Parser;

namespace Poppy.Tests.CodeGen;

/// <summary>
/// Unit tests for Atari 2600 (6507) code generation.
/// </summary>
public class Atari2600CodeGeneratorTests {
	/// <summary>
	/// Helper to generate Atari 2600 code from source.
	/// </summary>
	private static (byte[] Code, CodeGenerator Generator) GenerateAtari2600Code(string source) {
		var lexer = new PoppyLexer(source);
		var tokens = lexer.Tokenize();
		var parser = new PoppyParser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer();
		analyzer.Analyze(program);

		var generator = new CodeGenerator(analyzer, TargetArchitecture.MOS6507);
		var code = generator.Generate(program);

		return (code, generator);
	}

	// ========================================================================
	// Basic Instruction Tests (6507 = 6502)
	// ========================================================================

	[Fact]
	public void Generate_Atari2600_LdaImmediate() {
		var source = ".target atari2600\nlda #$42";
		var (code, gen) = GenerateAtari2600Code(source);

		Assert.False(gen.HasErrors);
		// Should be 4K ROM with LDA at start
		Assert.Equal(4096, code.Length);
		Assert.Equal(0xa9, code[0]); // LDA immediate
		Assert.Equal(0x42, code[1]); // Value
	}

	[Fact]
	public void Generate_Atari2600_SimpleProgram() {
		var source = @"
.target atari2600
.org $1000
start:
	lda #$00
	sta $80
	rts
";
		var (code, gen) = GenerateAtari2600Code(source);

		Assert.False(gen.HasErrors);
		Assert.Equal(4096, code.Length);
		
		// Verify instructions in ROM
		Assert.Equal(0xa9, code[0]); // LDA #$00
		Assert.Equal(0x00, code[1]);
		Assert.Equal(0x85, code[2]); // STA $80
		Assert.Equal(0x80, code[3]);
		Assert.Equal(0x60, code[4]); // RTS
	}

	[Fact]
	public void Generate_Atari2600_ResetVector() {
		var source = @"
.target atari2600
.org $1000
start:
	nop
.org $1ffc
.word start
";
		var (code, gen) = GenerateAtari2600Code(source);

		Assert.False(gen.HasErrors);
		Assert.Equal(4096, code.Length);
		
		// Verify reset vector at end of ROM (offset 4092-4093)
		Assert.Equal(0x00, code[4092]); // Low byte of $1000
		Assert.Equal(0x10, code[4093]); // High byte of $1000
	}

	[Fact]
	public void Generate_Atari2600_AllInstructions() {
		var source = @"
.target atari2600
.org $1000
	lda #$42
	ldx #$43
	ldy #$44
	sta $80
	stx $81
	sty $82
	inc $80
	dec $81
	inx
	dex
	iny
	dey
	asl $80
	lsr $81
	rol $82
	ror $83
";
		var (code, gen) = GenerateAtari2600Code(source);

		Assert.False(gen.HasErrors);
		// Just verify it compiles without errors
		Assert.True(code.Length > 0);
	}

	// ========================================================================
	// ROM Building Tests
	// ========================================================================

	[Fact]
	public void RomBuilder_Creates4KRom() {
		var builder = new Atari2600RomBuilder(4096, Atari2600RomBuilder.BankSwitchingMethod.None);
		var rom = builder.Build();

		Assert.Equal(4096, rom.Length);
		// Should be filled with $ff
		Assert.Equal(0xff, rom[0]);
		Assert.Equal(0xff, rom[100]);
	}

	[Fact]
	public void RomBuilder_Creates2KRom() {
		var builder = new Atari2600RomBuilder(2048, Atari2600RomBuilder.BankSwitchingMethod.None);
		var rom = builder.Build();

		Assert.Equal(2048, rom.Length);
	}

	[Fact]
	public void RomBuilder_ValidatesBankSwitching() {
		// F8 requires 8K
		Assert.Throws<ArgumentException>(() => 
			new Atari2600RomBuilder(4096, Atari2600RomBuilder.BankSwitchingMethod.F8));
		
		// F6 requires 16K
		Assert.Throws<ArgumentException>(() => 
			new Atari2600RomBuilder(4096, Atari2600RomBuilder.BankSwitchingMethod.F6));
	}

	[Fact]
	public void RomBuilder_AddsSegment() {
		var builder = new Atari2600RomBuilder(4096, Atari2600RomBuilder.BankSwitchingMethod.None);
		var data = new byte[] { 0xa9, 0x42, 0x60 }; // LDA #$42, RTS
		
		builder.AddSegment(0x1000, data);
		var rom = builder.Build();

		Assert.Equal(0xa9, rom[0]);
		Assert.Equal(0x42, rom[1]);
		Assert.Equal(0x60, rom[2]);
	}

	// ========================================================================
	// Instruction Set Tests
	// ========================================================================

	[Fact]
	public void InstructionSet6507_SupportsAll6502Instructions() {
		// Test that we can get encoding for common 6502 instructions
		Assert.True(InstructionSet6507.TryGetEncoding("lda", Poppy.Core.Parser.AddressingMode.Immediate, out var enc1));
		Assert.Equal(0xa9, enc1.Opcode);

		Assert.True(InstructionSet6507.TryGetEncoding("sta", Poppy.Core.Parser.AddressingMode.ZeroPage, out var enc2));
		Assert.Equal(0x85, enc2.Opcode);

		Assert.True(InstructionSet6507.TryGetEncoding("jsr", Poppy.Core.Parser.AddressingMode.Absolute, out var enc3));
		Assert.Equal(0x20, enc3.Opcode);
	}

	[Fact]
	public void InstructionSet6507_ReturnsAllMnemonics() {
		var mnemonics = InstructionSet6507.GetAllMnemonics().ToList();
		
		Assert.Contains("lda", mnemonics, StringComparer.OrdinalIgnoreCase);
		Assert.Contains("sta", mnemonics, StringComparer.OrdinalIgnoreCase);
		Assert.Contains("jmp", mnemonics, StringComparer.OrdinalIgnoreCase);
		Assert.Contains("brk", mnemonics, StringComparer.OrdinalIgnoreCase);
	}
}
