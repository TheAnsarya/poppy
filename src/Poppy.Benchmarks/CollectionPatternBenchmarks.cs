using System.Collections.Frozen;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

namespace Poppy.Benchmarks;

/// <summary>
/// Benchmarks comparing FrozenSet vs HashSet and FrozenDictionary vs Dictionary
/// for the patterns used in MacroTable, ManifestValidator, and InstructionSet
/// classes (#179, #180).
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class CollectionPatternBenchmarks {
	[Params(32)]
	public int WorkMultiplier { get; set; }

	// --- FrozenSet vs HashSet (ReservedWords pattern) ---
	private HashSet<string> _mutableReserved = null!;
	private FrozenSet<string> _frozenReserved = null!;
	private string[] _lookupWords = null!;

	// --- FrozenDictionary vs Dictionary (InstructionSet pattern) ---
	private Dictionary<(string, int), byte> _mutableOpcodes = null!;
	private FrozenDictionary<(string, int), byte> _frozenOpcodes = null!;
	private (string, int)[] _lookupKeys = null!;

	// --- ToArray vs ToList ---
	private (string Name, int Value)[] _sourceData = null!;

	[GlobalSetup]
	public void Setup() {
		// ReservedWords: ~60 entries, case-insensitive
		string[] words = [
			"adc", "and", "asl", "bcc", "bcs", "beq", "bit", "bmi", "bne", "bpl",
			"brk", "bvc", "bvs", "clc", "cld", "cli", "clv", "cmp", "cpx", "cpy",
			"dec", "dex", "dey", "eor", "inc", "inx", "iny", "jmp", "jsr", "lda",
			"ldx", "ldy", "lsr", "nop", "ora", "pha", "php", "pla", "plp", "rol",
			"ror", "rti", "rts", "sbc", "sec", "sed", "sei", "sta", "stx", "sty",
			"tax", "tay", "tsx", "txa", "txs", "tya", "org", "byte", "word", "enum"
		];
		_mutableReserved = new HashSet<string>(words, StringComparer.OrdinalIgnoreCase);
		_frozenReserved = _mutableReserved.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

		// Lookup: 80% hits (mixed case), 20% misses
		var rng = new Random(42);
		_lookupWords = new string[10000];
		for (int i = 0; i < 10000; i++) {
			if (rng.Next(5) < 4) {
				string w = words[rng.Next(words.Length)];
				_lookupWords[i] = rng.Next(2) == 0 ? w.ToUpperInvariant() : w;
			} else {
				_lookupWords[i] = $"myMacro_{i}";
			}
		}

		// InstructionSet: ~150 opcode entries with tuple keys
		var opcodes = new Dictionary<(string, int), byte>();
		string[] mnemonics = ["lda", "sta", "ldx", "stx", "adc", "sbc", "and", "ora", "eor", "cmp"];
		for (int m = 0; m < mnemonics.Length; m++) {
			for (int mode = 0; mode < 15; mode++) {
				opcodes[(mnemonics[m], mode)] = (byte)(m * 15 + mode);
			}
		}
		_mutableOpcodes = opcodes;
		_frozenOpcodes = opcodes.ToFrozenDictionary();

		// Lookup keys: 70% hits, 30% misses
		_lookupKeys = new (string, int)[10000];
		for (int i = 0; i < 10000; i++) {
			if (rng.Next(10) < 7) {
				_lookupKeys[i] = (mnemonics[rng.Next(mnemonics.Length)], rng.Next(15));
			} else {
				_lookupKeys[i] = ($"xxx_{i}", rng.Next(15));
			}
		}

		// ToArray vs ToList source data
		_sourceData = new (string, int)[2000];
		for (int i = 0; i < 2000; i++) {
			_sourceData[i] = ($"sym_{i}", i);
		}
	}

	// ===== FrozenSet vs HashSet (case-insensitive) =====

	[Benchmark(Baseline = true)]
	[BenchmarkCategory("ReservedWords")]
	public int HashSet_Contains_CaseInsensitive() {
		int found = 0;
		var set = _mutableReserved;
		for (int i = 0; i < WorkMultiplier * 14; i++) {
			foreach (string word in _lookupWords) {
				if (set.Contains(word)) {
					found++;
				}
			}
		}
		return found;
	}

	[Benchmark]
	[BenchmarkCategory("ReservedWords")]
	public int FrozenSet_Contains_CaseInsensitive() {
		int found = 0;
		var set = _frozenReserved;
		for (int i = 0; i < WorkMultiplier * 11; i++) {
			foreach (string word in _lookupWords) {
				if (set.Contains(word)) {
					found++;
				}
			}
		}
		return found;
	}

	// ===== FrozenDictionary vs Dictionary (tuple keys) =====

	[Benchmark]
	[BenchmarkCategory("InstructionLookup")]
	public int Dictionary_TryGetValue_TupleKey() {
		int found = 0;
		var dict = _mutableOpcodes;
		for (int i = 0; i < WorkMultiplier * 4; i++) {
			foreach (var key in _lookupKeys) {
				if (dict.TryGetValue(key, out _)) {
					found++;
				}
			}
		}
		return found;
	}

	[Benchmark]
	[BenchmarkCategory("InstructionLookup")]
	public int FrozenDictionary_TryGetValue_TupleKey() {
		int found = 0;
		var dict = _frozenOpcodes;
		for (int i = 0; i < WorkMultiplier * 5; i++) {
			foreach (var key in _lookupKeys) {
				if (dict.TryGetValue(key, out _)) {
					found++;
				}
			}
		}
		return found;
	}

	// ===== ToArray vs ToList =====

	[Benchmark]
	[BenchmarkCategory("Materialization")]
	public List<(string, int)> OrderBy_ToList() {
		List<(string, int)> result = [];
		for (int i = 0; i < WorkMultiplier * 2; i++) {
			result = _sourceData.OrderBy(e => e.Name).ToList();
		}
		return result;
	}

	[Benchmark]
	[BenchmarkCategory("Materialization")]
	public (string, int)[] OrderBy_ToArray() {
		(string, int)[] result = [];
		for (int i = 0; i < WorkMultiplier * 2; i++) {
			result = _sourceData.OrderBy(e => e.Name).ToArray();
		}
		return result;
	}
}
