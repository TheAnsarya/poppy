// TurboGrafxRomBuilder.cs
// TurboGrafx-16 / PC Engine ROM header builder
// Generates valid TG16/PCE ROM headers

using System;
using System.Text;

namespace Poppy.Core.CodeGen;

/// <summary>
/// Builder for generating TurboGrafx-16 / PC Engine ROM headers.
/// PCE ROMs have a simpler structure compared to other systems.
/// The format differs slightly between HuCard and CD-ROM.
/// </summary>
public class TurboGrafxRomBuilder {
	// ROM size constants
	private const int MinRomSize = 0x2000;     // 8KB minimum
	private const int MaxRomSize = 0x100000;   // 1MB maximum for HuCard

	/// <summary>
	/// HuCard standard ROM sizes
	/// </summary>
	public static readonly int[] StandardSizes = [
		0x2000,    // 8KB
		0x4000,    // 16KB
		0x8000,    // 32KB
		0x10000,   // 64KB
		0x20000,   // 128KB
		0x40000,   // 256KB
		0x80000,   // 512KB
		0x100000   // 1MB
	];

	// Memory map constants
	private const ushort ResetVector = 0xe000;   // Typical reset vector location
	private const ushort IrqVector = 0xfff6;     // IRQ1 vector
	private const ushort TimerVector = 0xfff8;   // Timer vector
	private const ushort Nmi2Vector = 0xfffa;    // NMI2 vector
	private const ushort ResetVectorLoc = 0xfffe; // Reset vector location

	// Builder state
	private int _romSize = 0x8000;  // Default 32KB
	private string _title = "";
	private byte[] _programData = [];
	private ushort _entryPoint = ResetVector;
	private bool _isSupergrafxCompatible = false;

	/// <summary>
	/// Sets the ROM size. Must be a power of 2, minimum 8KB.
	/// </summary>
	/// <param name="size">ROM size in bytes</param>
	/// <returns>This builder for chaining</returns>
	public TurboGrafxRomBuilder SetRomSize(int size) {
		if (size < MinRomSize || size > MaxRomSize) {
			throw new ArgumentOutOfRangeException(nameof(size), $"ROM size must be between {MinRomSize} and {MaxRomSize}");
		}

		// Check if power of 2
		if ((size & (size - 1)) != 0) {
			throw new ArgumentException("ROM size must be a power of 2", nameof(size));
		}

		_romSize = size;
		return this;
	}

	/// <summary>
	/// Sets the game title (for documentation purposes, not stored in ROM).
	/// </summary>
	/// <param name="title">Game title</param>
	/// <returns>This builder for chaining</returns>
	public TurboGrafxRomBuilder SetTitle(string title) {
		_title = title ?? "";
		return this;
	}

	/// <summary>
	/// Sets the entry point address.
	/// </summary>
	/// <param name="address">Entry point address</param>
	/// <returns>This builder for chaining</returns>
	public TurboGrafxRomBuilder SetEntryPoint(ushort address) {
		_entryPoint = address;
		return this;
	}

	/// <summary>
	/// Sets the program data to include in the ROM.
	/// </summary>
	/// <param name="data">Program binary data</param>
	/// <returns>This builder for chaining</returns>
	public TurboGrafxRomBuilder SetProgramData(byte[] data) {
		_programData = data ?? [];
		return this;
	}

	/// <summary>
	/// Sets SuperGrafx compatibility flag.
	/// </summary>
	/// <param name="compatible">True if SuperGrafx compatible</param>
	/// <returns>This builder for chaining</returns>
	public TurboGrafxRomBuilder SetSupergrafxCompatible(bool compatible) {
		_isSupergrafxCompatible = compatible;
		return this;
	}

	/// <summary>
	/// Builds the complete ROM with header and vectors.
	/// </summary>
	/// <returns>Complete ROM data</returns>
	public byte[] Build() {
		var rom = new byte[_romSize];

		// Fill with $ff (typical for unused ROM space)
		Array.Fill(rom, (byte)0xff);

		// Copy program data if provided
		if (_programData.Length > 0) {
			var copyLength = Math.Min(_programData.Length, _romSize - 8);  // Leave room for vectors
			Array.Copy(_programData, 0, rom, 0, copyLength);
		}

		// Set up interrupt vectors at end of ROM
		// For a typical 32KB ROM mapped to $e000-$ffff:
		// These addresses are relative to the bank, not absolute
		SetVectors(rom);

		return rom;
	}

	/// <summary>
	/// Sets up the interrupt vectors at the end of the ROM.
	/// </summary>
	/// <param name="rom">ROM data array</param>
	private void SetVectors(byte[] rom) {
		// Vector locations are at the end of the ROM
		// For HuCard, the last 8KB is mapped to $e000-$ffff
		// Vectors are at $fff6-$ffff

		int vectorBase = _romSize - 10;  // 10 bytes for all vectors

		// IRQ2/BRK vector ($fff6)
		rom[vectorBase + 0] = (byte)(_entryPoint & 0xff);
		rom[vectorBase + 1] = (byte)((_entryPoint >> 8) & 0xff);

		// IRQ1 vector ($fff8) - VDC interrupt
		rom[vectorBase + 2] = (byte)(_entryPoint & 0xff);
		rom[vectorBase + 3] = (byte)((_entryPoint >> 8) & 0xff);

		// Timer vector ($fffa)
		rom[vectorBase + 4] = (byte)(_entryPoint & 0xff);
		rom[vectorBase + 5] = (byte)((_entryPoint >> 8) & 0xff);

		// NMI vector ($fffc) - not used on TG16, but set anyway
		rom[vectorBase + 6] = (byte)(_entryPoint & 0xff);
		rom[vectorBase + 7] = (byte)((_entryPoint >> 8) & 0xff);

		// Reset vector ($fffe)
		rom[vectorBase + 8] = (byte)(_entryPoint & 0xff);
		rom[vectorBase + 9] = (byte)((_entryPoint >> 8) & 0xff);
	}

	/// <summary>
	/// Creates a minimal bootable ROM with a simple startup routine.
	/// </summary>
	/// <param name="romSize">Desired ROM size</param>
	/// <returns>Complete minimal ROM</returns>
	public static byte[] CreateMinimalRom(int romSize = 0x8000) {
		var builder = new TurboGrafxRomBuilder();
		builder.SetRomSize(romSize);

		// Create a minimal startup routine
		// This sets up the CPU and enters an infinite loop
		var startup = new byte[] {
			// Entry point at $e000 (for 32KB ROM)
			0x78,             // sei - disable interrupts
			0xd8,             // cld - clear decimal mode
			0xa2, 0xff,       // ldx #$ff
			0x9a,             // txs - set stack pointer
			0xa9, 0xff,       // lda #$ff
			0x53, 0x01,       // tam #$01 - set MPR1 to $ff (I/O)
			0xa9, 0xf8,       // lda #$f8
			0x53, 0x02,       // tam #$02 - set MPR2 to $f8 (RAM)
			// Infinite loop
			0x80, 0xfe        // bra *-2
		};

		builder.SetProgramData(startup);
		return builder.Build();
	}

	/// <summary>
	/// Validates a TG16 ROM.
	/// </summary>
	/// <param name="rom">ROM data to validate</param>
	/// <returns>True if valid TG16 ROM format</returns>
	public static bool ValidateRom(byte[] rom) {
		if (rom == null || rom.Length < MinRomSize) {
			return false;
		}

		// Check if size is power of 2
		if ((rom.Length & (rom.Length - 1)) != 0) {
			return false;
		}

		// Check if size is within valid range
		if (rom.Length > MaxRomSize) {
			return false;
		}

		// Check reset vector points to valid address
		int vectorBase = rom.Length - 2;
		ushort resetVector = (ushort)(rom[vectorBase] | (rom[vectorBase + 1] << 8));

		// Reset vector should be in ROM space ($e000-$ffff for standard mapping)
		// This is a loose check since banking can change this
		if (resetVector < 0x8000 && resetVector >= 0x2000) {
			return false;  // Points to RAM or I/O, probably invalid
		}

		return true;
	}

	/// <summary>
	/// Gets information about the ROM size code for banking.
	/// </summary>
	/// <param name="romSize">ROM size in bytes</param>
	/// <returns>Number of 8KB banks</returns>
	public static int GetBankCount(int romSize) {
		return romSize / 0x2000;  // Each bank is 8KB
	}

	/// <summary>
	/// Calculates a simple checksum for the ROM.
	/// </summary>
	/// <param name="rom">ROM data</param>
	/// <returns>16-bit checksum</returns>
	public static ushort CalculateChecksum(byte[] rom) {
		if (rom == null || rom.Length == 0) {
			return 0;
		}

		int sum = 0;
		foreach (var b in rom) {
			sum += b;
		}
		return (ushort)(sum & 0xffff);
	}

	/// <summary>
	/// Common memory page values for TAM instruction.
	/// </summary>
	public static class MemoryPageValues {
		/// <summary>Hardware I/O page</summary>
		public const byte IO = 0xff;

		/// <summary>Work RAM page</summary>
		public const byte RAM = 0xf8;

		/// <summary>CD-ROM RAM page (if CD system)</summary>
		public const byte CdRam = 0x80;

		/// <summary>Super System Card RAM pages ($68-$7f)</summary>
		public const byte SuperRamStart = 0x68;

		/// <summary>Arcade Card RAM pages ($40-$43)</summary>
		public const byte ArcadeRamStart = 0x40;
	}

	/// <summary>
	/// System types for TG16/PCE
	/// </summary>
	public enum SystemType {
		/// <summary>Standard TurboGrafx-16 / PC Engine</summary>
		Standard,

		/// <summary>SuperGrafx (with extra graphics hardware)</summary>
		SuperGrafx,

		/// <summary>TurboGrafx-CD / PC Engine CD-ROM²</summary>
		CdRom,

		/// <summary>Super System Card (256KB RAM)</summary>
		SuperSystemCard,

		/// <summary>Arcade Card (2MB RAM)</summary>
		ArcadeCard
	}

	/// <summary>
	/// Gets the minimum system type required for a ROM.
	/// </summary>
	/// <param name="rom">ROM data</param>
	/// <param name="isSupergrafxCompatible">SuperGrafx flag</param>
	/// <returns>Minimum required system type</returns>
	public static SystemType GetRequiredSystem(byte[] rom, bool isSupergrafxCompatible = false) {
		if (isSupergrafxCompatible) {
			return SystemType.SuperGrafx;
		}

		// Standard HuCard
		return SystemType.Standard;
	}

	/// <summary>
	/// I/O port addresses for TG16
	/// </summary>
	public static class IoAddresses {
		/// <summary>VDC address/status register ($0000)</summary>
		public const ushort VdcAddressStatus = 0x0000;

		/// <summary>VDC data low ($0002)</summary>
		public const ushort VdcDataLow = 0x0002;

		/// <summary>VDC data high ($0003)</summary>
		public const ushort VdcDataHigh = 0x0003;

		/// <summary>VCE address ($0400)</summary>
		public const ushort VceAddress = 0x0400;

		/// <summary>VCE data ($0402)</summary>
		public const ushort VceData = 0x0402;

		/// <summary>PSG channel select ($0800)</summary>
		public const ushort PsgChannelSelect = 0x0800;

		/// <summary>PSG main volume ($0801)</summary>
		public const ushort PsgMainVolume = 0x0801;

		/// <summary>Timer counter ($0c00)</summary>
		public const ushort TimerCounter = 0x0c00;

		/// <summary>Timer control ($0c01)</summary>
		public const ushort TimerControl = 0x0c01;

		/// <summary>Joypad port ($1000)</summary>
		public const ushort JoypadPort = 0x1000;

		/// <summary>IRQ disable ($1402)</summary>
		public const ushort IrqDisable = 0x1402;

		/// <summary>IRQ status ($1403)</summary>
		public const ushort IrqStatus = 0x1403;
	}
}
