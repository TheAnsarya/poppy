// SpcFileBuilder.cs
// SPC file format builder for SNES audio
// Generates valid .spc files containing SPC700 code and DSP state

using System;
using System.Text;

namespace Poppy.Core.CodeGen;

/// <summary>
/// Builder for generating SPC files (SNES audio format).
/// SPC files contain the full state of the SPC700 audio processor.
/// Format specification: https://wiki.superfamicom.org/spc-file-format
/// </summary>
public class SpcFileBuilder {
	// SPC file constants
	private const int HeaderSize = 256;         // $100 bytes
	private const int RamSize = 65536;          // $10000 bytes (64KB)
	private const int DspRegistersSize = 128;   // $80 bytes
	private const int ExtraRamSize = 64;        // $40 bytes (IPL ROM area when mapped out)
	private const int TotalFileSize = HeaderSize + RamSize + DspRegistersSize + ExtraRamSize;

	// Header offsets
	private const int SignatureOffset = 0x00;    // "SNES-SPC700 Sound File Data v0.30"
	private const int HasIdTagOffset = 0x23;     // $1a if has ID666 tag, $1b if not
	private const int VersionMinorOffset = 0x24; // Version minor (30)
	private const int PcOffset = 0x25;           // PC register (2 bytes)
	private const int AOffset = 0x27;            // A register
	private const int XOffset = 0x28;            // X register
	private const int YOffset = 0x29;            // Y register
	private const int PswOffset = 0x2a;          // PSW (processor status word)
	private const int SpOffset = 0x2b;           // SP register
	private const int Reserved1Offset = 0x2c;   // 2 bytes reserved

	// ID666 tag offsets (when present)
	private const int SongTitleOffset = 0x2e;   // 32 bytes
	private const int GameTitleOffset = 0x4e;   // 32 bytes
	private const int DumperOffset = 0x6e;      // 16 bytes
	private const int CommentsOffset = 0x7e;    // 32 bytes
	private const int DateOffset = 0x9e;        // 11 bytes (MM/DD/YYYY)
	private const int FadeOutOffset = 0xa9;     // 3 bytes (seconds)
	private const int FadeLengthOffset = 0xac;  // 4 bytes (milliseconds)
	private const int ArtistOffset = 0xb0;      // 32 bytes
	private const int DefaultChannelOffset = 0xd0; // 1 byte
	private const int EmulatorOffset = 0xd1;    // 1 byte

	// Data section offsets
	private const int RamOffset = 0x100;        // 64KB RAM
	private const int DspOffset = 0x10100;      // 128 DSP registers
	private const int ExtraRamOffset = 0x10180; // 64 bytes extra RAM

	// Signature string
	private static readonly byte[] Signature = Encoding.ASCII.GetBytes("SNES-SPC700 Sound File Data v0.30");

	// Builder state - Registers
	private ushort _pc = 0x0200;     // Program counter
	private byte _a = 0x00;          // Accumulator
	private byte _x = 0x00;          // X register
	private byte _y = 0x00;          // Y register
	private byte _psw = 0x00;        // Processor status word
	private byte _sp = 0xef;         // Stack pointer

	// Builder state - ID666 tag
	private bool _hasId666Tag = true;
	private string _songTitle = "";
	private string _gameTitle = "";
	private string _dumperName = "";
	private string _comments = "";
	private string _dumpDate = "";
	private int _fadeOutSeconds = 0;
	private int _fadeLengthMs = 0;
	private string _artistName = "";
	private byte _defaultChannel = 0;
	private byte _emulatorUsed = 0;

	// Builder state - Data
	private byte[] _ram = new byte[RamSize];
	private byte[] _dspRegisters = new byte[DspRegistersSize];
	private byte[] _extraRam = new byte[ExtraRamSize];

	/// <summary>
	/// Sets the program counter (PC) register.
	/// </summary>
	/// <param name="pc">16-bit program counter</param>
	/// <returns>This builder for chaining</returns>
	public SpcFileBuilder SetPC(ushort pc) {
		_pc = pc;
		return this;
	}

	/// <summary>
	/// Sets the accumulator (A) register.
	/// </summary>
	/// <param name="a">8-bit accumulator value</param>
	/// <returns>This builder for chaining</returns>
	public SpcFileBuilder SetA(byte a) {
		_a = a;
		return this;
	}

	/// <summary>
	/// Sets the X register.
	/// </summary>
	/// <param name="x">8-bit X register value</param>
	/// <returns>This builder for chaining</returns>
	public SpcFileBuilder SetX(byte x) {
		_x = x;
		return this;
	}

	/// <summary>
	/// Sets the Y register.
	/// </summary>
	/// <param name="y">8-bit Y register value</param>
	/// <returns>This builder for chaining</returns>
	public SpcFileBuilder SetY(byte y) {
		_y = y;
		return this;
	}

	/// <summary>
	/// Sets the processor status word (PSW).
	/// </summary>
	/// <param name="psw">8-bit PSW value</param>
	/// <returns>This builder for chaining</returns>
	public SpcFileBuilder SetPSW(byte psw) {
		_psw = psw;
		return this;
	}

	/// <summary>
	/// Sets the stack pointer (SP).
	/// </summary>
	/// <param name="sp">8-bit stack pointer</param>
	/// <returns>This builder for chaining</returns>
	public SpcFileBuilder SetSP(byte sp) {
		_sp = sp;
		return this;
	}

	/// <summary>
	/// Sets all registers at once.
	/// </summary>
	/// <param name="pc">Program counter</param>
	/// <param name="a">Accumulator</param>
	/// <param name="x">X register</param>
	/// <param name="y">Y register</param>
	/// <param name="psw">Processor status word</param>
	/// <param name="sp">Stack pointer</param>
	/// <returns>This builder for chaining</returns>
	public SpcFileBuilder SetRegisters(ushort pc, byte a, byte x, byte y, byte psw, byte sp) {
		_pc = pc;
		_a = a;
		_x = x;
		_y = y;
		_psw = psw;
		_sp = sp;
		return this;
	}

	/// <summary>
	/// Sets the song title for ID666 tag.
	/// </summary>
	/// <param name="title">Song title (max 32 characters)</param>
	/// <returns>This builder for chaining</returns>
	public SpcFileBuilder SetSongTitle(string title) {
		_songTitle = title ?? "";
		return this;
	}

	/// <summary>
	/// Sets the game title for ID666 tag.
	/// </summary>
	/// <param name="title">Game title (max 32 characters)</param>
	/// <returns>This builder for chaining</returns>
	public SpcFileBuilder SetGameTitle(string title) {
		_gameTitle = title ?? "";
		return this;
	}

	/// <summary>
	/// Sets the dumper name for ID666 tag.
	/// </summary>
	/// <param name="name">Dumper name (max 16 characters)</param>
	/// <returns>This builder for chaining</returns>
	public SpcFileBuilder SetDumperName(string name) {
		_dumperName = name ?? "";
		return this;
	}

	/// <summary>
	/// Sets the comments for ID666 tag.
	/// </summary>
	/// <param name="comments">Comments (max 32 characters)</param>
	/// <returns>This builder for chaining</returns>
	public SpcFileBuilder SetComments(string comments) {
		_comments = comments ?? "";
		return this;
	}

	/// <summary>
	/// Sets the dump date for ID666 tag.
	/// </summary>
	/// <param name="date">Date string (MM/DD/YYYY format, max 11 characters)</param>
	/// <returns>This builder for chaining</returns>
	public SpcFileBuilder SetDumpDate(string date) {
		_dumpDate = date ?? "";
		return this;
	}

	/// <summary>
	/// Sets the fade out time for ID666 tag.
	/// </summary>
	/// <param name="seconds">Fade out start time in seconds</param>
	/// <param name="fadeMs">Fade length in milliseconds</param>
	/// <returns>This builder for chaining</returns>
	public SpcFileBuilder SetFadeOut(int seconds, int fadeMs) {
		_fadeOutSeconds = seconds;
		_fadeLengthMs = fadeMs;
		return this;
	}

	/// <summary>
	/// Sets the artist name for ID666 tag.
	/// </summary>
	/// <param name="name">Artist name (max 32 characters)</param>
	/// <returns>This builder for chaining</returns>
	public SpcFileBuilder SetArtistName(string name) {
		_artistName = name ?? "";
		return this;
	}

	/// <summary>
	/// Sets whether to include ID666 tag.
	/// </summary>
	/// <param name="include">True to include tag</param>
	/// <returns>This builder for chaining</returns>
	public SpcFileBuilder SetIncludeId666Tag(bool include) {
		_hasId666Tag = include;
		return this;
	}

	/// <summary>
	/// Sets the emulator used code for ID666 tag.
	/// </summary>
	/// <param name="emulator">Emulator code (0=unknown, 1=ZSNES, 2=Snes9x)</param>
	/// <returns>This builder for chaining</returns>
	public SpcFileBuilder SetEmulatorUsed(byte emulator) {
		_emulatorUsed = emulator;
		return this;
	}

	/// <summary>
	/// Sets the RAM contents.
	/// </summary>
	/// <param name="data">RAM data (up to 64KB)</param>
	/// <returns>This builder for chaining</returns>
	public SpcFileBuilder SetRam(byte[] data) {
		if (data == null) {
			_ram = new byte[RamSize];
			return this;
		}

		var copyLength = Math.Min(data.Length, RamSize);
		Array.Copy(data, 0, _ram, 0, copyLength);
		return this;
	}

	/// <summary>
	/// Sets RAM data at a specific address.
	/// </summary>
	/// <param name="address">Starting address (0-65535)</param>
	/// <param name="data">Data to write</param>
	/// <returns>This builder for chaining</returns>
	public SpcFileBuilder SetRamAt(ushort address, byte[] data) {
		if (data == null || data.Length == 0) {
			return this;
		}

		var copyLength = Math.Min(data.Length, RamSize - address);
		Array.Copy(data, 0, _ram, address, copyLength);
		return this;
	}

	/// <summary>
	/// Sets a single byte in RAM.
	/// </summary>
	/// <param name="address">Address (0-65535)</param>
	/// <param name="value">Value to write</param>
	/// <returns>This builder for chaining</returns>
	public SpcFileBuilder SetRamByte(ushort address, byte value) {
		_ram[address] = value;
		return this;
	}

	/// <summary>
	/// Sets the DSP register values.
	/// </summary>
	/// <param name="registers">DSP register values (128 bytes)</param>
	/// <returns>This builder for chaining</returns>
	public SpcFileBuilder SetDspRegisters(byte[] registers) {
		if (registers == null) {
			_dspRegisters = new byte[DspRegistersSize];
			return this;
		}

		var copyLength = Math.Min(registers.Length, DspRegistersSize);
		Array.Copy(registers, 0, _dspRegisters, 0, copyLength);
		return this;
	}

	/// <summary>
	/// Sets a single DSP register.
	/// </summary>
	/// <param name="register">Register address (0-127)</param>
	/// <param name="value">Value to write</param>
	/// <returns>This builder for chaining</returns>
	public SpcFileBuilder SetDspRegister(byte register, byte value) {
		if (register >= DspRegistersSize) {
			throw new ArgumentOutOfRangeException(nameof(register));
		}
		_dspRegisters[register] = value;
		return this;
	}

	/// <summary>
	/// Sets the extra RAM (IPL ROM overlay area).
	/// </summary>
	/// <param name="data">Extra RAM data (64 bytes)</param>
	/// <returns>This builder for chaining</returns>
	public SpcFileBuilder SetExtraRam(byte[] data) {
		if (data == null) {
			_extraRam = new byte[ExtraRamSize];
			return this;
		}

		var copyLength = Math.Min(data.Length, ExtraRamSize);
		Array.Copy(data, 0, _extraRam, 0, copyLength);
		return this;
	}

	/// <summary>
	/// Builds the complete SPC file.
	/// </summary>
	/// <returns>Complete SPC file data</returns>
	public byte[] Build() {
		var spc = new byte[TotalFileSize];

		// Write header signature
		Array.Copy(Signature, 0, spc, SignatureOffset, Signature.Length);

		// Write two separator bytes (26 26)
		spc[0x21] = 0x26;
		spc[0x22] = 0x26;

		// Write ID666 tag indicator
		spc[HasIdTagOffset] = _hasId666Tag ? (byte)0x1a : (byte)0x1b;

		// Write version minor (30)
		spc[VersionMinorOffset] = 30;

		// Write registers
		spc[PcOffset] = (byte)(_pc & 0xff);
		spc[PcOffset + 1] = (byte)((_pc >> 8) & 0xff);
		spc[AOffset] = _a;
		spc[XOffset] = _x;
		spc[YOffset] = _y;
		spc[PswOffset] = _psw;
		spc[SpOffset] = _sp;

		// Write ID666 tag if enabled
		if (_hasId666Tag) {
			WriteString(spc, SongTitleOffset, _songTitle, 32);
			WriteString(spc, GameTitleOffset, _gameTitle, 32);
			WriteString(spc, DumperOffset, _dumperName, 16);
			WriteString(spc, CommentsOffset, _comments, 32);
			WriteString(spc, DateOffset, _dumpDate, 11);

			// Fade out (3-digit ASCII seconds)
			var fadeStr = _fadeOutSeconds.ToString().PadLeft(3, '0');
			if (fadeStr.Length > 3) fadeStr = fadeStr[..3];
			WriteString(spc, FadeOutOffset, fadeStr, 3);

			// Fade length (5-digit ASCII milliseconds)
			var fadeLenStr = _fadeLengthMs.ToString().PadLeft(5, '0');
			if (fadeLenStr.Length > 5) fadeLenStr = fadeLenStr[..5];
			WriteString(spc, FadeLengthOffset, fadeLenStr, 5);

			WriteString(spc, ArtistOffset, _artistName, 32);
			spc[DefaultChannelOffset] = _defaultChannel;
			spc[EmulatorOffset] = _emulatorUsed;
		}

		// Write RAM
		Array.Copy(_ram, 0, spc, RamOffset, RamSize);

		// Write DSP registers
		Array.Copy(_dspRegisters, 0, spc, DspOffset, DspRegistersSize);

		// Write extra RAM
		Array.Copy(_extraRam, 0, spc, ExtraRamOffset, ExtraRamSize);

		return spc;
	}

	/// <summary>
	/// Writes a string to the SPC file at the specified offset.
	/// </summary>
	private static void WriteString(byte[] spc, int offset, string value, int maxLength) {
		var bytes = Encoding.ASCII.GetBytes(value ?? "");
		var copyLength = Math.Min(bytes.Length, maxLength);
		Array.Copy(bytes, 0, spc, offset, copyLength);
		// Pad with zeros
		for (int i = copyLength; i < maxLength; i++) {
			spc[offset + i] = 0;
		}
	}

	/// <summary>
	/// Validates an SPC file.
	/// </summary>
	/// <param name="spc">SPC file data</param>
	/// <returns>True if valid SPC file</returns>
	public static bool ValidateFile(byte[] spc) {
		if (spc == null || spc.Length < TotalFileSize) {
			return false;
		}

		// Check signature
		for (int i = 0; i < Signature.Length; i++) {
			if (spc[i] != Signature[i]) {
				return false;
			}
		}

		return true;
	}

	/// <summary>
	/// Extracts register values from an SPC file.
	/// </summary>
	/// <param name="spc">SPC file data</param>
	/// <returns>Tuple of (PC, A, X, Y, PSW, SP)</returns>
	public static (ushort pc, byte a, byte x, byte y, byte psw, byte sp) GetRegisters(byte[] spc) {
		if (spc == null || spc.Length < HeaderSize) {
			throw new ArgumentException("Invalid SPC file");
		}

		ushort pc = (ushort)(spc[PcOffset] | (spc[PcOffset + 1] << 8));
		return (pc, spc[AOffset], spc[XOffset], spc[YOffset], spc[PswOffset], spc[SpOffset]);
	}

	/// <summary>
	/// Extracts the RAM from an SPC file.
	/// </summary>
	/// <param name="spc">SPC file data</param>
	/// <returns>64KB RAM array</returns>
	public static byte[] GetRam(byte[] spc) {
		if (spc == null || spc.Length < RamOffset + RamSize) {
			throw new ArgumentException("Invalid SPC file");
		}

		var ram = new byte[RamSize];
		Array.Copy(spc, RamOffset, ram, 0, RamSize);
		return ram;
	}

	/// <summary>
	/// Extracts the DSP registers from an SPC file.
	/// </summary>
	/// <param name="spc">SPC file data</param>
	/// <returns>128-byte DSP register array</returns>
	public static byte[] GetDspRegisters(byte[] spc) {
		if (spc == null || spc.Length < DspOffset + DspRegistersSize) {
			throw new ArgumentException("Invalid SPC file");
		}

		var dsp = new byte[DspRegistersSize];
		Array.Copy(spc, DspOffset, dsp, 0, DspRegistersSize);
		return dsp;
	}

	/// <summary>
	/// Extracts the song title from an SPC file.
	/// </summary>
	/// <param name="spc">SPC file data</param>
	/// <returns>Song title string</returns>
	public static string GetSongTitle(byte[] spc) {
		if (spc == null || spc.Length < SongTitleOffset + 32) {
			return "";
		}

		// Check if ID666 tag is present
		if (spc[HasIdTagOffset] != 0x1a) {
			return "";
		}

		return Encoding.ASCII.GetString(spc, SongTitleOffset, 32).TrimEnd('\0');
	}

	/// <summary>
	/// Extracts the game title from an SPC file.
	/// </summary>
	/// <param name="spc">SPC file data</param>
	/// <returns>Game title string</returns>
	public static string GetGameTitle(byte[] spc) {
		if (spc == null || spc.Length < GameTitleOffset + 32) {
			return "";
		}

		if (spc[HasIdTagOffset] != 0x1a) {
			return "";
		}

		return Encoding.ASCII.GetString(spc, GameTitleOffset, 32).TrimEnd('\0');
	}

	/// <summary>
	/// Known emulator codes for ID666 tag.
	/// </summary>
	public static class EmulatorCodes {
		/// <summary>Unknown emulator</summary>
		public const byte Unknown = 0;
		/// <summary>ZSNES</summary>
		public const byte ZSNES = 1;
		/// <summary>Snes9x</summary>
		public const byte Snes9x = 2;
	}

	/// <summary>
	/// Creates a minimal SPC file with basic initialization.
	/// </summary>
	/// <param name="pc">Starting program counter</param>
	/// <param name="program">Program code to load</param>
	/// <returns>Complete SPC file</returns>
	public static byte[] CreateMinimal(ushort pc, byte[] program) {
		var builder = new SpcFileBuilder();
		builder.SetPC(pc);
		builder.SetSP(0xef);  // Stack at $01ef (stack grows down)
		builder.SetPSW(0x00);

		// Initialize DSP to silence
		builder.SetDspRegister(InstructionSetSPC700.DspRegisters.FLG, 0xe0);  // Soft reset, mute
		builder.SetDspRegister(InstructionSetSPC700.DspRegisters.MVOLL, 0x00);
		builder.SetDspRegister(InstructionSetSPC700.DspRegisters.MVOLR, 0x00);

		// Load program into RAM
		if (program != null && program.Length > 0) {
			builder.SetRamAt(pc, program);
		}

		return builder.Build();
	}
}
