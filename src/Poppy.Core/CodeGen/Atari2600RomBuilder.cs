// ============================================================================
// Atari2600RomBuilder.cs - Atari 2600 ROM File Generator
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

namespace Poppy.Core.CodeGen;

/// <summary>
/// Builds Atari 2600 ROM files from code segments.
/// Supports standard 2K/4K ROMs and bank-switched configurations (F8, F6, F4, etc.).
/// </summary>
public sealed class Atari2600RomBuilder {
	private readonly Dictionary<int, byte> _rom;
	private readonly int _romSize;
	private readonly BankSwitchingMethod _bankSwitching;

	/// <summary>
	/// Bank switching methods supported by the Atari 2600.
	/// </summary>
	public enum BankSwitchingMethod {
		/// <summary>No bank switching - standard 2K or 4K ROM.</summary>
		None,

		/// <summary>F8 bank switching - 8K ROM, 2 banks of 4K.</summary>
		F8,

		/// <summary>F6 bank switching - 16K ROM, 4 banks of 4K.</summary>
		F6,

		/// <summary>F4 bank switching - 32K ROM, 8 banks of 4K.</summary>
		F4,

		/// <summary>FE bank switching - 8K ROM, 2 banks of 4K (DecaThlon method).</summary>
		FE,

		/// <summary>E0 bank switching - 8K ROM, 4 banks of 2K.</summary>
		E0,

		/// <summary>3F bank switching - up to 512K ROM.</summary>
		ThreeF,

		/// <summary>E7 bank switching - 16K ROM with 2K RAM.</summary>
		E7,
	}

	/// <summary>
	/// Creates a new Atari 2600 ROM builder.
	/// </summary>
	/// <param name="romSize">The size of the ROM in bytes (2048, 4096, 8192, 16384, 32768, etc.).</param>
	/// <param name="bankSwitching">The bank switching method to use.</param>
	public Atari2600RomBuilder(int romSize = 4096, BankSwitchingMethod bankSwitching = BankSwitchingMethod.None) {
		_romSize = romSize;
		_bankSwitching = bankSwitching;
		_rom = new Dictionary<int, byte>();

		// Validate ROM size
		if (romSize < 2048 || romSize > 524288) {
			throw new ArgumentException($"Invalid ROM size: {romSize}. Must be between 2048 and 524288 bytes.", nameof(romSize));
		}

		// Validate ROM size matches bank switching method
		ValidateBankSwitching();
	}

	/// <summary>
	/// Adds a code segment to the ROM.
	/// </summary>
	/// <param name="address">The starting address for the segment.</param>
	/// <param name="data">The binary data to write.</param>
	public void AddSegment(int address, byte[] data) {
		// Atari 2600 uses 13-bit addressing ($0000-$1fff)
		// The actual ROM is mirrored throughout the address space
		// We need to map the logical address to the physical ROM address

		for (int i = 0; i < data.Length; i++) {
			var romAddress = MapAddress(address + i);
			if (romAddress >= 0 && romAddress < _romSize) {
				_rom[romAddress] = data[i];
			}
		}
	}

	/// <summary>
	/// Builds the final ROM binary.
	/// </summary>
	/// <returns>The complete ROM binary.</returns>
	public byte[] Build() {
		var output = new byte[_romSize];

		// Initialize ROM with $ff (common for unused ROM space)
		Array.Fill(output, (byte)0xff);

		// Copy all segments into the output
		foreach (var kvp in _rom) {
			output[kvp.Key] = kvp.Value;
		}

		// Ensure reset vectors are set at the end of the ROM
		// The 6507 reads reset vector from $fffc/$fffd (which maps to the end of ROM)
		EnsureResetVectors(output);

		return output;
	}

	/// <summary>
	/// Maps a logical 6507 address to a physical ROM address.
	/// </summary>
	/// <param name="address">The logical address ($0000-$1fff).</param>
	/// <returns>The physical ROM offset.</returns>
	private int MapAddress(int address) {
		// The 6507 has a 13-bit address space ($0000-$1fff)
		// ROM typically lives in the upper 4K ($1000-$1fff for 4K ROMs)
		// or the entire space for 2K ROMs ($0000-$07ff, mirrored)

		// Mask to 13 bits
		address &= 0x1fff;

		// For standard ROMs, map the upper address space to ROM
		if (_bankSwitching == BankSwitchingMethod.None) {
			if (_romSize == 2048) {
				// 2K ROM: $1000-$17ff (mirrored at $1800-$1fff)
				if (address >= 0x1000) {
					return (address - 0x1000) & 0x07ff;
				}
			} else if (_romSize == 4096) {
				// 4K ROM: $1000-$1fff
				if (address >= 0x1000) {
					return address - 0x1000;
				}
			}
		}

		// For bank-switched ROMs, the mapping is more complex
		// For now, we'll use a simple linear mapping
		// Bank switching is typically handled at runtime by the emulator/hardware
		return address % _romSize;
	}

	/// <summary>
	/// Ensures the reset vectors are properly set.
	/// </summary>
	/// <param name="output">The ROM output buffer.</param>
	private void EnsureResetVectors(byte[] output) {
		// The 6507 reset vector is at $fffc/$fffd (in the 6502 address space)
		// In the Atari 2600, this maps to the last 2 bytes of the ROM
		// Many assemblers place a default reset vector if none is specified

		var vectorOffset = _romSize - 4; // $fffc maps to ROM_SIZE-4

		// Only set default vector if none is specified
		// Check if the vector area is still $ff (uninitialized)
		if (output[vectorOffset] == 0xff && output[vectorOffset + 1] == 0xff) {
			// Set a default reset vector pointing to the start of ROM ($1000 for 4K)
			var defaultResetAddress = _romSize == 2048 ? 0x1000 : 0x1000;
			output[vectorOffset] = (byte)(defaultResetAddress & 0xff);
			output[vectorOffset + 1] = (byte)((defaultResetAddress >> 8) & 0xff);
		}
	}

	/// <summary>
	/// Validates that the ROM size matches the bank switching method.
	/// </summary>
	private void ValidateBankSwitching() {
		switch (_bankSwitching) {
			case BankSwitchingMethod.None:
				if (_romSize != 2048 && _romSize != 4096) {
					throw new ArgumentException($"Bank switching 'None' requires 2K or 4K ROM, got {_romSize} bytes.");
				}

				break;

			case BankSwitchingMethod.F8:
			case BankSwitchingMethod.FE:
			case BankSwitchingMethod.E0:
				if (_romSize != 8192) {
					throw new ArgumentException($"Bank switching '{_bankSwitching}' requires 8K ROM, got {_romSize} bytes.");
				}

				break;

			case BankSwitchingMethod.F6:
			case BankSwitchingMethod.E7:
				if (_romSize != 16384) {
					throw new ArgumentException($"Bank switching '{_bankSwitching}' requires 16K ROM, got {_romSize} bytes.");
				}

				break;

			case BankSwitchingMethod.F4:
				if (_romSize != 32768) {
					throw new ArgumentException($"Bank switching '{_bankSwitching}' requires 32K ROM, got {_romSize} bytes.");
				}

				break;

			case BankSwitchingMethod.ThreeF:
				// 3F supports up to 512K
				if (_romSize < 4096 || _romSize > 524288) {
					throw new ArgumentException($"Bank switching '3F' requires ROM between 4K and 512K, got {_romSize} bytes.");
				}

				break;
		}
	}
}
