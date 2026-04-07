// WonderSwanRomBuilderTests.cs
// Unit tests for WonderSwan ROM builder

using Poppy.Arch.V30MZ;
using Poppy.Core.CodeGen;
using Xunit;

namespace Poppy.Tests.CodeGen;

/// <summary>
/// Unit tests for the WonderSwan ROM builder.
/// </summary>
public class WonderSwanRomBuilderTests {
	#region Instance Build Tests

	[Fact]
	public void Build_Default_ReturnsMinimumSize() {
		var builder = new WonderSwanRomBuilder();
		builder.AddSegment(0, [0x00]);
		var rom = builder.Build();

		// Minimum WonderSwan ROM is 128KB (1Mbit)
		Assert.Equal(128 * 1024, rom.Length);
	}

	[Fact]
	public void Build_HasHeaderAtEnd() {
		var builder = new WonderSwanRomBuilder();
		builder.AddSegment(0, [0xea]);  // NOP (V30MZ)
		var rom = builder.Build();

		// Header is last 10 bytes, publisher ID default is 0x01
		Assert.Equal(0x01, rom[rom.Length - 10]);
	}

	[Fact]
	public void Build_CalculatesChecksum() {
		var builder = new WonderSwanRomBuilder();
		builder.AddSegment(0, [0xea]);
		var rom = builder.Build();

		// Verify checksum at last 2 bytes
		ushort storedChecksum = (ushort)(rom[rom.Length - 2] | (rom[rom.Length - 1] << 8));

		// Recalculate checksum (sum of all bytes except last 2)
		ushort expected = 0;
		for (int i = 0; i < rom.Length - 2; i++) {
			expected += rom[i];
		}

		Assert.Equal(expected, storedChecksum);
	}

	#endregion

	#region AddSegment Tests

	[Fact]
	public void AddSegment_PlacesDataCorrectly() {
		var builder = new WonderSwanRomBuilder();
		var data = new byte[] { 0xea, 0x90, 0xf4 };
		builder.AddSegment(0, data);
		var rom = builder.Build();

		Assert.Equal(0xea, rom[0]);
		Assert.Equal(0x90, rom[1]);
		Assert.Equal(0xf4, rom[2]);
	}

	[Fact]
	public void AddSegment_MultipleSegments_AllPlaced() {
		var builder = new WonderSwanRomBuilder();
		builder.AddSegment(0, [0xea, 0x90]);
		builder.AddSegment(0x100, [0xf4, 0xf5]);
		var rom = builder.Build();

		Assert.Equal(0xea, rom[0]);
		Assert.Equal(0x90, rom[1]);
		Assert.Equal(0xf4, rom[0x100]);
		Assert.Equal(0xf5, rom[0x101]);
	}

	[Fact]
	public void AddSegment_UnusedSpace_FilledWithFF() {
		var builder = new WonderSwanRomBuilder();
		builder.AddSegment(0, [0xea]);
		var rom = builder.Build();

		// Check that space between segment and header is $ff
		Assert.Equal(0xff, rom[1]);
		Assert.Equal(0xff, rom[0x100]);
	}

	#endregion

	#region Custom Header Tests

	[Fact]
	public void Build_WithCustomHeader_UsesHeaderValues() {
		var header = new WonderSwanRomBuilder.WonderSwanHeader {
			PublisherId = 0x42,
			ColorMode = 0x01,
			GameId = 0x07,
		};

		var builder = new WonderSwanRomBuilder(header);
		builder.AddSegment(0, [0xea]);
		var rom = builder.Build();

		int headerOffset = rom.Length - 10;
		Assert.Equal(0x42, rom[headerOffset + 0]);  // Publisher
		Assert.Equal(0x01, rom[headerOffset + 1]);  // Color
		Assert.Equal(0x07, rom[headerOffset + 2]);  // Game ID
	}

	#endregion

	#region Static BuildRom Tests

	[Fact]
	public void BuildRom_Static_ProducesValidRom() {
		var code = new byte[] { 0xea, 0x90, 0xf4 };
		var rom = WonderSwanRomBuilder.BuildRom(code);

		Assert.Equal(128 * 1024, rom.Length);
		Assert.Equal(0xea, rom[0]);
		Assert.Equal(0x90, rom[1]);
		Assert.Equal(0xf4, rom[2]);
	}

	[Fact]
	public void BuildRom_Static_WithHeader_AppliesHeader() {
		var header = new WonderSwanRomBuilder.WonderSwanHeader {
			PublisherId = 0x42,
		};

		var rom = WonderSwanRomBuilder.BuildRom([0xea], header);

		int headerOffset = rom.Length - 10;
		Assert.Equal(0x42, rom[headerOffset]);
	}

	#endregion

	#region ParseHeader Tests

	[Fact]
	public void ParseHeader_SetsPublisher() {
		var metadata = new Dictionary<string, object> {
			{ "publisher", (byte)0x42 },
		};

		var header = WonderSwanRomBuilder.ParseHeader(metadata);
		Assert.Equal(0x42, header.PublisherId);
	}

	[Fact]
	public void ParseHeader_SetsColorMode() {
		var metadata = new Dictionary<string, object> {
			{ "color", (byte)0x01 },
		};

		var header = WonderSwanRomBuilder.ParseHeader(metadata);
		Assert.Equal(0x01, header.ColorMode);
	}

	#endregion
}
