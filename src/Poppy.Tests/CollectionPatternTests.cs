using Poppy.Core.Project;
using Poppy.Core.Semantics;
using Xunit;

namespace Poppy.Tests;

/// <summary>
/// Tests verifying correctness of FrozenSet/FrozenDictionary conversions
/// and collection pattern changes (#179, #180).
/// </summary>
public class CollectionPatternTests {
	// ===== MacroTable.ReservedWords FrozenSet =====

	[Theory]
	[InlineData("lda")]
	[InlineData("sta")]
	[InlineData("jmp")]
	[InlineData("jsr")]
	[InlineData("nop")]
	[InlineData("org")]
	[InlineData("byte")]
	[InlineData("include")]
	[InlineData("macro")]
	[InlineData("enum")]
	public void ReservedWords_ContainsExpectedWords(string word) {
		Assert.True(MacroTable.IsReservedWord(word));
	}

	[Theory]
	[InlineData("LDA")]
	[InlineData("Sta")]
	[InlineData("NOP")]
	[InlineData("INCLUDE")]
	[InlineData("Macro")]
	public void ReservedWords_CaseInsensitive(string word) {
		Assert.True(MacroTable.IsReservedWord(word));
	}

	[Theory]
	[InlineData("myLabel")]
	[InlineData("customMacro")]
	[InlineData("playerX")]
	[InlineData("")]
	public void ReservedWords_RejectsNonReserved(string word) {
		Assert.False(MacroTable.IsReservedWord(word));
	}

	// ===== ManifestValidator.ValidPlatforms FrozenSet =====

	[Theory]
	[InlineData("nes")]
	[InlineData("snes")]
	[InlineData("gb")]
	[InlineData("gba")]
	[InlineData("genesis")]
	[InlineData("sms")]
	[InlineData("atari2600")]
	[InlineData("lynx")]
	[InlineData("wonderswan")]
	[InlineData("tg16")]
	[InlineData("spc700")]
	[InlineData("gbc")]
	[InlineData("channelf")]
	[InlineData("channel-f")]
	[InlineData("channel_f")]
	[InlineData("f8")]
	public void ValidPlatforms_ContainsAllPlatforms(string platform) {
		Assert.True(ManifestValidator.IsValidPlatform(platform));
	}

	[Theory]
	[InlineData("NES")]
	[InlineData("Snes")]
	[InlineData("GBA")]
	[InlineData("GENESIS")]
	[InlineData("ChannelF")]
	[InlineData("CHANNEL-F")]
	[InlineData("F8")]
	public void ValidPlatforms_CaseInsensitive(string platform) {
		Assert.True(ManifestValidator.IsValidPlatform(platform));
	}

	[Theory]
	[InlineData("n64")]
	[InlineData("ps1")]
	[InlineData("")]
	public void ValidPlatforms_RejectsInvalid(string platform) {
		Assert.False(ManifestValidator.IsValidPlatform(platform));
	}

	[Fact]
	public void GetValidPlatforms_ReturnsAllPlatforms() {
		var platforms = ManifestValidator.GetValidPlatforms();

		Assert.Equal(16, platforms.Count);
		Assert.Contains("nes", platforms);
		Assert.Contains("snes", platforms);
		Assert.Contains("gba", platforms);
		Assert.Contains("channelf", platforms);
		Assert.Contains("f8", platforms);
	}

	[Fact]
	public void GetValidPlatforms_ReturnsSorted() {
		var platforms = ManifestValidator.GetValidPlatforms();

		for (int i = 1; i < platforms.Count; i++) {
			Assert.True(
				string.Compare(platforms[i - 1], platforms[i], StringComparison.Ordinal) <= 0,
				$"'{platforms[i - 1]}' should come before '{platforms[i]}'"
			);
		}
	}
}
