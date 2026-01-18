namespace Poppy.Tests.CodeGen;

using Poppy.Core.CodeGen;
using System.Text;
using Xunit;

public class TextEncoderTests {
	[Fact]
	public void LoadFromTbl_ParsesSimpleMapping() {
		var tbl = """
		41=A
		42=B
		43=C
		""";

		var encoder = TextEncoder.LoadFromTbl(tbl);

		Assert.Equal(3, encoder.CharMappings.Count);
		Assert.Equal((byte)0x41, encoder.CharMappings["A"]);
		Assert.Equal((byte)0x42, encoder.CharMappings["B"]);
		Assert.Equal((byte)0x43, encoder.CharMappings["C"]);
	}

	[Fact]
	public void LoadFromTbl_ParsesLowercaseHex() {
		var tbl = """
		80=A
		81=B
		ff=End
		""";

		var encoder = TextEncoder.LoadFromTbl(tbl);

		Assert.Equal((byte)0x80, encoder.CharMappings["A"]);
		Assert.Equal((byte)0x81, encoder.CharMappings["B"]);
		Assert.Equal((byte)0xff, encoder.CharMappings["End"]);
	}

	[Fact]
	public void LoadFromTbl_ParsesSpaceToken() {
		var tbl = """
		20=<space>
		41=A
		""";

		var encoder = TextEncoder.LoadFromTbl(tbl);

		Assert.Equal((byte)0x20, encoder.CharMappings[" "]);
		Assert.Equal((byte)0x41, encoder.CharMappings["A"]);
	}

	[Fact]
	public void LoadFromTbl_ParsesNameDirective() {
		var tbl = """
		@name=Dragon Quest
		41=A
		""";

		var encoder = TextEncoder.LoadFromTbl(tbl);

		Assert.Equal("Dragon Quest", encoder.Name);
	}

	[Fact]
	public void LoadFromTbl_ParsesEndDirective() {
		var tbl = """
		@end=fc
		41=A
		""";

		var encoder = TextEncoder.LoadFromTbl(tbl);

		Assert.Equal((byte)0xfc, encoder.EndByte);
	}

	[Fact]
	public void LoadFromTbl_SkipsComments() {
		var tbl = """
		; Comment line
		# Also comment
		41=A
		; Another comment
		42=B
		""";

		var encoder = TextEncoder.LoadFromTbl(tbl);

		Assert.Equal(2, encoder.CharMappings.Count);
	}

	[Fact]
	public void CreateAsciiEncoder_ContainsPrintableChars() {
		var encoder = TextEncoder.CreateAsciiEncoder();

		Assert.Equal("ASCII", encoder.Name);
		Assert.Equal((byte)0x20, encoder.CharMappings[" "]);
		Assert.Equal((byte)0x41, encoder.CharMappings["A"]);
		Assert.Equal((byte)0x61, encoder.CharMappings["a"]);
		Assert.Equal((byte)0x30, encoder.CharMappings["0"]);
	}

	[Fact]
	public void Encode_SimpleString() {
		var tbl = """
		80=A
		81=B
		82=C
		""";
		var encoder = TextEncoder.LoadFromTbl(tbl);

		var result = encoder.Encode("ABC");

		Assert.Equal(new byte[] { 0x80, 0x81, 0x82 }, result);
	}

	[Fact]
	public void Encode_WithEndByte() {
		var tbl = """
		@end=fc
		80=A
		81=B
		""";
		var encoder = TextEncoder.LoadFromTbl(tbl);

		var result = encoder.Encode("AB");

		Assert.Equal(new byte[] { 0x80, 0x81, 0xfc }, result);
	}

	[Fact]
	public void Encode_WithSpace() {
		var tbl = """
		80=A
		81=B
		c6=<space>
		""";
		var encoder = TextEncoder.LoadFromTbl(tbl);

		var result = encoder.Encode("A B");

		Assert.Equal(new byte[] { 0x80, 0xc6, 0x81 }, result);
	}

	[Fact]
	public void Encode_WithHexEscape() {
		var encoder = TextEncoder.CreateAsciiEncoder();

		var result = encoder.Encode("A[FF]B");

		Assert.Equal(new byte[] { 0x41, 0xff, 0x42 }, result);
	}

	[Fact]
	public void Encode_WithHexEscapeDollarSign() {
		var encoder = TextEncoder.CreateAsciiEncoder();

		var result = encoder.Encode("A[$FF]B");

		Assert.Equal(new byte[] { 0x41, 0xff, 0x42 }, result);
	}

	[Fact]
	public void Encode_WithControlCode() {
		var tbl = """
		80=A
		81=B
		""";
		var encoder = TextEncoder.LoadFromTbl(tbl);
		encoder.AddControlCode("LINE", [0xfe]);

		var result = encoder.Encode("A[LINE]B");

		Assert.Equal(new byte[] { 0x80, 0xfe, 0x81 }, result);
	}

	[Fact]
	public void Encode_WithMultiByteControlCode() {
		var tbl = """
		80=A
		81=B
		""";
		var encoder = TextEncoder.LoadFromTbl(tbl);
		encoder.AddControlCode("WAIT", [0xfd, 0x10]);

		var result = encoder.Encode("A[WAIT]B");

		Assert.Equal(new byte[] { 0x80, 0xfd, 0x10, 0x81 }, result);
	}

	[Fact]
	public void EncodeAll_MultipleStrings() {
		var tbl = """
		80=A
		81=B
		82=C
		""";
		var encoder = TextEncoder.LoadFromTbl(tbl);

		var results = encoder.EncodeAll(["A", "B", "C"]);

		Assert.Equal(3, results.Count);
		Assert.Equal(new byte[] { 0x80 }, results[0]);
		Assert.Equal(new byte[] { 0x81 }, results[1]);
		Assert.Equal(new byte[] { 0x82 }, results[2]);
	}

	[Fact]
	public void CanEncode_KnownChar_ReturnsTrue() {
		var encoder = TextEncoder.CreateAsciiEncoder();

		Assert.True(encoder.CanEncode('A'));
		Assert.True(encoder.CanEncode(' '));
	}

	[Fact]
	public void CanEncode_UnknownChar_ReturnsFalse() {
		var tbl = """
		41=A
		42=B
		""";
		var encoder = TextEncoder.LoadFromTbl(tbl);

		Assert.False(encoder.CanEncode('Z'));
	}

	[Fact]
	public void CanEncodeAll_AllKnown_ReturnsTrue() {
		var encoder = TextEncoder.CreateAsciiEncoder();

		Assert.True(encoder.CanEncodeAll("Hello World"));
	}

	[Fact]
	public void CanEncodeAll_WithHexEscape_ReturnsTrue() {
		var tbl = """
		41=A
		42=B
		""";
		var encoder = TextEncoder.LoadFromTbl(tbl);

		Assert.True(encoder.CanEncodeAll("A[FF]B"));
	}

	[Fact]
	public void CanEncodeAll_UnknownChar_ReturnsFalse() {
		var tbl = """
		41=A
		42=B
		""";
		var encoder = TextEncoder.LoadFromTbl(tbl);

		Assert.False(encoder.CanEncodeAll("ABC"));
	}

	[Fact]
	public void EncodeToAsm_SimpleString() {
		var encoder = TextEncoder.CreateAsciiEncoder();

		var result = encoder.EncodeToAsm("Hi", "msg_hello", true);

		Assert.Contains("msg_hello:", result);
		Assert.Contains(".byte $48, $69", result); // H, i
		Assert.Contains("; \"Hi\"", result);
	}

	[Fact]
	public void EncodeToAsm_LongString_WrapsLines() {
		var encoder = TextEncoder.CreateAsciiEncoder();

		var result = encoder.EncodeToAsm("HelloWorld", "msg_long", true);

		// Should wrap after 8 bytes
		var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);
		var byteLines = lines.Where(l => l.Contains(".byte")).ToArray();

		Assert.Equal(2, byteLines.Length);
	}

	[Fact]
	public void EncodeToAsm_UppercaseHex() {
		var encoder = TextEncoder.CreateAsciiEncoder();

		var result = encoder.EncodeToAsm("A", "msg", false);

		Assert.Contains("$41", result);
	}

	[Fact]
	public void EncodeAllToAsm_GeneratesPointerTable() {
		var tbl = """
		80=A
		81=B
		82=C
		""";
		var encoder = TextEncoder.LoadFromTbl(tbl);

		var entries = new[] {
			("msg_1", "A"),
			("msg_2", "B"),
			("msg_3", "C")
		};

		var result = encoder.EncodeAllToAsm(entries, "messages", true);

		Assert.Contains("messages_ptrs:", result);
		Assert.Contains(".word msg_1", result);
		Assert.Contains(".word msg_2", result);
		Assert.Contains(".word msg_3", result);
		Assert.Contains("messages_count = $03", result);
		Assert.Contains("messages_end:", result);
	}

	[Fact]
	public void DragonQuestStyleTable_EncodesCorrectly() {
		var tbl = """
		@name=Dragon Quest
		@end=fc
		80=A
		81=B
		82=C
		83=D
		84=E
		85=F
		86=G
		87=H
		88=I
		89=J
		8a=K
		8b=L
		8c=M
		8d=N
		8e=O
		8f=P
		90=Q
		91=R
		92=S
		93=T
		94=U
		95=V
		96=W
		97=X
		98=Y
		99=Z
		c6=<space>
		""";
		var encoder = TextEncoder.LoadFromTbl(tbl);

		var result = encoder.Encode("HERO");

		Assert.Equal(new byte[] { 0x87, 0x84, 0x91, 0x8e, 0xfc }, result);
	}

	[Fact]
	public void LoadFromTbl_FirstMappingWins() {
		// When the same character appears multiple times, first mapping should win
		var tbl = """
		80=A
		90=A
		""";

		var encoder = TextEncoder.LoadFromTbl(tbl);

		Assert.Equal((byte)0x80, encoder.CharMappings["A"]);
	}

	[Fact]
	public void Encode_EmptyString() {
		var encoder = TextEncoder.CreateAsciiEncoder();

		var result = encoder.Encode("");

		Assert.Empty(result);
	}

	[Fact]
	public void Encode_EmptyStringWithEndByte() {
		var encoder = TextEncoder.CreateAsciiEncoder();
		encoder.EndByte = 0x00;

		var result = encoder.Encode("");

		Assert.Equal(new byte[] { 0x00 }, result);
	}
}
