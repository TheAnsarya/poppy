namespace Poppy.Tests.CodeGen;

using Poppy.Core.CodeGen;

public class JsonToAsmConverterTests {
	[Fact]
	public void ConvertJsonArray_SimpleArray_GeneratesTable() {
		var json = """
		[
			{ "name": "Slime", "hp": 8, "attack": 5 },
			{ "name": "Drakee", "hp": 12, "attack": 9 }
		]
		""";

		var result = JsonToAsmConverter.ConvertJsonArray(json, "Monsters");

		Assert.Contains("monsters_COUNT = $02", result);
		Assert.Contains("monsters:", result);
		Assert.Contains("monsters_slime:", result);
		Assert.Contains("monsters_drakee:", result);
		Assert.Contains(".byte $08", result); // hp
		Assert.Contains(".byte $05", result); // attack
		Assert.Contains("monsters_END:", result);
	}

	[Fact]
	public void ConvertJsonArray_WithRecordsProperty_GeneratesTable() {
		var json = """
		{
			"schema": "monsters",
			"records": [
				{ "name": "Slime", "hp": 8 }
			]
		}
		""";

		var result = JsonToAsmConverter.ConvertJsonArray(json, "Enemies");

		Assert.Contains("enemies_COUNT = $01", result);
		Assert.Contains("enemies:", result);
		Assert.Contains("enemies_slime:", result);
	}

	[Fact]
	public void ConvertJsonArray_WithHexStrings_PreservesHex() {
		var json = """
		[
			{ "name": "Test", "flags": "$a0" }
		]
		""";

		var result = JsonToAsmConverter.ConvertJsonArray(json, "Items");

		Assert.Contains(".byte $a0", result);
	}

	[Fact]
	public void ConvertJsonArray_WithByteArrays_GeneratesMultipleBytes() {
		var json = """
		[
			{ "name": "Spell", "pattern": [1, 2, 3, 4] }
		]
		""";

		var result = JsonToAsmConverter.ConvertJsonArray(json, "Spells");

		Assert.Contains(".byte $01, $02, $03, $04", result);
	}

	[Fact]
	public void ConvertJsonArray_WordValues_GeneratesWordDirective() {
		var json = """
		[
			{ "name": "Dragon", "exp": 1000 }
		]
		""";

		var options = new JsonToAsmConverter.ConversionOptions { AutoWordSize = true };
		var result = JsonToAsmConverter.ConvertJsonArray(json, "Bosses", options);

		Assert.Contains(".word $03e8", result); // 1000 = $03e8
	}

	[Fact]
	public void ConvertJsonArray_UppercaseHex_GeneratesUppercase() {
		var json = """
		[
			{ "name": "Item", "price": 255 }
		]
		""";

		var options = new JsonToAsmConverter.ConversionOptions { LowercaseHex = false };
		var result = JsonToAsmConverter.ConvertJsonArray(json, "Items", options);

		Assert.Contains("$FF", result);
	}

	[Fact]
	public void ConvertJsonArray_NoRecordLabels_OmitsLabels() {
		var json = """
		[
			{ "name": "Slime", "hp": 8 }
		]
		""";

		var options = new JsonToAsmConverter.ConversionOptions { GenerateRecordLabels = false };
		var result = JsonToAsmConverter.ConvertJsonArray(json, "Monsters", options);

		Assert.DoesNotContain("monsters_slime:", result);
	}

	[Fact]
	public void ConvertJsonArray_NoComments_OmitsComments() {
		var json = """
		[
			{ "name": "Slime", "hp": 8 }
		]
		""";

		var options = new JsonToAsmConverter.ConversionOptions { IncludeComments = false };
		var result = JsonToAsmConverter.ConvertJsonArray(json, "Monsters", options);

		// Header comments still exist, but record comments should not include the index pattern
		Assert.DoesNotContain("; Record $", result);
		Assert.DoesNotContain("; Hp", result);
	}

	[Fact]
	public void ConvertJsonArray_CustomPrefix_IncludesPrefix() {
		var json = """
		[
			{ "name": "Test", "value": 1 }
		]
		""";

		var options = new JsonToAsmConverter.ConversionOptions { LabelPrefix = "game_" };
		var result = JsonToAsmConverter.ConvertJsonArray(json, "Data", options);

		Assert.Contains("game_data_COUNT", result);
		Assert.Contains("game_data:", result);
	}

	[Fact]
	public void ConvertJsonArray_BooleanValues_GeneratesByteBooleans() {
		var json = """
		[
			{ "name": "Flag", "enabled": true, "disabled": false }
		]
		""";

		var result = JsonToAsmConverter.ConvertJsonArray(json, "Flags");

		Assert.Contains(".byte $01", result); // true
		Assert.Contains(".byte $00", result); // false
	}

	[Fact]
	public void ConvertJsonObject_Constants_GeneratesEquates() {
		var json = """
		{
			"max_hp": 255,
			"max_level": 50,
			"base_exp": 1000
		}
		""";

		var result = JsonToAsmConverter.ConvertJsonObject(json, "CONST");

		Assert.Contains("CONST_max_hp = $ff", result);
		Assert.Contains("CONST_max_level = $32", result);
		Assert.Contains("CONST_base_exp = $03e8", result);
	}

	[Fact]
	public void ConvertJsonArray_StringValues_GeneratesTextComment() {
		var json = """
		[
			{ "name": "Herb", "description": "Restores HP" }
		]
		""";

		var result = JsonToAsmConverter.ConvertJsonArray(json, "Items");

		// Text strings are commented out (need TBL encoding)
		Assert.Contains("; .text \"Restores HP\"", result);
	}

	[Fact]
	public void ConvertJsonArray_RecordsWithId_UsesIdInLabel() {
		var json = """
		[
			{ "id": 10, "value": 5 }
		]
		""";

		var result = JsonToAsmConverter.ConvertJsonArray(json, "Data");

		Assert.Contains("data__0a:", result); // id 10 = $0a
	}

	[Fact]
	public void ConvertJsonArray_SpecialCharactersInName_SanitizesLabel() {
		var json = """
		[
			{ "name": "Magic Herb (+5)", "hp": 10 }
		]
		""";

		var result = JsonToAsmConverter.ConvertJsonArray(json, "Items");

		Assert.Contains("items_magic_herb_5:", result);
	}

	[Fact]
	public void ConvertJsonArray_EmptyArray_GeneratesEmptyTable() {
		var json = "[]";

		var result = JsonToAsmConverter.ConvertJsonArray(json, "Empty");

		Assert.Contains("empty_COUNT = $00", result);
		Assert.Contains("empty:", result);
		Assert.Contains("empty_END:", result);
	}

	[Fact]
	public void ConvertJsonArray_CustomFieldOrder_RespectsOrder() {
		var json = """
		[
			{ "name": "Test", "fieldA": 1, "fieldB": 2, "fieldC": 3 }
		]
		""";

		var options = new JsonToAsmConverter.ConversionOptions {
			FieldOrder = ["fieldC", "fieldA", "fieldB"],
			IncludeComments = true
		};
		var result = JsonToAsmConverter.ConvertJsonArray(json, "Data", options);

		// Field C should appear before Field A in the output
		int posC = result.IndexOf("; Field C");
		int posA = result.IndexOf("; Field A");
		int posB = result.IndexOf("; Field B");

		Assert.True(posC > -1, "Field C comment not found");
		Assert.True(posA > -1, "Field A comment not found");
		Assert.True(posB > -1, "Field B comment not found");
		Assert.True(posC < posA, "Field C should come before Field A");
		Assert.True(posA < posB, "Field A should come before Field B");
	}

	[Fact]
	public void ConvertJsonArray_ExcludesIdNameIndex_NotInOutput() {
		var json = """
		[
			{ "id": 1, "name": "Test", "index": 0, "value": 5 }
		]
		""";

		var result = JsonToAsmConverter.ConvertJsonArray(json, "Data");

		// The directive lines shouldn't include id/name/index values
		var lines = result.Split('\n');
		var directives = lines.Where(l => l.Contains(".byte") || l.Contains(".word")).ToList();

		// Should only have one .byte for "value"
		Assert.Single(directives);
		Assert.Contains(".byte $05", directives[0]);
	}

	[Fact]
	public void ConvertJsonArray_GeneratesHeader_WithTableInfo() {
		var json = """
		[
			{ "name": "A", "v": 1 },
			{ "name": "B", "v": 2 },
			{ "name": "C", "v": 3 }
		]
		""";

		var result = JsonToAsmConverter.ConvertJsonArray(json, "MyTable");

		Assert.Contains("; MyTable Data Table", result);
		Assert.Contains("; Records: 3", result);
		Assert.Contains("JsonToAsmConverter", result);
	}
}
