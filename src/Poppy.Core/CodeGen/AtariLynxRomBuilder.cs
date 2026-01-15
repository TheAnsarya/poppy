// ============================================================================
// AtariLynxRomBuilder.cs - Atari Lynx ROM File Generator
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

namespace Poppy.Core.CodeGen;

/// <summary>
/// Builds Atari Lynx ROM files from code segments.
/// The Lynx uses a simple ROM format with a 64-byte header.
/// </summary>
public sealed class AtariLynxRomBuilder {
	private readonly Dictionary<int, byte> _rom;
	private readonly int _romSize;
	private readonly string _gameName;

	// Lynx ROM header constants
	private const int HeaderSize = 64;
	private const byte MagicByte0 = 0x4c; // 'L'
	private const byte MagicByte1 = 0x59; // 'Y'
	private const byte MagicByte2 = 0x4e; // 'N'
	private const byte MagicByte3 = 0x58; // 'X'

	/// <summary>
	/// Creates a new Atari Lynx ROM builder.
	/// </summary>
	/// <param name="romSize">The size of the ROM in bytes (typical: 128K, 256K, 512K).</param>
	/// <param name="gameName">The name of the game (max 32 characters, appears in Lynx menu).</param>
	public AtariLynxRomBuilder(int romSize = 131072, string gameName = "Poppy Game") {
		_romSize = romSize;
		_gameName = gameName;
		_rom = new Dictionary<int, byte>();

		// Validate ROM size
		if (romSize < 128 || romSize > 2097152) {
			throw new ArgumentException($"Invalid ROM size: {romSize}. Must be between 128 bytes and 2MB.", nameof(romSize));
		}

		// Trim game name to 32 characters
		if (_gameName.Length > 32) {
			_gameName = _gameName[..32];
		}
	}

	/// <summary>
	/// Adds a code segment to the ROM.
	/// </summary>
	/// <param name="address">The starting address for the segment (CPU address >= 0x0200, or ROM offset if &lt; 0x0200).</param>
	/// <param name="data">The binary data to write.</param>
	public void AddSegment(int address, byte[] data) {
		// Map CPU address to ROM offset
		// Load address is $0200, so subtract to get ROM offset
		// If address < $0200, treat it as a ROM offset already
		const int LoadAddress = 0x0200;

		for (int i = 0; i < data.Length; i++) {
			var addr = address + i;
			var romAddress = addr >= LoadAddress ? addr - LoadAddress : addr;

			if (romAddress >= 0 && romAddress < _romSize) {
				_rom[romAddress] = data[i];
			}
		}
	}

	/// <summary>
	/// Builds the final ROM binary with Lynx header.
	/// </summary>
	/// <returns>The complete ROM binary including header.</returns>
	public byte[] Build() {
		var output = new byte[HeaderSize + _romSize];

		// Initialize ROM with $ff (common for unused ROM space)
		Array.Fill(output, (byte)0xff, HeaderSize, _romSize);

		// Build header
		BuildHeader(output);

		// Copy all segments into the output (after header)
		foreach (var kvp in _rom) {
			if (kvp.Key >= 0 && kvp.Key < _romSize) {
				output[HeaderSize + kvp.Key] = kvp.Value;
			}
		}

		return output;
	}

	/// <summary>
	/// Builds the Lynx ROM header.
	/// </summary>
	/// <param name="output">The output buffer (must be at least 64 bytes).</param>
	private void BuildHeader(byte[] output) {
		// Magic number "LYNX" at offset 0-3
		output[0] = MagicByte0;
		output[1] = MagicByte1;
		output[2] = MagicByte2;
		output[3] = MagicByte3;

		// Page size in pages of 256 bytes (offset 4-5, little-endian)
		var pageSize = (ushort)(_romSize / 256);
		output[4] = (byte)(pageSize & 0xff);
		output[5] = (byte)((pageSize >> 8) & 0xff);

		// Load address (offset 6-7, little-endian)
		// Typically $0200 for Lynx games (after system RAM)
		output[6] = 0x00;
		output[7] = 0x02;

		// Start address (offset 8-9, little-endian)
		// This is the entry point of the game
		// Default to $0200 if not explicitly set
		output[8] = 0x00;
		output[9] = 0x02;

		// Game name (offset 10-41, ASCII, null-terminated)
		var nameBytes = System.Text.Encoding.ASCII.GetBytes(_gameName);
		Array.Copy(nameBytes, 0, output, 10, Math.Min(nameBytes.Length, 32));

		// Manufacturer (offset 42-57, ASCII, null-terminated)
		var manufacturer = "Poppy Compiler";
		var mfgBytes = System.Text.Encoding.ASCII.GetBytes(manufacturer);
		Array.Copy(mfgBytes, 0, output, 42, Math.Min(mfgBytes.Length, 16));

		// Rotation (offset 58)
		// 0 = no rotation, 1 = left rotation, 2 = right rotation
		output[58] = 0;

		// Spare bytes (offset 59-63) - typically unused
		// Leave as 0xff (already filled)
	}

	/// <summary>
	/// Sets a custom start address in the ROM header.
	/// </summary>
	/// <param name="address">The start address (entry point).</param>
	public void SetStartAddress(ushort address) {
		// This will be used when building the header
		// For now, we'll keep it simple and use the default
		// TODO: Store this and use it in BuildHeader
	}
}
