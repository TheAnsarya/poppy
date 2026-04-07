namespace Poppy.Core.Arch;

/// <summary>
/// Centralizes target architecture string-to-enum mapping.
/// Eliminates duplication between SemanticAnalyzer, CodeGenerator, and CLI.
/// </summary>
public static class TargetResolver {
	/// <summary>
	/// Resolves a target name string to a TargetArchitecture enum value.
	/// Accepts all known aliases (platform names, CPU names, abbreviations).
	/// </summary>
	/// <returns>The resolved architecture, or null if unrecognized.</returns>
	public static TargetArchitecture? Resolve(string name) {
		return name.ToLowerInvariant() switch {
			"6502" or "mos6502" or "nes" or "famicom" or "fc" => TargetArchitecture.MOS6502,
			"6507" or "mos6507" or "atari2600" or "2600" or "a26" or "vcs" => TargetArchitecture.MOS6507,
			"65sc02" or "mos65sc02" or "lynx" or "atarilynx" => TargetArchitecture.MOS65SC02,
			"65816" or "wdc65816" or "snes" or "superfamicom" or "sfc" => TargetArchitecture.WDC65816,
			"sm83" or "gb" or "gbc" or "gameboy" or "gameboycolor" => TargetArchitecture.SM83,
			"m68000" or "68000" or "m68k" or "genesis" or "megadrive" or "md" => TargetArchitecture.M68000,
			"z80" or "sms" or "mastersystem" or "gg" or "gamegear" => TargetArchitecture.Z80,
			"v30mz" or "ws" or "wonderswan" or "wsc" => TargetArchitecture.V30MZ,
			"arm7tdmi" or "arm" or "arm7" or "gba" or "gameboyadvance" => TargetArchitecture.ARM7TDMI,
			"spc700" or "spc" => TargetArchitecture.SPC700,
			"huc6280" or "tg16" or "turbografx16" or "pcengine" or "pce" => TargetArchitecture.HuC6280,
			"f8" or "channelf" or "channel_f" or "channel-f" => TargetArchitecture.F8,
			_ => null
		};
	}

	/// <summary>
	/// Gets the canonical display name for a target architecture.
	/// </summary>
	public static string GetDisplayName(TargetArchitecture arch) {
		return arch switch {
			TargetArchitecture.MOS6502 => "MOS 6502 (NES)",
			TargetArchitecture.MOS6507 => "MOS 6507 (Atari 2600)",
			TargetArchitecture.MOS65SC02 => "MOS 65SC02 (Atari Lynx)",
			TargetArchitecture.WDC65816 => "WDC 65816 (SNES)",
			TargetArchitecture.SM83 => "Sharp SM83 (Game Boy)",
			TargetArchitecture.M68000 => "Motorola 68000 (Genesis)",
			TargetArchitecture.Z80 => "Zilog Z80 (SMS)",
			TargetArchitecture.V30MZ => "NEC V30MZ (WonderSwan)",
			TargetArchitecture.ARM7TDMI => "ARM7TDMI (GBA)",
			TargetArchitecture.SPC700 => "Sony SPC700",
			TargetArchitecture.HuC6280 => "Hudson HuC6280 (TG-16)",
			TargetArchitecture.F8 => "Fairchild F8 (Channel F)",
			_ => arch.ToString()
		};
	}

	/// <summary>
	/// Gets the ITargetProfile for a given architecture.
	/// </summary>
	public static ITargetProfile GetProfile(TargetArchitecture arch) {
		return TryGetProfile(arch) ?? throw new NotSupportedException($"No profile for architecture: {arch}");
	}

	/// <summary>
	/// Gets the ITargetProfile for a given architecture, or null if unsupported.
	/// </summary>
	public static ITargetProfile? TryGetProfile(TargetArchitecture arch) {
		return arch switch {
			TargetArchitecture.MOS6502 => Profiles.Mos6502Profile.Instance,
			TargetArchitecture.MOS6507 => Profiles.Mos6507Profile.Instance,
			TargetArchitecture.MOS65SC02 => Profiles.Mos65sc02Profile.Instance,
			TargetArchitecture.WDC65816 => Profiles.Wdc65816Profile.Instance,
			TargetArchitecture.SM83 => Profiles.Sm83Profile.Instance,
			TargetArchitecture.M68000 => Profiles.M68000Profile.Instance,
			TargetArchitecture.Z80 => Profiles.Z80Profile.Instance,
			TargetArchitecture.V30MZ => Profiles.V30mzProfile.Instance,
			TargetArchitecture.ARM7TDMI => Profiles.Arm7tdmiProfile.Instance,
			TargetArchitecture.SPC700 => Profiles.Spc700Profile.Instance,
			TargetArchitecture.HuC6280 => Profiles.Huc6280Profile.Instance,
			_ => null
		};
	}
}
