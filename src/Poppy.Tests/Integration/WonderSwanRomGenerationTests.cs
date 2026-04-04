// ============================================================================
// WonderSwanRomGenerationTests.cs - WonderSwan ROM Generation Integration Tests
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================
// Verifies the full pipeline: lexer → parser → semantic analyzer → code generator
// produces correct V30MZ binary output with proper WonderSwan ROM layout.
// ============================================================================

using Poppy.Core.CodeGen;
using Poppy.Core.Semantics;
using Xunit;

namespace Poppy.Tests.Integration;

/// <summary>
/// Integration tests for WonderSwan ROM generation.
/// Verifies the full pipeline from source to binary ROM.
/// </summary>
public sealed class WonderSwanRomGenerationTests {
	[Fact]
	public void Generate_MinimalWsRom_CreatesCorrectBinary() {
		// Minimal WonderSwan ROM: implied instructions only
		var source = @"
.target wonderswan

.org $0000
start:
	cli
	cld
	nop
	hlt
";
		var (binary, analyzer, generator) = CompileWonderSwan(source);

		Assert.False(analyzer.HasErrors, GetErrorsString(analyzer));
		Assert.False(generator.HasErrors, GetErrorsString(generator));

		// Minimum WonderSwan ROM is 128KB
		Assert.Equal(128 * 1024, binary.Length);

		// Code at start
		Assert.Equal(0xfa, binary[0]); // CLI
		Assert.Equal(0xfc, binary[1]); // CLD
		Assert.Equal(0x90, binary[2]); // NOP
		Assert.Equal(0xf4, binary[3]); // HLT
	}

	[Fact]
	public void Generate_WsRom_HasValidHeader() {
		var source = @"
.target wonderswan

.org $0000
	nop
";
		var (binary, analyzer, generator) = CompileWonderSwan(source);

		Assert.False(analyzer.HasErrors, GetErrorsString(analyzer));
		Assert.False(generator.HasErrors, GetErrorsString(generator));

		// Header is last 10 bytes
		var headerOffset = binary.Length - 10;

		// Publisher ID default is 0x01
		Assert.Equal(0x01, binary[headerOffset]);

		// Verify checksum (last 2 bytes)
		ushort storedChecksum = (ushort)(binary[binary.Length - 2] | (binary[binary.Length - 1] << 8));
		ushort calculatedChecksum = 0;
		for (int i = 0; i < binary.Length - 2; i++) {
			calculatedChecksum += binary[i];
		}
		Assert.Equal(calculatedChecksum, storedChecksum);
	}

	[Fact]
	public void Generate_WsRom_PushPopRegisters() {
		var source = @"
.target wonderswan

.org $0000
	push ax
	push bx
	push cx
	push ds
	pop ds
	pop cx
	pop bx
	pop ax
";
		var (binary, analyzer, generator) = CompileWonderSwan(source);

		Assert.False(analyzer.HasErrors, GetErrorsString(analyzer));
		Assert.False(generator.HasErrors, GetErrorsString(generator));

		Assert.Equal(0x50, binary[0]); // PUSH AX
		Assert.Equal(0x53, binary[1]); // PUSH BX
		Assert.Equal(0x51, binary[2]); // PUSH CX
		Assert.Equal(0x1e, binary[3]); // PUSH DS
		Assert.Equal(0x1f, binary[4]); // POP DS
		Assert.Equal(0x59, binary[5]); // POP CX
		Assert.Equal(0x5b, binary[6]); // POP BX
		Assert.Equal(0x58, binary[7]); // POP AX
	}

	[Fact]
	public void Generate_WsRom_ControlFlow() {
		// Test INT, near CALL, near JMP, RET
		var source = @"
.target wonderswan

.org $0000
start:
	int #$10
	call subroutine
	jmp done

subroutine:
	push bp
	inc cx
	dec dx
	pop bp
	ret

done:
	hlt
";
		var (binary, analyzer, generator) = CompileWonderSwan(source);

		Assert.False(analyzer.HasErrors, GetErrorsString(analyzer));
		Assert.False(generator.HasErrors, GetErrorsString(generator));

		// INT $10 at offset 0
		Assert.Equal(0xcd, binary[0]); // INT
		Assert.Equal(0x10, binary[1]); // $10

		// CALL subroutine at offset 2 (3 bytes: $e8 + rel16)
		Assert.Equal(0xe8, binary[2]); // CALL near

		// JMP done at offset 5 (3 bytes: $e9 + rel16)
		Assert.Equal(0xe9, binary[5]); // JMP near

		// subroutine at offset 8
		Assert.Equal(0x55, binary[8]);  // PUSH BP
		Assert.Equal(0x41, binary[9]);  // INC CX
		Assert.Equal(0x4a, binary[10]); // DEC DX
		Assert.Equal(0x5d, binary[11]); // POP BP
		Assert.Equal(0xc3, binary[12]); // RET

		// done at offset 13
		Assert.Equal(0xf4, binary[13]); // HLT
	}

	[Fact]
	public void Generate_WsRom_IncDecRegisters() {
		var source = @"
.target wonderswan

.org $0000
	inc ax
	inc bx
	inc si
	inc di
	dec ax
	dec bx
	dec si
	dec di
";
		var (binary, analyzer, generator) = CompileWonderSwan(source);

		Assert.False(analyzer.HasErrors, GetErrorsString(analyzer));
		Assert.False(generator.HasErrors, GetErrorsString(generator));

		Assert.Equal(0x40, binary[0]); // INC AX
		Assert.Equal(0x43, binary[1]); // INC BX
		Assert.Equal(0x46, binary[2]); // INC SI
		Assert.Equal(0x47, binary[3]); // INC DI
		Assert.Equal(0x48, binary[4]); // DEC AX
		Assert.Equal(0x4b, binary[5]); // DEC BX
		Assert.Equal(0x4e, binary[6]); // DEC SI
		Assert.Equal(0x4f, binary[7]); // DEC DI
	}

	[Fact]
	public void Generate_WsRom_StringOpsWithRep() {
		var source = @"
.target wonderswan

.org $0000
	cld
	rep
	movsb
	rep
	stosw
";
		var (binary, analyzer, generator) = CompileWonderSwan(source);

		Assert.False(analyzer.HasErrors, GetErrorsString(analyzer));
		Assert.False(generator.HasErrors, GetErrorsString(generator));

		Assert.Equal(0xfc, binary[0]); // CLD
		Assert.Equal(0xf3, binary[1]); // REP
		Assert.Equal(0xa4, binary[2]); // MOVSB
		Assert.Equal(0xf3, binary[3]); // REP
		Assert.Equal(0xab, binary[4]); // STOSW
	}

	[Fact]
	public void Generate_WsRom_ConditionalJumps() {
		// Test that conditional jumps encode correctly with short relative offsets
		var source = @"
.target wonderswan

.org $0000
	jz skip
	nop
skip:
	nop
";
		var (binary, analyzer, generator) = CompileWonderSwan(source);

		Assert.False(analyzer.HasErrors, GetErrorsString(analyzer));
		Assert.False(generator.HasErrors, GetErrorsString(generator));

		Assert.Equal(0x74, binary[0]); // JZ
		Assert.Equal(0x01, binary[1]); // skip over 1 byte (NOP)
		Assert.Equal(0x90, binary[2]); // NOP (skipped)
		Assert.Equal(0x90, binary[3]); // NOP (target)
	}

	[Fact]
	public void Generate_WsRom_PushImmediate() {
		var source = @"
.target wonderswan

.org $0000
	push #$42
	push #$1234
";
		var (binary, analyzer, generator) = CompileWonderSwan(source);

		Assert.False(analyzer.HasErrors, GetErrorsString(analyzer));
		Assert.False(generator.HasErrors, GetErrorsString(generator));

		Assert.Equal(0x6a, binary[0]); // PUSH imm8
		Assert.Equal(0x42, binary[1]); // $42
		Assert.Equal(0x68, binary[2]); // PUSH imm16
		Assert.Equal(0x34, binary[3]); // low byte
		Assert.Equal(0x12, binary[4]); // high byte
	}

	[Fact]
	public void Generate_WsRom_RetWithImmediate() {
		var source = @"
.target wonderswan

.org $0000
	ret #$04
	retf #$08
";
		var (binary, analyzer, generator) = CompileWonderSwan(source);

		Assert.False(analyzer.HasErrors, GetErrorsString(analyzer));
		Assert.False(generator.HasErrors, GetErrorsString(generator));

		Assert.Equal(0xc2, binary[0]); // RET imm16
		Assert.Equal(0x04, binary[1]); // $0004 low
		Assert.Equal(0x00, binary[2]); // $0004 high
		Assert.Equal(0xca, binary[3]); // RETF imm16
		Assert.Equal(0x08, binary[4]); // $0008 low
		Assert.Equal(0x00, binary[5]); // $0008 high
	}

	#region Helpers

	private static (byte[] Binary, SemanticAnalyzer Analyzer, CodeGenerator Generator)
		CompileWonderSwan(string source) {
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer(TargetArchitecture.V30MZ);
		analyzer.Analyze(program);

		var generator = new CodeGenerator(analyzer, TargetArchitecture.V30MZ);
		var binary = generator.Generate(program);

		return (binary, analyzer, generator);
	}

	private static string GetErrorsString(SemanticAnalyzer analyzer) {
		return string.Join("\n", analyzer.Errors.Select(e => e.Message));
	}

	private static string GetErrorsString(CodeGenerator generator) {
		return string.Join("\n", generator.Errors.Select(e => e.Message));
	}

	#endregion
}
