namespace Poppy.Core.CodeGen;

using System.Text;
using System.Text.Json;

/// <summary>
/// Converts JSON data files to Poppy assembly source code
/// </summary>
public class JsonToAsmConverter {
	/// <summary>
	/// Options for JSON to ASM conversion
	/// </summary>
	public record ConversionOptions {
		/// <summary>Assembly label prefix</summary>
		public string LabelPrefix { get; init; } = "";

		/// <summary>Use lowercase hex values</summary>
		public bool LowercaseHex { get; init; } = true;

		/// <summary>Include comments from JSON</summary>
		public bool IncludeComments { get; init; } = true;

		/// <summary>Use word (16-bit) for values over 255</summary>
		public bool AutoWordSize { get; init; } = true;

		/// <summary>Endianness for multi-byte values</summary>
		public Endianness Endianness { get; init; } = Endianness.Little;

		/// <summary>Indentation string (tab or spaces)</summary>
		public string Indent { get; init; } = "\t";

		/// <summary>Generate table label for each record</summary>
		public bool GenerateRecordLabels { get; init; } = true;

		/// <summary>Generate count constant</summary>
		public bool GenerateCount { get; init; } = true;

		/// <summary>Field order (null = use JSON order)</summary>
		public string[]? FieldOrder { get; init; }
	}

	/// <summary>
	/// Endianness for multi-byte values
	/// </summary>
	public enum Endianness {
		/// <summary>Little-endian (low byte first)</summary>
		Little,
		/// <summary>Big-endian (high byte first)</summary>
		Big
	}

	/// <summary>
	/// Convert JSON array of objects to assembly data tables
	/// </summary>
	public static string ConvertJsonArray(string json, string tableName, ConversionOptions? options = null) {
		options ??= new ConversionOptions();
		var sb = new StringBuilder();

		using var doc = JsonDocument.Parse(json);
		var root = doc.RootElement;

		// Handle both array and object with records array
		JsonElement records;
		if (root.ValueKind == JsonValueKind.Array) {
			records = root;
		} else if (root.TryGetProperty("records", out var recs)) {
			records = recs;
		} else {
			throw new ArgumentException("JSON must be an array or object with 'records' property");
		}

		int count = records.GetArrayLength();

		// Header comment
		sb.AppendLine("; " + new string('=', 76));
		sb.AppendLine($"; {tableName} Data Table");
		sb.AppendLine("; " + new string('=', 76));
		sb.AppendLine($"; Auto-generated from JSON by Poppy JsonToAsmConverter");
		sb.AppendLine($"; Records: {count}");
		sb.AppendLine("; " + new string('=', 76));
		sb.AppendLine();

		// Count constant
		if (options.GenerateCount) {
			string countLabel = $"{options.LabelPrefix}{SanitizeLabel(tableName)}_COUNT";
			sb.AppendLine($"{countLabel} = ${count:x2}");
			sb.AppendLine();
		}

		// Table label
		string tableLabel = $"{options.LabelPrefix}{SanitizeLabel(tableName)}";
		sb.AppendLine($"{tableLabel}:");

		// Process each record
		int index = 0;
		foreach (var record in records.EnumerateArray()) {
			// Record label and comment
			if (options.GenerateRecordLabels || options.IncludeComments) {
				sb.AppendLine();
				string recordName = GetRecordName(record, index);
				if (options.IncludeComments) {
					string indexHex = options.LowercaseHex ? $"${index:x2}" : $"${index:X2}";
					sb.AppendLine($"; Record {indexHex}: {recordName}");
				}
				if (options.GenerateRecordLabels) {
					string label = $"{tableLabel}_{SanitizeLabel(recordName)}";
					sb.AppendLine($"{label}:");
				}
			}

			// Generate data directives for each field
			var fields = GetFieldOrder(record, options.FieldOrder);
			foreach (var fieldName in fields) {
				if (!record.TryGetProperty(fieldName, out var value)) continue;
				if (fieldName.Equals("id", StringComparison.OrdinalIgnoreCase)) continue;
				if (fieldName.Equals("name", StringComparison.OrdinalIgnoreCase)) continue;
				if (fieldName.Equals("index", StringComparison.OrdinalIgnoreCase)) continue;

				var line = GenerateFieldDirective(fieldName, value, options);
				sb.AppendLine(line);
			}

			index++;
		}

		sb.AppendLine();
		sb.AppendLine($"{tableLabel}_END:");

		return sb.ToString();
	}

	/// <summary>
	/// Convert a single JSON object to assembly equates/constants
	/// </summary>
	public static string ConvertJsonObject(string json, string prefix, ConversionOptions? options = null) {
		options ??= new ConversionOptions();
		var sb = new StringBuilder();

		using var doc = JsonDocument.Parse(json);
		var root = doc.RootElement;

		if (root.ValueKind != JsonValueKind.Object) {
			throw new ArgumentException("JSON must be an object");
		}

		sb.AppendLine("; " + new string('=', 76));
		sb.AppendLine($"; {prefix} Constants");
		sb.AppendLine("; " + new string('=', 76));
		sb.AppendLine();

		foreach (var prop in root.EnumerateObject()) {
			string label = $"{prefix}_{SanitizeLabel(prop.Name)}";
			string value = GetConstantValue(prop.Value, options);
			string comment = "";

			if (options.IncludeComments && prop.Value.ValueKind == JsonValueKind.Object) {
				if (prop.Value.TryGetProperty("description", out var desc)) {
					comment = $"\t\t; {desc.GetString()}";
				}
			}

			sb.AppendLine($"{label} = {value}{comment}");
		}

		return sb.ToString();
	}

	/// <summary>
	/// Generate assembly directive for a field value
	/// </summary>
	private static string GenerateFieldDirective(string fieldName, JsonElement value, ConversionOptions options) {
		var sb = new StringBuilder();
		sb.Append(options.Indent);

		switch (value.ValueKind) {
			case JsonValueKind.Number:
				if (value.TryGetInt32(out int intVal)) {
					if (options.AutoWordSize && intVal > 255) {
						sb.Append(".word ");
						sb.Append(FormatHex(intVal, 4, options.LowercaseHex));
					} else {
						sb.Append(".byte ");
						sb.Append(FormatHex(intVal & 0xff, 2, options.LowercaseHex));
					}
				} else {
					sb.Append(".byte $00");
				}
				break;

			case JsonValueKind.String:
				string? strVal = value.GetString();
				if (strVal != null && strVal.StartsWith('$')) {
					// Hex literal
					sb.Append(".byte ");
					sb.Append(strVal);
				} else {
					// Text string - would need table encoding
					sb.Append("; .text \"");
					sb.Append(strVal);
					sb.Append('"');
				}
				break;

			case JsonValueKind.Array:
				sb.Append(".byte ");
				var bytes = new List<string>();
				foreach (var elem in value.EnumerateArray()) {
					if (elem.TryGetInt32(out int b)) {
						bytes.Add(FormatHex(b & 0xff, 2, options.LowercaseHex));
					}
				}
				sb.Append(string.Join(", ", bytes));
				break;

			case JsonValueKind.True:
				sb.Append(".byte $01");
				break;

			case JsonValueKind.False:
			case JsonValueKind.Null:
				sb.Append(".byte $00");
				break;

			default:
				sb.Append(".byte $00");
				break;
		}

		// Add comment with field name
		if (options.IncludeComments) {
			sb.Append("\t\t; ");
			sb.Append(FormatFieldName(fieldName));
			if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int numVal)) {
				sb.Append($" = {numVal}");
			}
		}

		return sb.ToString();
	}

	private static string GetConstantValue(JsonElement value, ConversionOptions options) {
		return value.ValueKind switch {
			JsonValueKind.Number when value.TryGetInt32(out int i) => FormatHex(i, i > 255 ? 4 : 2, options.LowercaseHex),
			JsonValueKind.String => value.GetString() ?? "$00",
			JsonValueKind.True => "$01",
			JsonValueKind.False => "$00",
			JsonValueKind.Object when value.TryGetProperty("value", out var v) => GetConstantValue(v, options),
			_ => "$00"
		};
	}

	private static string GetRecordName(JsonElement record, int index) {
		if (record.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String) {
			return name.GetString() ?? $"Record{index}";
		}
		if (record.TryGetProperty("id", out var id)) {
			if (id.ValueKind == JsonValueKind.String) return id.GetString() ?? $"Record{index}";
			if (id.TryGetInt32(out int idNum)) return $"_{idNum:x2}";
		}
		return $"_{index:x2}";
	}

	private static string[] GetFieldOrder(JsonElement record, string[]? customOrder) {
		if (customOrder != null) return customOrder;

		var fields = new List<string>();
		foreach (var prop in record.EnumerateObject()) {
			fields.Add(prop.Name);
		}
		return [.. fields];
	}

	private static string FormatHex(int value, int digits, bool lowercase) {
		string format = lowercase ? $"x{digits}" : $"X{digits}";
		return $"${value.ToString(format)}";
	}

	private static string SanitizeLabel(string name) {
		var sb = new StringBuilder();
		bool lastWasUnderscore = false;

		foreach (char c in name) {
			if (char.IsLetterOrDigit(c)) {
				sb.Append(char.ToLowerInvariant(c));
				lastWasUnderscore = false;
			} else if (!lastWasUnderscore) {
				sb.Append('_');
				lastWasUnderscore = true;
			}
		}

		// Remove trailing underscore
		while (sb.Length > 0 && sb[^1] == '_') {
			sb.Length--;
		}

		return sb.ToString();
	}

	private static string FormatFieldName(string name) {
		// Convert snake_case or camelCase to Title Case
		var sb = new StringBuilder();
		bool nextUpper = true;

		foreach (char c in name) {
			if (c == '_') {
				sb.Append(' ');
				nextUpper = true;
			} else if (char.IsUpper(c) && sb.Length > 0 && !char.IsWhiteSpace(sb[^1])) {
				sb.Append(' ');
				sb.Append(c);
				nextUpper = false;
			} else {
				sb.Append(nextUpper ? char.ToUpperInvariant(c) : c);
				nextUpper = false;
			}
		}

		return sb.ToString();
	}
}
