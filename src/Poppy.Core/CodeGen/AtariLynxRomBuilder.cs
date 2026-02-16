// ============================================================================
// AtariLynxRomBuilder.cs - Atari Lynx LNX ROM File Generator
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

namespace Poppy.Core.CodeGen;

/// <summary>
/// Builds Atari Lynx LNX ROM files from code segments.
/// </summary>
/// <remarks>
/// <para>
/// The LNX format is a container format for Atari Lynx ROMs. It consists of
/// a 64-byte header followed by the ROM data.
/// </para>
/// <para>
/// LNX Header Format (64 bytes):
/// - Offset 0-3: Magic "LYNX" (4 bytes)
/// - Offset 4-5: Bank 0 page count (16-bit little-endian, 256 bytes per page)
/// - Offset 6-7: Bank 1 page count (16-bit little-endian)
/// - Offset 8-9: Version (16-bit little-endian)
/// - Offset 10-41: Cart name (32 bytes, null-terminated ASCII)
/// - Offset 42-57: Manufacturer name (16 bytes, null-terminated ASCII)
/// - Offset 58: Rotation (0=None, 1=Left, 2=Right)
/// - Offset 59-63: Reserved/spare bytes (5 bytes)
/// </para>
/// <para>
/// The Lynx loads ROM data to RAM at $0200. Code/data offsets in segments
/// should use CPU addresses ($0200+), which the builder converts to ROM offsets.
/// </para>
/// </remarks>
public sealed class AtariLynxRomBuilder {
	private readonly Dictionary<int, byte> _rom;
	private readonly int _bank0Size;
	private readonly int _bank1Size;
	private readonly string _gameName;
	private readonly string _manufacturer;
	private readonly LynxRotation _rotation;
	private ushort _version;

	/// <summary>LNX header size in bytes.</summary>
	public const int HeaderSize = 64;

	/// <summary>Size of each page in bytes.</summary>
	public const int PageSize = 256;

	/// <summary>CPU load address where ROM data is loaded.</summary>
	public const int LoadAddress = 0x0200;

	// LNX Magic bytes
	private const byte MagicL = 0x4c;
	private const byte MagicY = 0x59;
	private const byte MagicN = 0x4e;
	private const byte MagicX = 0x58;

	/// <summary>
	/// Creates a new Atari Lynx ROM builder.
	/// </summary>
	/// <param name="bank0Size">Size of bank 0 in bytes (must be multiple of 256).</param>
	/// <param name="bank1Size">Size of bank 1 in bytes (0 for single-bank ROMs).</param>
	/// <param name="gameName">The name of the game (max 32 characters).</param>
	/// <param name="manufacturer">The manufacturer name (max 16 characters).</param>
	/// <param name="rotation">Screen rotation mode.</param>
	/// <param name="version">LNX format version (default 1).</param>
	public AtariLynxRomBuilder(
		int bank0Size = 131072,
		int bank1Size = 0,
		string gameName = "Poppy Game",
		string manufacturer = "Poppy",
		LynxRotation rotation = LynxRotation.None,
		ushort version = 1) {

		// Validate bank sizes (must be multiple of page size)
		if (bank0Size < 0 || bank0Size % PageSize != 0) {
			throw new ArgumentException(
				$"Bank 0 size must be a non-negative multiple of {PageSize}. Got: {bank0Size}",
				nameof(bank0Size));
		}
		if (bank1Size < 0 || bank1Size % PageSize != 0) {
			throw new ArgumentException(
				$"Bank 1 size must be a non-negative multiple of {PageSize}. Got: {bank1Size}",
				nameof(bank1Size));
		}
		if (bank0Size == 0 && bank1Size > 0) {
			throw new ArgumentException("Bank 1 cannot be used without bank 0.", nameof(bank1Size));
		}

		_bank0Size = bank0Size;
		_bank1Size = bank1Size;
		_gameName = gameName.Length > 32 ? gameName[..32] : gameName;
		_manufacturer = manufacturer.Length > 16 ? manufacturer[..16] : manufacturer;
		_rotation = rotation;
		_version = version;
		_rom = new Dictionary<int, byte>();
	}

	/// <summary>
	/// Total ROM size in bytes (excluding header).
	/// </summary>
	public int RomSize => _bank0Size + _bank1Size;

	/// <summary>
	/// Gets the number of pages for bank 0.
	/// </summary>
	public ushort Bank0Pages => (ushort)(_bank0Size / PageSize);

	/// <summary>
	/// Gets the number of pages for bank 1.
	/// </summary>
	public ushort Bank1Pages => (ushort)(_bank1Size / PageSize);

	/// <summary>
	/// Adds a code segment to the ROM.
	/// </summary>
	/// <param name="address">CPU address ($0200+) or raw ROM offset.</param>
	/// <param name="data">The binary data to write.</param>
	/// <param name="bank">Target bank (0 or 1). Default is 0.</param>
	public void AddSegment(int address, byte[] data, int bank = 0) {
		if (bank < 0 || bank > 1) {
			throw new ArgumentException("Bank must be 0 or 1.", nameof(bank));
		}
		if (bank == 1 && _bank1Size == 0) {
			throw new InvalidOperationException("Cannot add to bank 1 when bank1Size is 0.");
		}

		// Map CPU address to ROM offset
		// If address >= LoadAddress, treat as CPU address and subtract
		// Otherwise, treat as raw ROM offset
		var baseOffset = address >= LoadAddress ? address - LoadAddress : address;

		// Add bank offset for bank 1
		if (bank == 1) {
			baseOffset += _bank0Size;
		}

		var maxSize = RomSize;
		for (int i = 0; i < data.Length; i++) {
			var romOffset = baseOffset + i;
			if (romOffset >= 0 && romOffset < maxSize) {
				_rom[romOffset] = data[i];
			}
		}
	}

	/// <summary>
	/// Injects standard boot code at the start of the ROM.
	/// </summary>
	/// <param name="entryPoint">The entry point address to jump to after boot.</param>
	/// <remarks>
	/// Boot code is placed at $0200 (ROM offset 0). The entry point should typically
	/// be after the boot code, for example at $0230 or wherever your main code starts.
	/// </remarks>
	public void InjectBootCode(int entryPoint) {
		var bootCode = LynxBootCodeGenerator.GenerateBootCode(entryPoint);
		AddSegment(LoadAddress, bootCode, 0);
	}

	/// <summary>
	/// Injects minimal boot code at the start of the ROM.
	/// </summary>
	/// <param name="entryPoint">The entry point address to jump to after minimal boot.</param>
	/// <remarks>
	/// Minimal boot code only disables interrupts, clears decimal mode, sets up stack,
	/// and jumps to entry point. Use this when your code handles hardware initialization.
	/// </remarks>
	public void InjectMinimalBootCode(int entryPoint) {
		var bootCode = LynxBootCodeGenerator.GenerateMinimalBootCode(entryPoint);
		AddSegment(LoadAddress, bootCode, 0);
	}

	/// <summary>
	/// Builds the final LNX ROM binary with header.
	/// </summary>
	/// <returns>The complete ROM binary including 64-byte LNX header.</returns>
	public byte[] Build() {
		var output = new byte[HeaderSize + RomSize];

		// Initialize ROM area with $ff (typical for unused ROM space)
		Array.Fill(output, (byte)0xff, HeaderSize, RomSize);

		// Build LNX header
		BuildHeader(output);

		// Copy all segments into the output (after header)
		foreach (var kvp in _rom) {
			if (kvp.Key >= 0 && kvp.Key < RomSize) {
				output[HeaderSize + kvp.Key] = kvp.Value;
			}
		}

		return output;
	}

	/// <summary>
	/// Builds the raw ROM binary without LNX header.
	/// </summary>
	/// <returns>The raw ROM binary (no header).</returns>
	public byte[] BuildRaw() {
		var output = new byte[RomSize];
		Array.Fill(output, (byte)0xff);

		foreach (var kvp in _rom) {
			if (kvp.Key >= 0 && kvp.Key < RomSize) {
				output[kvp.Key] = kvp.Value;
			}
		}

		return output;
	}

	/// <summary>
	/// Builds the LNX header.
	/// </summary>
	private void BuildHeader(byte[] output) {
		// Magic "LYNX" (offset 0-3)
		output[0] = MagicL;
		output[1] = MagicY;
		output[2] = MagicN;
		output[3] = MagicX;

		// Bank 0 page count (offset 4-5, little-endian)
		output[4] = (byte)(Bank0Pages & 0xff);
		output[5] = (byte)((Bank0Pages >> 8) & 0xff);

		// Bank 1 page count (offset 6-7, little-endian)
		output[6] = (byte)(Bank1Pages & 0xff);
		output[7] = (byte)((Bank1Pages >> 8) & 0xff);

		// Version (offset 8-9, little-endian)
		output[8] = (byte)(_version & 0xff);
		output[9] = (byte)((_version >> 8) & 0xff);

		// Cart name (offset 10-41, null-terminated ASCII)
		var nameBytes = System.Text.Encoding.ASCII.GetBytes(_gameName);
		Array.Copy(nameBytes, 0, output, 10, Math.Min(nameBytes.Length, 32));
		// Ensure null termination if name is shorter than 32 chars
		if (nameBytes.Length < 32) {
			output[10 + nameBytes.Length] = 0;
		}

		// Manufacturer name (offset 42-57, null-terminated ASCII)
		var mfgBytes = System.Text.Encoding.ASCII.GetBytes(_manufacturer);
		Array.Copy(mfgBytes, 0, output, 42, Math.Min(mfgBytes.Length, 16));
		if (mfgBytes.Length < 16) {
			output[42 + mfgBytes.Length] = 0;
		}

		// Rotation (offset 58)
		output[58] = (byte)_rotation;

		// Spare bytes (offset 59-63) - leave as 0
		output[59] = 0;
		output[60] = 0;
		output[61] = 0;
		output[62] = 0;
		output[63] = 0;
	}
}

/// <summary>
/// Screen rotation modes for Atari Lynx games.
/// </summary>
public enum LynxRotation : byte {
	/// <summary>No rotation (default horizontal orientation).</summary>
	None = 0,

	/// <summary>Rotate display left 90 degrees.</summary>
	Left = 1,

	/// <summary>Rotate display right 90 degrees.</summary>
	Right = 2
}
