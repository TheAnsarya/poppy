using Poppy.Core.Arch;
using Poppy.Core.CodeGen;
using Poppy.Core.Semantics;

using PoppyLexer = Poppy.Core.Lexer.Lexer;
using PoppyParser = Poppy.Core.Parser.Parser;

namespace Poppy.Arch.ARM7TDMI.Tests.CodeGen;

/// <summary>
/// End-to-end ARM instruction encoding tests that verify emitted machine bytes.
/// </summary>
public sealed class ArmInstructionEncodingIntegrationTests {
	private static (byte[] Code, CodeGenerator Generator, SemanticAnalyzer Analyzer) Compile(string source) {
		var lexer = new PoppyLexer(source, "arm-integration.pasm");
		var tokens = lexer.Tokenize();
		var parser = new PoppyParser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer();
		analyzer.Analyze(program);

		var generator = new CodeGenerator(analyzer, TargetArchitecture.ARM7TDMI);
		var code = generator.Generate(program);

		return (code, generator, analyzer);
	}

	[Fact]
	public void MovImmediate_EmitsExpectedArmWord() {
		var source = @"
.target gba
.org $08000000
mov r0, #42
";

		var (code, gen, analyzer) = Compile(source);

		Assert.False(analyzer.HasErrors, string.Join("; ", analyzer.Errors.Select(e => e.Message)));
		Assert.False(gen.HasErrors, string.Join("; ", gen.Errors.Select(e => e.Message)));
		Assert.Equal([0x2a, 0x00, 0xa0, 0xe3], code[..4]); // E3A0002A
	}

	[Fact]
	public void AddRegister_EmitsExpectedArmWord() {
		var source = @"
.target gba
.org $08000000
add r1, r2, r3
";

		var (code, gen, analyzer) = Compile(source);

		Assert.False(analyzer.HasErrors, string.Join("; ", analyzer.Errors.Select(e => e.Message)));
		Assert.False(gen.HasErrors, string.Join("; ", gen.Errors.Select(e => e.Message)));
		Assert.Equal([0x03, 0x10, 0x82, 0xe0], code[..4]); // E0821003
	}

	[Fact]
	public void CmpImmediate_EmitsExpectedArmWord() {
		var source = @"
.target gba
.org $08000000
cmp r0, #1
";

		var (code, gen, analyzer) = Compile(source);

		Assert.False(analyzer.HasErrors, string.Join("; ", analyzer.Errors.Select(e => e.Message)));
		Assert.False(gen.HasErrors, string.Join("; ", gen.Errors.Select(e => e.Message)));
		Assert.Equal([0x01, 0x00, 0x50, 0xe3], code[..4]); // E3500001
	}

	[Fact]
	public void BranchForwardLabel_EmitsExpectedOffsetAndCrossRef() {
		var source = @"
.target gba
.org $08000000
b target
nop
target:
nop
";

		var (code, gen, analyzer) = Compile(source);

		Assert.False(analyzer.HasErrors, string.Join("; ", analyzer.Errors.Select(e => e.Message)));
		Assert.False(gen.HasErrors, string.Join("; ", gen.Errors.Select(e => e.Message)));
		Assert.Equal([0x00, 0x00, 0x00, 0xea], code[..4]); // EA000000

		var branchRef = gen.CrossReferences.Single(r => r.Type == 3);
		Assert.Equal((uint)0x08000000, branchRef.From);
		Assert.Equal((uint)0x08000008, branchRef.To);
	}

	[Fact]
	public void BlAndBxAndSwi_EmitExpectedWords() {
		var source = @"
.target gba
.org $08000000
bl target
bx lr
target:
swi #$11
";

		var (code, gen, analyzer) = Compile(source);

		Assert.False(analyzer.HasErrors, string.Join("; ", analyzer.Errors.Select(e => e.Message)));
		Assert.False(gen.HasErrors, string.Join("; ", gen.Errors.Select(e => e.Message)));
		Assert.Equal([0x00, 0x00, 0x00, 0xeb], code[..4]);   // EB000000
		Assert.Equal([0x1e, 0xff, 0x2f, 0xe1], code[4..8]); // E12FFF1E
		Assert.Equal([0x11, 0x00, 0x00, 0xef], code[8..12]); // EF000011

		var callRef = gen.CrossReferences.Single(r => r.Type == 1);
		Assert.Equal((uint)0x08000000, callRef.From);
		Assert.Equal((uint)0x08000008, callRef.To);
	}

	[Fact]
	public void LoadStoreImmediateForms_EmitExpectedWords() {
		var source = @"
.target gba
.org $08000000
ldr r0, r1
str r2, r3, #12
ldrb r4, r5, #1
strb r6, r7
";

		var (code, gen, analyzer) = Compile(source);

		Assert.False(analyzer.HasErrors, string.Join("; ", analyzer.Errors.Select(e => e.Message)));
		Assert.False(gen.HasErrors, string.Join("; ", gen.Errors.Select(e => e.Message)));
		Assert.Equal([0x00, 0x00, 0x91, 0xe5], code[..4]);
		Assert.Equal([0x0c, 0x20, 0x83, 0xe5], code[4..8]);
		Assert.Equal([0x01, 0x40, 0xd5, 0xe5], code[8..12]);
		Assert.Equal([0x00, 0x60, 0xc7, 0xe5], code[12..16]);
	}

	[Fact]
	public void MultiplyForms_EmitExpectedWords() {
		var source = @"
.target gba
.org $08000000
mul r0, r1, r2
mla r3, r4, r5, r6
muls r8, r9, r10
";

		var (code, gen, analyzer) = Compile(source);

		Assert.False(analyzer.HasErrors, string.Join("; ", analyzer.Errors.Select(e => e.Message)));
		Assert.False(gen.HasErrors, string.Join("; ", gen.Errors.Select(e => e.Message)));
		Assert.Equal([0x91, 0x02, 0x00, 0xe0], code[..4]);
		Assert.Equal([0x94, 0x65, 0x23, 0xe0], code[4..8]);
		Assert.Equal([0x99, 0x0a, 0x18, 0xe0], code[8..12]);
	}

	[Fact]
	public void BracketedLoadStoreForms_EmitExpectedWords() {
		var source = @"
.target gba
.org $08000000
ldr r0, [r1]
str r2, [r3, #12]
ldrb r4, [r5, r6]
strb r7, [r8, r9]
";

		var (code, gen, analyzer) = Compile(source);

		Assert.False(analyzer.HasErrors, string.Join("; ", analyzer.Errors.Select(e => e.Message)));
		Assert.False(gen.HasErrors, string.Join("; ", gen.Errors.Select(e => e.Message)));
		Assert.Equal([0x00, 0x00, 0x91, 0xe5], code[..4]);
		Assert.Equal([0x0c, 0x20, 0x83, 0xe5], code[4..8]);
		Assert.Equal([0x06, 0x40, 0xd5, 0xe7], code[8..12]);
		Assert.Equal([0x09, 0x70, 0xc8, 0xe7], code[12..16]);
	}

	[Fact]
	public void NegativeImmediateLoadStoreForms_EmitExpectedWords() {
		var source = @"
.target gba
.org $08000000
ldr r0, [r1, #-4]
str r2, [r3, #-12]
ldrb r4, [r5, #-1]
strb r6, [r7, #-2]
";

		var (code, gen, analyzer) = Compile(source);

		Assert.False(analyzer.HasErrors, string.Join("; ", analyzer.Errors.Select(e => e.Message)));
		Assert.False(gen.HasErrors, string.Join("; ", gen.Errors.Select(e => e.Message)));
		Assert.Equal([0x04, 0x00, 0x11, 0xe5], code[0..4]);
		Assert.Equal([0x0c, 0x20, 0x03, 0xe5], code[4..8]);
		Assert.Equal([0x01, 0x40, 0x55, 0xe5], code[8..12]);
		Assert.Equal([0x02, 0x60, 0x47, 0xe5], code[12..16]);
	}

	[Fact]
	public void WriteBackAndPostIndexLoadStoreForms_EmitExpectedWords() {
		var source = @"
.target gba
.org $08000000
ldr r0, [r1, #4]!
str r2, [r3, #12]!
ldrb r4, [r5], #1
strb r6, [r7], #2
ldr r8, [r9], r10
str r11, [r12], r13
";

		var (code, gen, analyzer) = Compile(source);

		Assert.False(analyzer.HasErrors, string.Join("; ", analyzer.Errors.Select(e => e.Message)));
		Assert.False(gen.HasErrors, string.Join("; ", gen.Errors.Select(e => e.Message)));
		Assert.Equal([0x04, 0x00, 0xb1, 0xe5], code[0..4]);
		Assert.Equal([0x0c, 0x20, 0xa3, 0xe5], code[4..8]);
		Assert.Equal([0x01, 0x40, 0xd5, 0xe4], code[8..12]);
		Assert.Equal([0x02, 0x60, 0xc7, 0xe4], code[12..16]);
		Assert.Equal([0x0a, 0x80, 0x99, 0xe6], code[16..20]);
		Assert.Equal([0x0d, 0xb0, 0x8c, 0xe6], code[20..24]);
	}

	[Fact]
	public void ShiftedRegisterOffsetLoadStoreForms_EmitExpectedWords() {
		var source = @"
.target gba
.org $08000000
ldr r0, [r1, r2, lsl #2]
strb r3, [r4], r5, lsl #1
";

		var (code, gen, analyzer) = Compile(source);

		Assert.False(analyzer.HasErrors, string.Join("; ", analyzer.Errors.Select(e => e.Message)));
		Assert.False(gen.HasErrors, string.Join("; ", gen.Errors.Select(e => e.Message)));
		Assert.Equal([0x02, 0x01, 0x91, 0xe7], code[0..4]);
		Assert.Equal([0x85, 0x30, 0xc4, 0xe6], code[4..8]);
	}

	[Fact]
	public void ConditionalMultiplyMnemonic_EmitsExpectedWord() {
		var source = @"
.target gba
.org $08000000
mulseq r0, r1, r2
";

		var (code, gen, analyzer) = Compile(source);

		Assert.False(analyzer.HasErrors, string.Join("; ", analyzer.Errors.Select(e => e.Message)));
		Assert.False(gen.HasErrors, string.Join("; ", gen.Errors.Select(e => e.Message)));
		Assert.Equal([0x91, 0x02, 0x10, 0x00], code[..4]);
	}

	[Fact]
	public void ConditionalVariantsAcrossFamilies_EmitExpectedWords() {
		var source = @"
.target gba
.org $08000000
moveq r0, #42
addne r1, r2, r3
cmplt r0, #1
ldreq r0, [r1]
strne r2, [r3, #12]
beq target
blne target
bxne lr
swige #$11
target:
nop
";

		var (code, gen, analyzer) = Compile(source);

		Assert.False(analyzer.HasErrors, string.Join("; ", analyzer.Errors.Select(e => e.Message)));
		Assert.False(gen.HasErrors, string.Join("; ", gen.Errors.Select(e => e.Message)));
		Assert.Equal([0x2a, 0x00, 0xa0, 0x03], code[0..4]);
		Assert.Equal([0x03, 0x10, 0x82, 0x10], code[4..8]);
		Assert.Equal([0x01, 0x00, 0x50, 0xb3], code[8..12]);
		Assert.Equal([0x00, 0x00, 0x91, 0x05], code[12..16]);
		Assert.Equal([0x0c, 0x20, 0x83, 0x15], code[16..20]);
		Assert.Equal([0x02, 0x00, 0x00, 0x0a], code[20..24]);
		Assert.Equal([0x01, 0x00, 0x00, 0x1b], code[24..28]);
		Assert.Equal([0x1e, 0xff, 0x2f, 0x11], code[28..32]);
		Assert.Equal([0x11, 0x00, 0x00, 0xaf], code[32..36]);
	}

	[Fact]
	public void LongMultiplyForms_EmitExpectedWords() {
		var source = @"
.target gba
.org $08000000
umull r0, r1, r2, r3
smull r0, r1, r2, r3
umlal r0, r1, r2, r3
smlal r0, r1, r2, r3
";

		var (code, gen, analyzer) = Compile(source);

		Assert.False(analyzer.HasErrors, string.Join("; ", analyzer.Errors.Select(e => e.Message)));
		Assert.False(gen.HasErrors, string.Join("; ", gen.Errors.Select(e => e.Message)));
		Assert.Equal([0x92, 0x03, 0xc1, 0xe0], code[0..4]);
		Assert.Equal([0x92, 0x03, 0x81, 0xe0], code[4..8]);
		Assert.Equal([0x92, 0x03, 0xe1, 0xe0], code[8..12]);
		Assert.Equal([0x92, 0x03, 0xa1, 0xe0], code[12..16]);
	}

	[Fact]
	public void LongMultiplySetFlagsForms_EmitExpectedWords() {
		var source = @"
.target gba
.org $08000000
umulls r0, r1, r2, r3
smulls r0, r1, r2, r3
umlals r0, r1, r2, r3
smlals r0, r1, r2, r3
umullseq r0, r1, r2, r3
smlalsne r0, r1, r2, r3
";

		var (code, gen, analyzer) = Compile(source);

		Assert.False(analyzer.HasErrors, string.Join("; ", analyzer.Errors.Select(e => e.Message)));
		Assert.False(gen.HasErrors, string.Join("; ", gen.Errors.Select(e => e.Message)));
		Assert.Equal([0x92, 0x03, 0xd1, 0xe0], code[0..4]);
		Assert.Equal([0x92, 0x03, 0x91, 0xe0], code[4..8]);
		Assert.Equal([0x92, 0x03, 0xf1, 0xe0], code[8..12]);
		Assert.Equal([0x92, 0x03, 0xb1, 0xe0], code[12..16]);
		Assert.Equal([0x92, 0x03, 0xd1, 0x00], code[16..20]);
		Assert.Equal([0x92, 0x03, 0xb1, 0x10], code[20..24]);
	}

}
