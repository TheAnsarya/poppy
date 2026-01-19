namespace Poppy.Core.CodeGen;

using System.Text;

/// <summary>
/// Encodes text strings to binary data using TBL table files
/// </summary>
public class TextEncoder {
	private readonly Dictionary<string, byte> _charToByte = [];
	private readonly Dictionary<string, ushort> _charToWord = [];
	private readonly Dictionary<string, byte[]> _controlCodes = [];
	private readonly Dictionary<string, byte> _dteEntries = [];

	/// <summary>Table name/description</summary>
	public string Name { get; set; } = "Unnamed";

	/// <summary>End-of-string byte value</summary>
	public byte? EndByte { get; set; }

	/// <summary>
	/// Load encoder from TBL content
	/// </summary>
	public static TextEncoder LoadFromTbl(string content) {
		var encoder = new TextEncoder();
		var lines = content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

		foreach (var line in lines) {
			var trimmed = line.Trim();

			// Skip comments
			if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith(';') || trimmed.StartsWith('#')) {
				continue;
			}

			// Handle directives
			if (trimmed.StartsWith('@')) {
				ParseDirective(encoder, trimmed);
				continue;
			}

			// Parse hex=char format
			var eqIndex = trimmed.IndexOf('=');
			if (eqIndex < 2) continue;

			var hexPart = trimmed[..eqIndex];
			var charPart = trimmed[(eqIndex + 1)..];

			// Handle escape sequences and special tokens
			charPart = UnescapeString(charPart);

			if (hexPart.Length == 2) {
				// Single byte
				if (byte.TryParse(hexPart, System.Globalization.NumberStyles.HexNumber, null, out byte b)) {
					if (!encoder._charToByte.ContainsKey(charPart)) {
						encoder._charToByte[charPart] = b;
					}
				}
			} else if (hexPart.Length == 4) {
				// Word
				if (ushort.TryParse(hexPart, System.Globalization.NumberStyles.HexNumber, null, out ushort w)) {
					if (!encoder._charToWord.ContainsKey(charPart)) {
						encoder._charToWord[charPart] = w;
					}
				}
			}
		}

		return encoder;
	}

	/// <summary>
	/// Load encoder from file
	/// </summary>
	public static TextEncoder LoadFromFile(string path) {
		var content = File.ReadAllText(path);
		var encoder = LoadFromTbl(content);
		encoder.Name = Path.GetFileNameWithoutExtension(path);
		return encoder;
	}

	/// <summary>
	/// Create ASCII encoder (0x20-0x7E)
	/// </summary>
	public static TextEncoder CreateAsciiEncoder() {
		var encoder = new TextEncoder { Name = "ASCII" };
		for (int i = 0x20; i <= 0x7e; i++) {
			encoder._charToByte[((char)i).ToString()] = (byte)i;
		}
		return encoder;
	}

	/// <summary>
	/// Create Pokemon Red/Blue style encoder (Game Boy)
	/// </summary>
	public static TextEncoder CreatePokemonEncoder() {
		var encoder = new TextEncoder { Name = "Pokemon" };
		encoder.EndByte = 0x50;

		// Uppercase A-Z at 0x80-0x99
		for (int i = 0; i < 26; i++) {
			encoder._charToByte[((char)('A' + i)).ToString()] = (byte)(0x80 + i);
		}

		// Lowercase a-z at 0xa0-0xb9
		for (int i = 0; i < 26; i++) {
			encoder._charToByte[((char)('a' + i)).ToString()] = (byte)(0xa0 + i);
		}

		// Numbers 0-9 at 0xf6-0xff
		for (int i = 0; i < 10; i++) {
			encoder._charToByte[((char)('0' + i)).ToString()] = (byte)(0xf6 + i);
		}

		// Common punctuation
		encoder._charToByte[" "] = 0x7f;
		encoder._charToByte["!"] = 0xe7;
		encoder._charToByte["."] = 0xe8;
		encoder._charToByte[","] = 0xf4;
		encoder._charToByte["-"] = 0xf2;
		encoder._charToByte["'"] = 0xf3;

		// Control codes
		encoder._controlCodes["[LINE]"] = [0x4f];
		encoder._controlCodes["[END]"] = [0x50];
		encoder._controlCodes["[PARA]"] = [0x51];
		encoder._controlCodes["[CONT]"] = [0x55];
		encoder._controlCodes["[DONE]"] = [0x57];

		return encoder;
	}

	/// <summary>
	/// Create Dragon Quest/Warrior style encoder (NES/SNES)
	/// </summary>
	public static TextEncoder CreateDragonQuestEncoder() {
		var encoder = new TextEncoder { Name = "Dragon Quest" };
		encoder.EndByte = 0xff;

		// Uppercase A-Z at 0x80
		for (int i = 0; i < 26; i++) {
			encoder._charToByte[((char)('A' + i)).ToString()] = (byte)(0x80 + i);
		}

		// Lowercase a-z at 0x9a
		for (int i = 0; i < 26; i++) {
			encoder._charToByte[((char)('a' + i)).ToString()] = (byte)(0x9a + i);
		}

		// Numbers 0-9 at 0xb4
		for (int i = 0; i < 10; i++) {
			encoder._charToByte[((char)('0' + i)).ToString()] = (byte)(0xb4 + i);
		}

		// Common characters
		encoder._charToByte[" "] = 0xbe;
		encoder._charToByte["."] = 0xbf;
		encoder._charToByte[","] = 0xc0;
		encoder._charToByte["-"] = 0xc1;
		encoder._charToByte["!"] = 0xc2;
		encoder._charToByte["?"] = 0xc3;
		encoder._charToByte["'"] = 0xc4;
		encoder._charToByte["\""] = 0xc5;
		encoder._charToByte[":"] = 0xc6;

		// Control codes
		encoder._controlCodes["[NAME]"] = [0xf0];
		encoder._controlCodes["[ITEM]"] = [0xf1];
		encoder._controlCodes["[NUM]"] = [0xf2];
		encoder._controlCodes["[LINE]"] = [0xfc];
		encoder._controlCodes["[PARA]"] = [0xfd];
		encoder._controlCodes["[WAIT]"] = [0xfe];
		encoder._controlCodes["[END]"] = [0xff];

		return encoder;
	}

	/// <summary>
	/// Create Final Fantasy style encoder (NES/SNES)
	/// </summary>
	public static TextEncoder CreateFinalFantasyEncoder() {
		var encoder = new TextEncoder { Name = "Final Fantasy" };
		encoder.EndByte = 0x00;

		// Uppercase A-Z at 0x8a
		for (int i = 0; i < 26; i++) {
			encoder._charToByte[((char)('A' + i)).ToString()] = (byte)(0x8a + i);
		}

		// Lowercase a-z at 0xa4
		for (int i = 0; i < 26; i++) {
			encoder._charToByte[((char)('a' + i)).ToString()] = (byte)(0xa4 + i);
		}

		// Numbers 0-9 at 0x80
		for (int i = 0; i < 10; i++) {
			encoder._charToByte[((char)('0' + i)).ToString()] = (byte)(0x80 + i);
		}

		// Common punctuation
		encoder._charToByte[" "] = 0xff;
		encoder._charToByte["."] = 0xc0;
		encoder._charToByte[","] = 0xc1;
		encoder._charToByte["-"] = 0xc2;
		encoder._charToByte["'"] = 0xc3;
		encoder._charToByte["!"] = 0xc4;
		encoder._charToByte["?"] = 0xc5;
		encoder._charToByte[":"] = 0xbe;

		// Control codes
		encoder._controlCodes["[END]"] = [0x00];
		encoder._controlCodes["[LINE]"] = [0x01];
		encoder._controlCodes["[NAME]"] = [0x02];
		encoder._controlCodes["[ITEM]"] = [0x03];

		return encoder;
	}

	/// <summary>
	/// Create EarthBound/Mother style encoder (SNES)
	/// </summary>
	public static TextEncoder CreateEarthBoundEncoder() {
		var encoder = new TextEncoder { Name = "EarthBound" };
		encoder.EndByte = 0x02;

		// Uppercase A-Z at 0x30
		for (int i = 0; i < 26; i++) {
			encoder._charToByte[((char)('A' + i)).ToString()] = (byte)(0x30 + i);
		}

		// Lowercase a-z at 0x4a
		for (int i = 0; i < 26; i++) {
			encoder._charToByte[((char)('a' + i)).ToString()] = (byte)(0x4a + i);
		}

		// Numbers 0-9 at 0x60
		for (int i = 0; i < 10; i++) {
			encoder._charToByte[((char)('0' + i)).ToString()] = (byte)(0x60 + i);
		}

		// Space and punctuation
		encoder._charToByte[" "] = 0x00;
		encoder._charToByte["-"] = 0x1e;
		encoder._charToByte["'"] = 0x1f;
		encoder._charToByte["!"] = 0x6a;
		encoder._charToByte["?"] = 0x6b;
		encoder._charToByte["."] = 0x6c;
		encoder._charToByte[","] = 0x6d;

		// Control codes
		encoder._controlCodes["[END]"] = [0x02];
		encoder._controlCodes["[LINE]"] = [0x03];
		encoder._controlCodes["[PAGE]"] = [0x04];
		encoder._controlCodes["[NAME]"] = [0x1c];
		encoder._controlCodes["[ITEM]"] = [0x1d];

		return encoder;
	}

	/// <summary>
	/// Get encoder by template name
	/// </summary>
	public static TextEncoder GetTemplate(string name) {
		return name.ToLowerInvariant() switch {
			"ascii" => CreateAsciiEncoder(),
			"pokemon" or "pkmn" => CreatePokemonEncoder(),
			"dw" or "dq" or "dragonquest" or "dragonwarrior" => CreateDragonQuestEncoder(),
			"ff" or "finalfantasy" => CreateFinalFantasyEncoder(),
			"eb" or "earthbound" or "mother" => CreateEarthBoundEncoder(),
			_ => CreateAsciiEncoder()
		};
	}

	/// <summary>
	/// Load DTE (Dual Tile Encoding) dictionary from content.
	/// Format: XX=ab (byte XX encodes the string "ab")
	/// </summary>
	public void LoadDteDictionary(string content) {
		var lines = content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

		foreach (var line in lines) {
			var trimmed = line.Trim();

			// Skip comments
			if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith(';') || trimmed.StartsWith('#')) {
				continue;
			}

			// Parse XX=text format (DTE/MTE)
			var eqIndex = trimmed.IndexOf('=');
			if (eqIndex < 2) continue;

			var hexPart = trimmed[..eqIndex];
			var textPart = trimmed[(eqIndex + 1)..];

			if (hexPart.Length == 2 && byte.TryParse(hexPart, System.Globalization.NumberStyles.HexNumber, null, out byte b)) {
				// DTE: one byte encodes multiple characters
				if (textPart.Length >= 2 && !_charToByte.ContainsKey(textPart)) {
					_dteEntries[textPart] = b;
				}
			}
		}
	}

	/// <summary>
	/// Load DTE dictionary from file
	/// </summary>
	public void LoadDteDictionaryFromFile(string path) {
		var content = File.ReadAllText(path);
		LoadDteDictionary(content);
	}

	/// <summary>
	/// Get all DTE mappings
	/// </summary>
	public IReadOnlyDictionary<string, byte> DteMappings => _dteEntries;

	/// <summary>
	/// Add a control code
	/// </summary>
	public void AddControlCode(string name, byte[] bytes) {
		_controlCodes[$"[{name}]"] = bytes;
	}

	/// <summary>
	/// Encode a string to bytes
	/// </summary>
	public byte[] Encode(string text) {
		var result = new List<byte>();
		int pos = 0;

		while (pos < text.Length) {
			// Try control codes first (longest match)
			bool found = false;
			foreach (var code in _controlCodes.OrderByDescending(c => c.Key.Length)) {
				if (text.AsSpan(pos).StartsWith(code.Key)) {
					result.AddRange(code.Value);
					pos += code.Key.Length;
					found = true;
					break;
				}
			}
			if (found) continue;

			// Try DTE/MTE entries (longest match first for MTE)
			if (_dteEntries.Count > 0) {
				bool dteFound = false;
				foreach (var dte in _dteEntries.OrderByDescending(d => d.Key.Length)) {
					if (text.AsSpan(pos).StartsWith(dte.Key)) {
						result.Add(dte.Value);
						pos += dte.Key.Length;
						dteFound = true;
						break;
					}
				}
				if (dteFound) continue;
			}

			// Try word mappings (2-char strings first)
			if (_charToWord.Count > 0 && pos + 1 < text.Length) {
				var twoChar = text.Substring(pos, 2);
				if (_charToWord.TryGetValue(twoChar, out ushort word)) {
					result.Add((byte)(word & 0xff));
					result.Add((byte)(word >> 8));
					pos += 2;
					continue;
				}
			}

			// Handle hex escape [XX] or [$XX] BEFORE single char
			if (text[pos] == '[') {
				int closePos = text.IndexOf(']', pos);
				if (closePos > pos + 1) {
					var inner = text[(pos + 1)..closePos];
					if (inner.StartsWith('$')) inner = inner[1..];

					if (byte.TryParse(inner, System.Globalization.NumberStyles.HexNumber, null, out byte hexByte)) {
						result.Add(hexByte);
						pos = closePos + 1;
						continue;
					}
				}
			}

			// Try single char
			var oneChar = text[pos].ToString();
			if (_charToByte.TryGetValue(oneChar, out byte b)) {
				result.Add(b);
				pos++;
				continue;
			}

			// Unknown character - skip
			pos++;
		}

		// Add end byte if specified
		if (EndByte.HasValue) {
			result.Add(EndByte.Value);
		}

		return [.. result];
	}

	/// <summary>
	/// Encode multiple strings
	/// </summary>
	public List<byte[]> EncodeAll(IEnumerable<string> texts) {
		return texts.Select(Encode).ToList();
	}

	/// <summary>
	/// Generate assembly for encoded text
	/// </summary>
	public string EncodeToAsm(string text, string label, bool lowercaseHex = true) {
		var bytes = Encode(text);
		var sb = new StringBuilder();

		sb.AppendLine($"; \"{EscapeForComment(text)}\"");
		sb.Append($"{label}:");

		if (bytes.Length <= 8) {
			sb.Append("\n\t.byte ");
			sb.AppendLine(string.Join(", ", bytes.Select(b => FormatHex(b, lowercaseHex))));
		} else {
			sb.AppendLine();
			for (int i = 0; i < bytes.Length; i += 8) {
				sb.Append("\t.byte ");
				var chunk = bytes.Skip(i).Take(8);
				sb.AppendLine(string.Join(", ", chunk.Select(b => FormatHex(b, lowercaseHex))));
			}
		}

		return sb.ToString();
	}

	/// <summary>
	/// Generate assembly for multiple text strings with pointer table
	/// </summary>
	public string EncodeAllToAsm(IEnumerable<(string label, string text)> entries, string tableName, bool lowercaseHex = true) {
		var sb = new StringBuilder();

		sb.AppendLine("; " + new string('=', 76));
		sb.AppendLine($"; {tableName} Text Data");
		sb.AppendLine("; " + new string('=', 76));
		sb.AppendLine();

		var entryList = entries.ToList();

		// Pointer table
		sb.AppendLine($"{tableName}_ptrs:");
		foreach (var (label, _) in entryList) {
			sb.AppendLine($"\t.word {label}");
		}
		sb.AppendLine();

		// Count constant
		sb.AppendLine($"{tableName}_count = ${entryList.Count:x2}");
		sb.AppendLine();

		// Text data
		foreach (var (label, text) in entryList) {
			sb.AppendLine(EncodeToAsm(text, label, lowercaseHex));
		}

		sb.AppendLine($"{tableName}_end:");

		return sb.ToString();
	}

	/// <summary>
	/// Check if a character can be encoded
	/// </summary>
	public bool CanEncode(char c) {
		return _charToByte.ContainsKey(c.ToString());
	}

	/// <summary>
	/// Check if a string can be fully encoded
	/// </summary>
	public bool CanEncodeAll(string text) {
		int pos = 0;
		while (pos < text.Length) {
			// Check control codes
			bool found = false;
			foreach (var code in _controlCodes.Keys) {
				if (text.AsSpan(pos).StartsWith(code)) {
					pos += code.Length;
					found = true;
					break;
				}
			}
			if (found) continue;

			// Check hex escapes
			if (text[pos] == '[' && pos + 3 < text.Length) {
				int closePos = text.IndexOf(']', pos);
				if (closePos > pos) {
					pos = closePos + 1;
					continue;
				}
			}

			// Check single char
			if (!_charToByte.ContainsKey(text[pos].ToString())) {
				return false;
			}
			pos++;
		}
		return true;
	}

	/// <summary>
	/// Get all mapped characters
	/// </summary>
	public IReadOnlyDictionary<string, byte> CharMappings => _charToByte;

	/// <summary>
	/// Get all control codes
	/// </summary>
	public IReadOnlyDictionary<string, byte[]> ControlCodes => _controlCodes;

	private static void ParseDirective(TextEncoder encoder, string line) {
		var parts = line.Split('=', 2);
		if (parts.Length < 2) return;

		var directive = parts[0].ToLowerInvariant();
		var value = parts[1].Trim();

		switch (directive) {
			case "@name":
				encoder.Name = value;
				break;
			case "@end":
				if (byte.TryParse(value, System.Globalization.NumberStyles.HexNumber, null, out byte end)) {
					encoder.EndByte = end;
				}
				break;
		}
	}

	private static string UnescapeString(string s) {
		if (string.IsNullOrEmpty(s)) return s;

		return s
			.Replace("<space>", " ")
			.Replace("<SPACE>", " ")
			.Replace("\\n", "\n")
			.Replace("\\r", "\r")
			.Replace("\\t", "\t")
			.Replace("\\\\", "\\");
	}

	private static string FormatHex(byte b, bool lowercase) {
		return lowercase ? $"${b:x2}" : $"${b:X2}";
	}

	private static string EscapeForComment(string text) {
		return text
			.Replace("\n", "\\n")
			.Replace("\r", "\\r")
			.Replace("\t", "\\t");
	}
}

/// <summary>
/// Options for text encoding operations
/// </summary>
public record TextEncodingOptions {
	/// <summary>Add null terminator (0x00) at end</summary>
	public bool NullTerminate { get; init; } = true;

	/// <summary>Custom end byte (overrides null terminator)</summary>
	public byte? EndByte { get; init; }

	/// <summary>Pad to fixed length</summary>
	public int? FixedLength { get; init; }

	/// <summary>Padding byte value</summary>
	public byte PadByte { get; init; } = 0x00;

	/// <summary>Use lowercase hex in assembly output</summary>
	public bool LowercaseHex { get; init; } = true;
}
