// ============================================================================
// TargetResolverTests.cs - TargetResolver Unit Tests
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================
// Tracks: #261 (Arch layer tests)

using Poppy.Core.Arch;
using Poppy.Core.Semantics;

namespace Poppy.Tests.Arch;

/// <summary>
/// Tests for TargetResolver: string-to-enum mapping, display names, and profile lookup.
/// </summary>
public sealed class TargetResolverTests {
	// ========================================================================
	// Resolve() — CPU Name Aliases
	// ========================================================================

	[Theory]
	[InlineData("6502", TargetArchitecture.MOS6502)]
	[InlineData("mos6502", TargetArchitecture.MOS6502)]
	[InlineData("6507", TargetArchitecture.MOS6507)]
	[InlineData("mos6507", TargetArchitecture.MOS6507)]
	[InlineData("65sc02", TargetArchitecture.MOS65SC02)]
	[InlineData("mos65sc02", TargetArchitecture.MOS65SC02)]
	[InlineData("65816", TargetArchitecture.WDC65816)]
	[InlineData("wdc65816", TargetArchitecture.WDC65816)]
	[InlineData("sm83", TargetArchitecture.SM83)]
	[InlineData("m68000", TargetArchitecture.M68000)]
	[InlineData("68000", TargetArchitecture.M68000)]
	[InlineData("m68k", TargetArchitecture.M68000)]
	[InlineData("z80", TargetArchitecture.Z80)]
	[InlineData("v30mz", TargetArchitecture.V30MZ)]
	[InlineData("arm7tdmi", TargetArchitecture.ARM7TDMI)]
	[InlineData("arm", TargetArchitecture.ARM7TDMI)]
	[InlineData("spc700", TargetArchitecture.SPC700)]
	[InlineData("huc6280", TargetArchitecture.HuC6280)]
	[InlineData("f8", TargetArchitecture.F8)]
	public void Resolve_CpuNames_ReturnsCorrectArchitecture(string name, TargetArchitecture expected) {
		Assert.Equal(expected, TargetResolver.Resolve(name));
	}

	// ========================================================================
	// Resolve() — Platform Name Aliases
	// ========================================================================

	[Theory]
	[InlineData("nes", TargetArchitecture.MOS6502)]
	[InlineData("famicom", TargetArchitecture.MOS6502)]
	[InlineData("fc", TargetArchitecture.MOS6502)]
	[InlineData("atari2600", TargetArchitecture.MOS6507)]
	[InlineData("2600", TargetArchitecture.MOS6507)]
	[InlineData("a26", TargetArchitecture.MOS6507)]
	[InlineData("vcs", TargetArchitecture.MOS6507)]
	[InlineData("lynx", TargetArchitecture.MOS65SC02)]
	[InlineData("atarilynx", TargetArchitecture.MOS65SC02)]
	[InlineData("snes", TargetArchitecture.WDC65816)]
	[InlineData("superfamicom", TargetArchitecture.WDC65816)]
	[InlineData("sfc", TargetArchitecture.WDC65816)]
	[InlineData("gb", TargetArchitecture.SM83)]
	[InlineData("gbc", TargetArchitecture.SM83)]
	[InlineData("gameboy", TargetArchitecture.SM83)]
	[InlineData("gameboycolor", TargetArchitecture.SM83)]
	[InlineData("genesis", TargetArchitecture.M68000)]
	[InlineData("megadrive", TargetArchitecture.M68000)]
	[InlineData("md", TargetArchitecture.M68000)]
	[InlineData("sms", TargetArchitecture.Z80)]
	[InlineData("mastersystem", TargetArchitecture.Z80)]
	[InlineData("ws", TargetArchitecture.V30MZ)]
	[InlineData("wonderswan", TargetArchitecture.V30MZ)]
	[InlineData("wsc", TargetArchitecture.V30MZ)]
	[InlineData("gba", TargetArchitecture.ARM7TDMI)]
	[InlineData("gameboyadvance", TargetArchitecture.ARM7TDMI)]
	[InlineData("tg16", TargetArchitecture.HuC6280)]
	[InlineData("turbografx16", TargetArchitecture.HuC6280)]
	[InlineData("pcengine", TargetArchitecture.HuC6280)]
	[InlineData("pce", TargetArchitecture.HuC6280)]
	[InlineData("channelf", TargetArchitecture.F8)]
	[InlineData("channel_f", TargetArchitecture.F8)]
	public void Resolve_PlatformNames_ReturnsCorrectArchitecture(string name, TargetArchitecture expected) {
		Assert.Equal(expected, TargetResolver.Resolve(name));
	}

	// ========================================================================
	// Resolve() — Case Insensitivity
	// ========================================================================

	[Theory]
	[InlineData("NES")]
	[InlineData("Nes")]
	[InlineData("nes")]
	[InlineData("nEs")]
	public void Resolve_CaseInsensitive_AllResolveSame(string name) {
		Assert.Equal(TargetArchitecture.MOS6502, TargetResolver.Resolve(name));
	}

	[Theory]
	[InlineData("SNES")]
	[InlineData("Snes")]
	[InlineData("SM83")]
	[InlineData("Sm83")]
	[InlineData("GB")]
	[InlineData("GBA")]
	[InlineData("ARM7TDMI")]
	[InlineData("V30MZ")]
	[InlineData("Z80")]
	[InlineData("M68K")]
	public void Resolve_UppercaseVariants_ReturnsNonNull(string name) {
		Assert.NotNull(TargetResolver.Resolve(name));
	}

	// ========================================================================
	// Resolve() — Unknown Names
	// ========================================================================

	[Theory]
	[InlineData("")]
	[InlineData("unknown")]
	[InlineData("x86")]
	[InlineData("mips")]
	[InlineData("risc-v")]
	[InlineData("powerpc")]
	[InlineData("8080")]
	public void Resolve_UnknownNames_ReturnsNull(string name) {
		Assert.Null(TargetResolver.Resolve(name));
	}

	// ========================================================================
	// GetDisplayName() — All Architectures
	// ========================================================================

	[Theory]
	[InlineData(TargetArchitecture.MOS6502, "MOS 6502 (NES)")]
	[InlineData(TargetArchitecture.MOS6507, "MOS 6507 (Atari 2600)")]
	[InlineData(TargetArchitecture.MOS65SC02, "MOS 65SC02 (Atari Lynx)")]
	[InlineData(TargetArchitecture.WDC65816, "WDC 65816 (SNES)")]
	[InlineData(TargetArchitecture.SM83, "Sharp SM83 (Game Boy)")]
	[InlineData(TargetArchitecture.M68000, "Motorola 68000 (Genesis)")]
	[InlineData(TargetArchitecture.Z80, "Zilog Z80 (SMS)")]
	[InlineData(TargetArchitecture.V30MZ, "NEC V30MZ (WonderSwan)")]
	[InlineData(TargetArchitecture.ARM7TDMI, "ARM7TDMI (GBA)")]
	[InlineData(TargetArchitecture.SPC700, "Sony SPC700")]
	[InlineData(TargetArchitecture.HuC6280, "Hudson HuC6280 (TG-16)")]
	[InlineData(TargetArchitecture.F8, "Fairchild F8 (Channel F)")]
	public void GetDisplayName_AllArchitectures_ReturnsExpected(TargetArchitecture arch, string expected) {
		Assert.Equal(expected, TargetResolver.GetDisplayName(arch));
	}

	// ========================================================================
	// GetProfile() — All Supported Architectures
	// ========================================================================

	[Theory]
	[InlineData(TargetArchitecture.MOS6502)]
	[InlineData(TargetArchitecture.MOS6507)]
	[InlineData(TargetArchitecture.MOS65SC02)]
	[InlineData(TargetArchitecture.WDC65816)]
	[InlineData(TargetArchitecture.SM83)]
	[InlineData(TargetArchitecture.M68000)]
	[InlineData(TargetArchitecture.Z80)]
	[InlineData(TargetArchitecture.V30MZ)]
	[InlineData(TargetArchitecture.ARM7TDMI)]
	[InlineData(TargetArchitecture.SPC700)]
	[InlineData(TargetArchitecture.HuC6280)]
	public void GetProfile_SupportedArchitectures_ReturnsNonNull(TargetArchitecture arch) {
		var profile = TargetResolver.GetProfile(arch);
		Assert.NotNull(profile);
		Assert.Equal(arch, profile.Architecture);
	}

	[Fact]
	public void GetProfile_F8_ThrowsNotSupported() {
		Assert.Throws<NotSupportedException>(() => TargetResolver.GetProfile(TargetArchitecture.F8));
	}

	// ========================================================================
	// GetProfile() — Singleton Behavior
	// ========================================================================

	[Theory]
	[InlineData(TargetArchitecture.MOS6502)]
	[InlineData(TargetArchitecture.WDC65816)]
	[InlineData(TargetArchitecture.SM83)]
	[InlineData(TargetArchitecture.M68000)]
	[InlineData(TargetArchitecture.Z80)]
	public void GetProfile_CalledTwice_ReturnsSameInstance(TargetArchitecture arch) {
		var first = TargetResolver.GetProfile(arch);
		var second = TargetResolver.GetProfile(arch);
		Assert.Same(first, second);
	}

	// ========================================================================
	// Round-Trip: Resolve → GetProfile
	// ========================================================================

	[Theory]
	[InlineData("nes")]
	[InlineData("snes")]
	[InlineData("gb")]
	[InlineData("gba")]
	[InlineData("genesis")]
	[InlineData("sms")]
	[InlineData("ws")]
	[InlineData("lynx")]
	[InlineData("2600")]
	[InlineData("pce")]
	[InlineData("spc700")]
	public void RoundTrip_ResolveThenGetProfile_MatchesArchitecture(string name) {
		var arch = TargetResolver.Resolve(name);
		Assert.NotNull(arch);

		var profile = TargetResolver.GetProfile(arch.Value);
		Assert.Equal(arch.Value, profile.Architecture);
	}
}
