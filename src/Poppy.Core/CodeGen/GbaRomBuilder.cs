// GbaRomBuilder.cs
// Game Boy Advance (GBA) ROM header builder
// Generates valid GBA ROM headers at $000000-$0000BF (192 bytes)

using System;
using System.Text;

namespace Poppy.Core.CodeGen;

/// <summary>
/// Builder for generating Game Boy Advance ROM headers.
/// The header is located at $000000-$0000BF (192 bytes).
/// </summary>
public class GbaRomBuilder {
	// Header constants
	private const int HeaderSize = 192;        // $c0 bytes
	private const int NintendoLogoOffset = 4;  // $004
	private const int NintendoLogoSize = 156;  // $9c bytes
	private const int TitleOffset = 0xa0;      // $0a0
	private const int TitleSize = 12;          // 12 characters
	private const int GameCodeOffset = 0xac;   // $0ac
	private const int GameCodeSize = 4;        // 4 characters
	private const int MakerCodeOffset = 0xb0;  // $0b0
	private const int MakerCodeSize = 2;       // 2 characters
	private const int FixedValueOffset = 0xb2; // $0b2
	private const int FixedValue = 0x96;       // Must be $96
	private const int MainUnitOffset = 0xb3;   // $0b3
	private const int DeviceTypeOffset = 0xb4; // $0b4
	private const int ReservedOffset = 0xb5;   // $0b5 (7 bytes)
	private const int VersionOffset = 0xbc;    // $0bc
	private const int ChecksumOffset = 0xbd;   // $0bd
	private const int ReservedEndOffset = 0xbe; // $0be (2 bytes)

	// Default entry point (ARM branch instruction)
	// b $08000000 + offset = ea00007e (branch forward $200 bytes)
	private static readonly byte[] DefaultEntryPoint = [0x7e, 0x00, 0x00, 0xea];

	// Nintendo logo (required for authenticity check)
	// This is the compressed bitmap that must match in BIOS
	private static readonly byte[] NintendoLogo = [
		0x24, 0xff, 0xae, 0x51, 0x69, 0x9a, 0xa2, 0x21, 0x3d, 0x84, 0x82, 0x0a,
		0x84, 0xe4, 0x09, 0xad, 0x11, 0x24, 0x8b, 0x98, 0xc0, 0x81, 0x7f, 0x21,
		0xa3, 0x52, 0xbe, 0x19, 0x93, 0x09, 0xce, 0x20, 0x10, 0x46, 0x4a, 0x4a,
		0xf8, 0x27, 0x31, 0xec, 0x58, 0xc7, 0xe8, 0x33, 0x82, 0xe3, 0xce, 0xbf,
		0x85, 0xf4, 0xdf, 0x94, 0xce, 0x4b, 0x09, 0xc1, 0x94, 0x56, 0x8a, 0xc0,
		0x13, 0x72, 0xa7, 0xfc, 0x9f, 0x84, 0x4d, 0x73, 0xa3, 0xca, 0x9a, 0x61,
		0x58, 0x97, 0xa3, 0x27, 0xfc, 0x03, 0x98, 0x76, 0x23, 0x1d, 0xc7, 0x61,
		0x03, 0x04, 0xae, 0x56, 0xbf, 0x38, 0x84, 0x00, 0x40, 0xa7, 0x0e, 0xfd,
		0xff, 0x52, 0xfe, 0x03, 0x6f, 0x95, 0x30, 0xf1, 0x97, 0xfb, 0xc0, 0x85,
		0x60, 0xd6, 0x80, 0x25, 0xa9, 0x63, 0xbe, 0x03, 0x01, 0x4e, 0x38, 0xe2,
		0xf9, 0xa2, 0x34, 0xff, 0xbb, 0x3e, 0x03, 0x44, 0x78, 0x00, 0x90, 0xcb,
		0x88, 0x11, 0x3a, 0x94, 0x65, 0xc0, 0x7c, 0x63, 0x87, 0xf0, 0x3c, 0xaf,
		0xd6, 0x25, 0xe4, 0x8b, 0x38, 0x0a, 0xac, 0x72, 0x21, 0xd4, 0xf8, 0x07
	];

	// Header fields
	private byte[] _entryPoint = DefaultEntryPoint;
	private string _title = "";
	private string _gameCode = "XXXX";
	private string _makerCode = "00";
	private byte _mainUnit = 0x00;
	private byte _deviceType = 0x00;
	private byte _version = 0x00;

	/// <summary>
	/// Sets the entry point branch instruction.
	/// Default is a branch to $08000200.
	/// </summary>
	/// <param name="entryPoint">4-byte ARM branch instruction</param>
	/// <returns>This builder for chaining</returns>
	public GbaRomBuilder SetEntryPoint(byte[] entryPoint) {
		if (entryPoint == null || entryPoint.Length != 4) {
			throw new ArgumentException("Entry point must be exactly 4 bytes");
		}
		_entryPoint = entryPoint;
		return this;
	}

	/// <summary>
	/// Sets the entry point to branch to a specific address.
	/// Generates an ARM B instruction.
	/// </summary>
	/// <param name="targetAddress">Target address (typically $08000000 + offset)</param>
	/// <returns>This builder for chaining</returns>
	public GbaRomBuilder SetEntryPointAddress(uint targetAddress) {
		// Calculate offset from $08000000 (ROM start)
		// The branch is at $08000000, PC is $08000008 when executed (PC + 8)
		// So offset = target - $08000008
		int offset = (int)(targetAddress - 0x08000008);

		// Encode as ARM B instruction
		_entryPoint = InstructionSetARM7TDMI.EncodeBranch(offset);
		return this;
	}

	/// <summary>
	/// Sets the game title (12 characters max, uppercase ASCII).
	/// </summary>
	/// <param name="title">Game title</param>
	/// <returns>This builder for chaining</returns>
	public GbaRomBuilder SetTitle(string title) {
		_title = title ?? "";
		if (_title.Length > TitleSize) {
			_title = _title[..TitleSize];
		}
		return this;
	}

	/// <summary>
	/// Sets the 4-character game code.
	/// Format: ABCD where A=type, BCD=game identifier
	/// Common types: A=normal, B=special, C=color, etc.
	/// </summary>
	/// <param name="gameCode">4-character game code</param>
	/// <returns>This builder for chaining</returns>
	public GbaRomBuilder SetGameCode(string gameCode) {
		if (string.IsNullOrEmpty(gameCode) || gameCode.Length != GameCodeSize) {
			throw new ArgumentException("Game code must be exactly 4 characters");
		}
		_gameCode = gameCode.ToUpperInvariant();
		return this;
	}

	/// <summary>
	/// Sets the 2-character maker (company) code.
	/// "01" = Nintendo
	/// </summary>
	/// <param name="makerCode">2-character maker code</param>
	/// <returns>This builder for chaining</returns>
	public GbaRomBuilder SetMakerCode(string makerCode) {
		if (string.IsNullOrEmpty(makerCode) || makerCode.Length != MakerCodeSize) {
			throw new ArgumentException("Maker code must be exactly 2 characters");
		}
		_makerCode = makerCode.ToUpperInvariant();
		return this;
	}

	/// <summary>
	/// Sets the main unit code (normally 0).
	/// </summary>
	/// <param name="mainUnit">Main unit code</param>
	/// <returns>This builder for chaining</returns>
	public GbaRomBuilder SetMainUnit(byte mainUnit) {
		_mainUnit = mainUnit;
		return this;
	}

	/// <summary>
	/// Sets the device type (normally 0).
	/// </summary>
	/// <param name="deviceType">Device type</param>
	/// <returns>This builder for chaining</returns>
	public GbaRomBuilder SetDeviceType(byte deviceType) {
		_deviceType = deviceType;
		return this;
	}

	/// <summary>
	/// Sets the software version number.
	/// </summary>
	/// <param name="version">Version (0-255)</param>
	/// <returns>This builder for chaining</returns>
	public GbaRomBuilder SetVersion(byte version) {
		_version = version;
		return this;
	}

	/// <summary>
	/// Builds the complete GBA ROM header (192 bytes).
	/// </summary>
	/// <returns>192-byte header</returns>
	public byte[] Build() {
		var header = new byte[HeaderSize];

		// Entry point ($000-$003) - 4-byte ARM branch instruction
		Array.Copy(_entryPoint, 0, header, 0, 4);

		// Nintendo logo ($004-$09f) - 156 bytes
		Array.Copy(NintendoLogo, 0, header, NintendoLogoOffset, NintendoLogoSize);

		// Game title ($0a0-$0ab) - 12 characters, space padded
		var titleBytes = Encoding.ASCII.GetBytes(_title.PadRight(TitleSize).ToUpperInvariant());
		Array.Copy(titleBytes, 0, header, TitleOffset, TitleSize);

		// Game code ($0ac-$0af) - 4 characters
		var gameCodeBytes = Encoding.ASCII.GetBytes(_gameCode);
		Array.Copy(gameCodeBytes, 0, header, GameCodeOffset, GameCodeSize);

		// Maker code ($0b0-$0b1) - 2 characters
		var makerCodeBytes = Encoding.ASCII.GetBytes(_makerCode);
		Array.Copy(makerCodeBytes, 0, header, MakerCodeOffset, MakerCodeSize);

		// Fixed value ($0b2) - must be $96
		header[FixedValueOffset] = FixedValue;

		// Main unit code ($0b3)
		header[MainUnitOffset] = _mainUnit;

		// Device type ($0b4)
		header[DeviceTypeOffset] = _deviceType;

		// Reserved ($0b5-$0bb) - 7 bytes of zero
		// Already zeroed

		// Software version ($0bc)
		header[VersionOffset] = _version;

		// Header checksum ($0bd) - calculated complement check
		header[ChecksumOffset] = CalculateChecksum(header);

		// Reserved ($0be-$0bf) - 2 bytes of zero
		// Already zeroed

		return header;
	}

	/// <summary>
	/// Calculates the header checksum (complement check).
	/// Sum of bytes $0a0-$0bc, then (0 - sum - $19) and $ff
	/// </summary>
	/// <param name="header">Header bytes to checksum</param>
	/// <returns>Checksum byte</returns>
	public static byte CalculateChecksum(byte[] header) {
		if (header == null || header.Length < 0xbd) {
			throw new ArgumentException("Header too short for checksum calculation");
		}

		int sum = 0;
		for (int i = TitleOffset; i <= VersionOffset; i++) {
			sum += header[i];
		}

		// Complement check: -(sum + $19) & $ff
		return (byte)(-(sum + 0x19) & 0xff);
	}

	/// <summary>
	/// Validates a GBA ROM header.
	/// Checks the fixed value, Nintendo logo, and checksum.
	/// </summary>
	/// <param name="header">Header bytes to validate</param>
	/// <returns>True if header is valid</returns>
	public static bool ValidateHeader(byte[] header) {
		if (header == null || header.Length < HeaderSize) {
			return false;
		}

		// Check fixed value
		if (header[FixedValueOffset] != FixedValue) {
			return false;
		}

		// Check Nintendo logo
		for (int i = 0; i < NintendoLogoSize; i++) {
			if (header[NintendoLogoOffset + i] != NintendoLogo[i]) {
				return false;
			}
		}

		// Check checksum
		byte expectedChecksum = CalculateChecksum(header);
		if (header[ChecksumOffset] != expectedChecksum) {
			return false;
		}

		return true;
	}

	/// <summary>
	/// Gets the Nintendo logo bytes for embedding in ROMs.
	/// </summary>
	/// <returns>156-byte Nintendo logo</returns>
	public static byte[] GetNintendoLogo() {
		var logo = new byte[NintendoLogoSize];
		Array.Copy(NintendoLogo, logo, NintendoLogoSize);
		return logo;
	}

	/// <summary>
	/// Extracts the game title from a ROM header.
	/// </summary>
	/// <param name="header">ROM header bytes</param>
	/// <returns>Game title (trimmed)</returns>
	public static string GetTitle(byte[] header) {
		if (header == null || header.Length < TitleOffset + TitleSize) {
			return "";
		}

		return Encoding.ASCII.GetString(header, TitleOffset, TitleSize).TrimEnd();
	}

	/// <summary>
	/// Extracts the game code from a ROM header.
	/// </summary>
	/// <param name="header">ROM header bytes</param>
	/// <returns>4-character game code</returns>
	public static string GetGameCode(byte[] header) {
		if (header == null || header.Length < GameCodeOffset + GameCodeSize) {
			return "";
		}

		return Encoding.ASCII.GetString(header, GameCodeOffset, GameCodeSize);
	}

	/// <summary>
	/// Extracts the maker code from a ROM header.
	/// </summary>
	/// <param name="header">ROM header bytes</param>
	/// <returns>2-character maker code</returns>
	public static string GetMakerCode(byte[] header) {
		if (header == null || header.Length < MakerCodeOffset + MakerCodeSize) {
			return "";
		}

		return Encoding.ASCII.GetString(header, MakerCodeOffset, MakerCodeSize);
	}

	/// <summary>
	/// Common game code prefixes for GBA games.
	/// </summary>
	public static class GameCodePrefixes {
		/// <summary>Normal GBA game</summary>
		public const char Normal = 'A';

		/// <summary>Game with special features</summary>
		public const char Special = 'B';

		/// <summary>Nintendo DS enhanced game</summary>
		public const char Enhanced = 'C';

		/// <summary>Demo/Sample game</summary>
		public const char Demo = 'F';

		/// <summary>Korea region game</summary>
		public const char Korea = 'K';

		/// <summary>Japan region game</summary>
		public const char Japan = 'J';

		/// <summary>USA region game</summary>
		public const char USA = 'E';

		/// <summary>Europe region game</summary>
		public const char Europe = 'P';
	}
}
