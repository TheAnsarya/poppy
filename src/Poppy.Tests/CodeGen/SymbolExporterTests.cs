// Poppy Compiler - Symbol Exporter Unit Tests
// Copyright © 2026

using Poppy.Core.CodeGen;
using Poppy.Core.Lexer;
using Poppy.Core.Semantics;

namespace Poppy.Tests.CodeGen;

/// <summary>
/// Tests for the SymbolExporter class.
/// </summary>
public sealed class SymbolExporterTests {
	[Fact]
	public void Export_FceuxFormat_GeneratesCorrectFormat() {
		var symbolTable = new SymbolTable();
		symbolTable.Define("reset", SymbolType.Label, 0x8000, new SourceLocation("test.asm", 1, 1, 0));
		symbolTable.Define("nmi", SymbolType.Label, 0x8010, new SourceLocation("test.asm", 2, 1, 0));
		symbolTable.Define("PPUCTRL", SymbolType.Constant, 0x2000, new SourceLocation("test.asm", 3, 1, 0));

		var exporter = new SymbolExporter(symbolTable, TargetArchitecture.MOS6502);
		var tempFile = Path.GetTempFileName() + ".nl";
		try {
			exporter.Export(tempFile);
			var content = File.ReadAllText(tempFile);

			Assert.Contains("$2000#PPUCTRL#", content);
			Assert.Contains("$8000#reset#", content);
			Assert.Contains("$8010#nmi#", content);
			Assert.Contains("# FCEUX Symbol File", content);
		}
		finally {
			if (File.Exists(tempFile)) File.Delete(tempFile);
		}
	}

	[Fact]
	public void Export_MesenFormat_GeneratesCorrectFormat() {
		var symbolTable = new SymbolTable();
		symbolTable.Define("reset", SymbolType.Label, 0x8000, new SourceLocation("test.asm", 1, 1, 0));
		symbolTable.Define("temp", SymbolType.Label, 0x0000, new SourceLocation("test.asm", 2, 1, 0));

		var exporter = new SymbolExporter(symbolTable, TargetArchitecture.MOS6502);
		var tempFile = Path.GetTempFileName() + ".mlb";
		try {
			exporter.Export(tempFile);
			var content = File.ReadAllText(tempFile);

			Assert.Contains("RAM:$0000:temp", content);
			Assert.Contains("PRG:$8000:reset", content);
		}
		finally {
			if (File.Exists(tempFile)) File.Delete(tempFile);
		}
	}

	[Fact]
	public void Export_GenericFormat_GeneratesCorrectFormat() {
		var symbolTable = new SymbolTable();
		symbolTable.Define("reset", SymbolType.Label, 0x8000, new SourceLocation("test.asm", 1, 1, 0));
		symbolTable.Define("nmi", SymbolType.Label, 0x8010, new SourceLocation("test.asm", 2, 1, 0));

		var exporter = new SymbolExporter(symbolTable, TargetArchitecture.MOS6502);
		var tempFile = Path.GetTempFileName() + ".sym";
		try {
			exporter.Export(tempFile);
			var content = File.ReadAllText(tempFile);

			Assert.Contains("00:8000 reset", content);
			Assert.Contains("00:8010 nmi", content);
			Assert.Contains("; Symbol File", content);
		}
		finally {
			if (File.Exists(tempFile)) File.Delete(tempFile);
		}
	}

	[Fact]
	public void Export_SkipsMacros() {
		var symbolTable = new SymbolTable();
		symbolTable.Define("reset", SymbolType.Label, 0x8000, new SourceLocation("test.asm", 1, 1, 0));
		symbolTable.Define("test_macro", SymbolType.Macro, null, new SourceLocation("test.asm", 2, 1, 0));

		var exporter = new SymbolExporter(symbolTable, TargetArchitecture.MOS6502);
		var tempFile = Path.GetTempFileName() + ".nl";
		try {
			exporter.Export(tempFile);
			var content = File.ReadAllText(tempFile);

			Assert.Contains("reset", content);
			Assert.DoesNotContain("test_macro", content);
		}
		finally {
			if (File.Exists(tempFile)) File.Delete(tempFile);
		}
	}

	[Fact]
	public void Export_OrdersByAddress() {
		var symbolTable = new SymbolTable();
		symbolTable.Define("label_c", SymbolType.Label, 0x8020, new SourceLocation("test.asm", 1, 1, 0));
		symbolTable.Define("label_a", SymbolType.Label, 0x8000, new SourceLocation("test.asm", 2, 1, 0));
		symbolTable.Define("label_b", SymbolType.Label, 0x8010, new SourceLocation("test.asm", 3, 1, 0));

		var exporter = new SymbolExporter(symbolTable, TargetArchitecture.MOS6502);
		var tempFile = Path.GetTempFileName() + ".nl";
		try {
			exporter.Export(tempFile);
			var content = File.ReadAllText(tempFile);
			var lines = content.Split('\n').Where(l => l.StartsWith('$')).ToArray();

			Assert.Equal(3, lines.Length);
			Assert.Contains("label_a", lines[0]);
			Assert.Contains("label_b", lines[1]);
			Assert.Contains("label_c", lines[2]);
		}
		finally {
			if (File.Exists(tempFile)) File.Delete(tempFile);
		}
	}
}
