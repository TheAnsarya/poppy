using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Poppy.Core.Arch;
using Poppy.Core.CodeGen;
using Poppy.Core.Lexer;
using Poppy.Core.Parser;
using Poppy.Core.Semantics;

namespace Poppy.Benchmarks;

/// <summary>
/// Micro-benchmarks for ARM7TDMI special-emission compile paths.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class ArmSpecialEmissionBenchmarks {
	private string _dataProcessingSource = null!;
	private string _branchCallSource = null!;
	private string _loadStoreSource = null!;
	private string _multiplySource = null!;

	[GlobalSetup]
	public void Setup() {
		Poppy.Arch.MOS6502.Registration.RegisterAll();
		Poppy.Arch.WDC65816.Registration.RegisterAll();
		Poppy.Arch.SM83.Registration.RegisterAll();
		Poppy.Arch.M68000.Registration.RegisterAll();
		Poppy.Arch.Z80.Registration.RegisterAll();
		Poppy.Arch.V30MZ.Registration.RegisterAll();
		Poppy.Arch.ARM7TDMI.Registration.RegisterAll();
		Poppy.Arch.SPC700.Registration.RegisterAll();
		Poppy.Arch.HuC6280.Registration.RegisterAll();

		_dataProcessingSource = """
			.target gba
			.org $08000000
			mov r0, #1
			add r1, r0, r2
			sub r3, r3, #4
			cmp r1, r2
			""";

		_branchCallSource = """
			.target gba
			.org $08000000
			bl init
			b loop
			init:
			nop
			bx lr
			loop:
			swi #$11
			""";

		_loadStoreSource = """
			.target gba
			.org $08000000
			ldr r0, [r1]
			str r2, [r3, #12]
			ldrb r4, [r5, r6]
			strb r7, [r8, r9]
			""";

		_multiplySource = """
			.target gba
			.org $08000000
			mul r0, r1, r2
			mla r3, r4, r5, r6
			muls r8, r9, r10
			""";
	}

	[Benchmark(Description = "ARM special: data-processing")]
	public byte[] Compile_DataProcessing() => CompileArmSnippet(_dataProcessingSource);

	[Benchmark(Description = "ARM special: branch/call")]
	public byte[] Compile_BranchCall() => CompileArmSnippet(_branchCallSource);

	[Benchmark(Description = "ARM special: load/store")]
	public byte[] Compile_LoadStore() => CompileArmSnippet(_loadStoreSource);

	[Benchmark(Description = "ARM special: multiply")]
	public byte[] Compile_Multiply() => CompileArmSnippet(_multiplySource);

	private static byte[] CompileArmSnippet(string source) {
		var tokens = new Lexer(source, "arm-bench.pasm").Tokenize();
		var program = new Parser(tokens).Parse();
		var analyzer = new SemanticAnalyzer(TargetArchitecture.ARM7TDMI);
		analyzer.Analyze(program);
		return new CodeGenerator(analyzer, TargetArchitecture.ARM7TDMI).Generate(program);
	}
}
