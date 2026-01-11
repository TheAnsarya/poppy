using System;
using System.Collections.Generic;
using System.IO;

namespace Poppy.Core.CodeGen;

/// <summary>
/// Builds iNES 1.0 and iNES 2.0 headers for NES ROMs
/// </summary>
public class INesHeaderBuilder
{
	private int _prgRomSize;		// in 16KB units
	private int _chrRomSize;		// in 8KB units
	private int _mapper;			// mapper number (0-4095)
	private int _submapper;			// submapper number (0-15, iNES 2.0 only)
	private bool _verticalMirroring;
	private bool _batteryBacked;
	private bool _trainer;
	private bool _fourScreen;
	private int _prgRamSize;		// in 8KB units (iNES 2.0: actual size)
	private int _chrRamSize;		// in 8KB units (iNES 2.0: actual size)
	private bool _pal;				// false = NTSC, true = PAL
	private bool _ines2;			// true for iNES 2.0 format

	/// <summary>
	/// Create a new iNES header builder with default values
	/// </summary>
	public INesHeaderBuilder()
	{
		_prgRomSize = 2;			// default to 32KB PRG ROM
		_chrRomSize = 1;			// default to 8KB CHR ROM
		_mapper = 0;				// default to NROM
		_submapper = 0;
		_verticalMirroring = true;
		_batteryBacked = false;
		_trainer = false;
		_fourScreen = false;
		_prgRamSize = 1;			// default to 8KB PRG RAM
		_chrRamSize = 0;			// default to no CHR RAM
		_pal = false;				// default to NTSC
		_ines2 = true;				// default to iNES 2.0
	}

	/// <summary>
	/// Set PRG ROM size (in 16KB units for iNES 1.0, or actual bytes for iNES 2.0)
	/// </summary>
	public INesHeaderBuilder SetPrgRomSize(int size)
	{
		_prgRomSize = size;
		return this;
	}

	/// <summary>
	/// Set CHR ROM size (in 8KB units for iNES 1.0, or actual bytes for iNES 2.0)
	/// </summary>
	public INesHeaderBuilder SetChrRomSize(int size)
	{
		_chrRomSize = size;
		return this;
	}

	/// <summary>
	/// Set mapper number (0-4095 for iNES 2.0, 0-255 for iNES 1.0)
	/// </summary>
	public INesHeaderBuilder SetMapper(int mapper)
	{
		_mapper = mapper;
		return this;
	}

	/// <summary>
	/// Set submapper number (0-15, iNES 2.0 only)
	/// </summary>
	public INesHeaderBuilder SetSubmapper(int submapper)
	{
		_submapper = submapper;
		return this;
	}

	/// <summary>
	/// Set mirroring mode
	/// </summary>
	public INesHeaderBuilder SetMirroring(bool vertical)
	{
		_verticalMirroring = vertical;
		return this;
	}

	/// <summary>
	/// Set four-screen VRAM mode
	/// </summary>
	public INesHeaderBuilder SetFourScreen(bool fourScreen)
	{
		_fourScreen = fourScreen;
		return this;
	}

	/// <summary>
	/// Set battery-backed save RAM
	/// </summary>
	public INesHeaderBuilder SetBatteryBacked(bool battery)
	{
		_batteryBacked = battery;
		return this;
	}

	/// <summary>
	/// Set whether ROM has 512-byte trainer
	/// </summary>
	public INesHeaderBuilder SetTrainer(bool trainer)
	{
		_trainer = trainer;
		return this;
	}

	/// <summary>
	/// Set PRG RAM size (in 8KB units for iNES 1.0, or actual bytes for iNES 2.0)
	/// </summary>
	public INesHeaderBuilder SetPrgRamSize(int size)
	{
		_prgRamSize = size;
		return this;
	}

	/// <summary>
	/// Set CHR RAM size (in 8KB units for iNES 1.0, or actual bytes for iNES 2.0)
	/// </summary>
	public INesHeaderBuilder SetChrRamSize(int size)
	{
		_chrRamSize = size;
		return this;
	}

	/// <summary>
	/// Set TV system (false = NTSC, true = PAL)
	/// </summary>
	public INesHeaderBuilder SetPal(bool pal)
	{
		_pal = pal;
		return this;
	}

	/// <summary>
	/// Set whether to use iNES 2.0 format (default: true)
	/// </summary>
	public INesHeaderBuilder SetINes2(bool ines2)
	{
		_ines2 = ines2;
		return this;
	}

	/// <summary>
	/// Build the 16-byte iNES header
	/// </summary>
	public byte[] Build()
	{
		var header = new byte[16];

		// bytes 0-3: "NES" followed by MS-DOS EOF
		header[0] = 0x4e;	// 'N'
		header[1] = 0x45;	// 'E'
		header[2] = 0x53;	// 'S'
		header[3] = 0x1a;	// MS-DOS EOF

		if (_ines2)
		{
			BuildINes2Header(header);
		}
		else
		{
			BuildINes1Header(header);
		}

		return header;
	}

	/// <summary>
	/// Build iNES 1.0 header
	/// </summary>
	private void BuildINes1Header(byte[] header)
	{
		// byte 4: PRG ROM size in 16KB units
		header[4] = (byte)_prgRomSize;

		// byte 5: CHR ROM size in 8KB units
		header[5] = (byte)_chrRomSize;

		// byte 6: flags 6
		//   0: mirroring (0=horizontal, 1=vertical)
		//   1: battery-backed PRG RAM
		//   2: 512-byte trainer
		//   3: four-screen VRAM
		//   4-7: lower nybble of mapper number
		byte flags6 = 0;
		if (_verticalMirroring) flags6 |= 0x01;
		if (_batteryBacked) flags6 |= 0x02;
		if (_trainer) flags6 |= 0x04;
		if (_fourScreen) flags6 |= 0x08;
		flags6 |= (byte)((_mapper & 0x0f) << 4);
		header[6] = flags6;

		// byte 7: flags 7
		//   0: VS Unisystem
		//   1: PlayChoice-10
		//   2-3: if equal to 2, flags 8-15 are in NES 2.0 format
		//   4-7: upper nybble of mapper number
		byte flags7 = (byte)((_mapper & 0xf0));
		header[7] = flags7;

		// byte 8: PRG RAM size in 8KB units (0 = infer 8KB for compatibility)
		header[8] = (byte)_prgRamSize;

		// byte 9: TV system (0=NTSC, 1=PAL)
		header[9] = _pal ? (byte)1 : (byte)0;

		// bytes 10-15: unused (should be zero)
		for (int i = 10; i < 16; i++)
		{
			header[i] = 0;
		}
	}

	/// <summary>
	/// Build iNES 2.0 header
	/// </summary>
	private void BuildINes2Header(byte[] header)
	{
		// byte 4: LSB of PRG ROM size
		header[4] = (byte)(_prgRomSize & 0xff);

		// byte 5: LSB of CHR ROM size
		header[5] = (byte)(_chrRomSize & 0xff);

		// byte 6: flags 6 (same as iNES 1.0)
		byte flags6 = 0;
		if (_verticalMirroring) flags6 |= 0x01;
		if (_batteryBacked) flags6 |= 0x02;
		if (_trainer) flags6 |= 0x04;
		if (_fourScreen) flags6 |= 0x08;
		flags6 |= (byte)((_mapper & 0x0f) << 4);
		header[6] = flags6;

		// byte 7: flags 7
		//   0: VS Unisystem
		//   1: PlayChoice-10
		//   2-3: NES 2.0 identifier (must be 2)
		//   4-7: upper nybble of mapper number
		byte flags7 = (byte)((_mapper & 0xf0));
		flags7 |= 0x08;		// set bits 2-3 to 2 (iNES 2.0 identifier)
		header[7] = flags7;

		// byte 8: mapper variant and high bits
		//   0-3: submapper number
		//   4-7: bits 8-11 of mapper number
		byte mapper8 = (byte)(_submapper & 0x0f);
		mapper8 |= (byte)((_mapper & 0xf00) >> 4);
		header[8] = mapper8;

		// byte 9: upper bits of PRG ROM and CHR ROM sizes
		//   0-3: MSB of CHR ROM size
		//   4-7: MSB of PRG ROM size
		byte romSizes9 = (byte)((_chrRomSize & 0xf00) >> 8);
		romSizes9 |= (byte)((_prgRomSize & 0xf00) >> 4);
		header[9] = romSizes9;

		// byte 10: PRG RAM and EEPROM sizes (shift count)
		// For now, we'll use simple size encoding
		header[10] = (byte)_prgRamSize;

		// byte 11: CHR RAM and EEPROM sizes (shift count)
		header[11] = (byte)_chrRamSize;

		// byte 12: CPU/PPU timing
		//   0-1: CPU/PPU timing mode (0=NTSC, 1=PAL, 2=multi-region, 3=Dendy)
		//   2-7: reserved (should be 0)
		header[12] = _pal ? (byte)1 : (byte)0;

		// byte 13: VS system or extended console type
		header[13] = 0;

		// byte 14: miscellaneous ROMs present
		header[14] = 0;

		// byte 15: default expansion device
		header[15] = 0;
	}

	/// <summary>
	/// Write the header to a binary writer
	/// </summary>
	public void WriteTo(BinaryWriter writer)
	{
		var header = Build();
		writer.Write(header);
	}
}
