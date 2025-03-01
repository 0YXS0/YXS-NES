using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace Nes.Core;
// PPU Memory Map ==============================
// $0000-$0FFF	$1000	Pattern table 0
// $1000-$1FFF	$1000	Pattern table 1
// $2000-$23FF	$0400	Nametable 0
// $2400-$27FF	$0400	Nametable 1
// $2800-$2BFF	$0400	Nametable 2
// $2C00-$2FFF	$0400	Nametable 3
// $3000-$3EFF	$0F00	Mirrors of $2000-$2EFF
// $3F00-$3F1F	$0020	Palette RAM indexes
// $3F20-$3FFF	$00E0	Mirrors of $3F00-$3F1F
// =============================================

/// <summary>
/// NES上的图像处理单元(PPU)
/// </summary>
public class Ppu(Emulator emulator)
{

    #region Private Fields

    private const int OamDataSize = 256;

    private const int PlateTableSize = 32;

    private const int VramSize = 2048;

    /// <summary>
    /// 背景调色板地址
    /// </summary>
    private static readonly ushort[] _backgroundColorPaletteAddresses =
    [
        0x3f01,
        0x3f05,
        0x3f09,
        0x3f0d
    ];

    /// <summary>
    /// 精灵调色板地址
    /// </summary>
    private static readonly ushort[] _spriteColorPaletteAddresses =
    [
        0x3f11,
        0x3f15,
        0x3f19,
        0x3f1d
    ];

    // 将当前帧的显示数据存储在屏幕上。
    private readonly byte[] _bmp = new byte[256 * 240];

    private readonly byte[] _oam = new byte[OamDataSize];

    private readonly byte[] _paletteTable = new byte[PlateTableSize];

    private readonly byte[] _spriteIndicies = new byte[8];

    private readonly byte[] _sprites = new byte[32];

    private readonly byte[] _vram = new byte[VramSize];

    private byte _attributeTableByte;

    private int _cycles;

    private byte _f;

    private byte _internalDataBuffer;

    private byte _nameTableByte;

    private int _numSprites;

    private byte _oamAddress;

    private PpuAddressRegister _ppuAddress;
    private PpuControlRegister _ppuCtrl;
    private PpuMaskRegister _ppuMask;
    private PpuScrollRegister _ppuScroll;
    private PpuStatusRegister _ppuStatus;
    private int _scanline;

    private ushort _t;

    private byte _tileBitfieldHi;

    private byte _tileBitfieldLo;

    private ulong _tileShiftRegister;

    private byte _w;

    #endregion Private Fields

    #region Internal Properties

    internal VramMirroring Mirroring { get; set; }

    internal PpuAddressRegister PpuAddress => _ppuAddress;

    internal PpuControlRegister PpuControl => _ppuCtrl;

    internal PpuMaskRegister PpuMask => _ppuMask;

    internal PpuScrollRegister PpuScroll => _ppuScroll;

    internal PpuStatusRegister PpuStatus => _ppuStatus;

    #endregion Internal Properties

    #region Public Methods

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte ReadRegister(ushort address)
    {
        return address switch
        {
            0x2000 => _ppuCtrl.Value,
            0x2001 => _ppuMask.Value,
            0x2002 => ReadStatus( ),
            0x2003 => _oamAddress,
            0x2004 => ReadOamData( ),
            0x2005 => _ppuScroll.Value,
            0x2006 => (byte)(_ppuAddress.Value & 0xFF),
            0x2007 => ReadPpuData( ),
            _ => throw new AccessViolationException($"PPU在地址读取的寄存器无效 {address:x8}.")
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteRegister(ushort address, byte data)
    {
        _ppuStatus.LastRegisterWrite = data;
        switch(address)
        {
            case 0x2000:
                WriteControlRegister(data);
                break;

            case 0x2001:
                WriteMaskRegister(data);
                break;

            case 0x2003:
                WriteOamAddress(data);
                break;

            case 0x2004:
                WriteOamData(data);
                break;

            case 0x2005:
                WriteScrollRegister(data);
                break;

            case 0x2006:
                WriteAddressRegister(data);
                break;

            case 0x2007:
                WritePpuData(data);
                break;

            case 0x4014:
                WriteOamDma(data);
                break;

            default:
                throw new AccessViolationException("PPU寄存器写入寄存器无效： " + address.ToString("X4"));
        }
    }

    public void Reset( )
    {
        if(emulator.InstalledCartridge is null)
            throw new InvalidOperationException("未安装卡带。");

        Mirroring = emulator.InstalledCartridge.Mirroring;

        _cycles = 340;
        _scanline = 240;

        _w = 0;
        _f = 0;
        _t = 0;
        _tileBitfieldHi = 0;
        _tileBitfieldLo = 0;
        _tileShiftRegister = 0;

        _ppuAddress.Reset( );
        _ppuCtrl.Reset( );
        _ppuMask.Reset( );
        _ppuScroll.Reset( );
        _ppuStatus.Reset( );

        Array.Clear(_vram, 0, _vram.Length);
        Array.Clear(_paletteTable, 0, _paletteTable.Length);
        Array.Clear(_oam, 0, _oam.Length);
        Array.Clear(_sprites, 0, _sprites.Length);
        Array.Clear(_bmp, 0, _bmp.Length);
    }

    public void Step( )
    {
        if(_scanline == 241 && _cycles == 1)
        {/// 垂直消隐（VBlank）
            _ppuStatus.VBlankStarted = 1;   // 设置VBlank标志
            if(_ppuCtrl.GenerateNmiWhenVBlankBegins)
            {
                emulator.Cpu.TriggerNmiInterrupt( );
            }
        }

        // 判断是否启用渲染
        var renderingEnabled = _ppuMask.ShowBackground != 0 || _ppuMask.ShowSprites != 0;

        if(renderingEnabled)
        {
            if(_scanline == 261 && _f == 1 && _cycles == 339)
            {
                _f ^= 1;
                _scanline = 0;
                _cycles = -1;
                emulator.OnDrawFrame(_bmp);
                return;
            }
        }

        _cycles++;
        if(_cycles > 340)
        {// 一条扫描线结束
            if(_scanline == 261)
            {// 一帧结束
                _f ^= 1;
                _scanline = 0;
                _cycles = -1;
                emulator.OnDrawFrame(_bmp);
            }
            else
            {
                _cycles = -1;
                _scanline++;
            }
        }

        var renderCycle = _cycles is > 0 and <= 256;
        var prefetchCycle = _cycles is >= 321 and <= 336;
        var fetchCycle = renderCycle || prefetchCycle;
        var renderScanline = _scanline is >= 0 and < 240;
        var prerenderScanline = _scanline == 261;
        if(prerenderScanline && _cycles == 1)
        {
            _ppuStatus.VBlankStarted = 0;   // 清除VBlank标志
            _ppuStatus.SpriteOverflow = 0;  // 精灵溢出标志
            _ppuStatus.SpriteZeroHit = 0;   // 精灵0命中标志
        }

        if(!renderingEnabled) return;

        if(_cycles == 257)
        {
            if(renderScanline)
                EvaluateSprites( );
            else
                _numSprites = 0;
        }

        if(renderCycle && renderScanline) RenderPixel( );

        if(fetchCycle && (renderScanline || prerenderScanline))
        {
            _tileShiftRegister >>= 4;
            ushort address;
            switch(_cycles % 8)
            {
                case 1:
                    // FetchNametableByte
                    address = (ushort)(0x2000 | (_ppuAddress.Value & 0x0fff));
                    _nameTableByte = ReadData(address);
                    break;

                case 3:
                    // FetchAttributeTableByte
                    address = (ushort)(0x23c0 | (_ppuAddress.Value & 0x0c00) | ((_ppuAddress.Value >> 4) & 0x38) |
                                       ((_ppuAddress.Value >> 2) & 0x07));
                    _attributeTableByte = ReadData(address);
                    break;

                case 5:
                    // FetchTileBitfieldLo
                    address =
                        (ushort)(_ppuCtrl.BackgroundPatternTableAddress + _nameTableByte * 16 + FineY( ));
                    _tileBitfieldLo = ReadData(address);
                    break;

                case 7:
                    // FetchTileBitfieldHi
                    address = (ushort)(_ppuCtrl.BackgroundPatternTableAddress + _nameTableByte * 16 + FineY( ) + 8);
                    _tileBitfieldHi = ReadData(address);
                    break;

                case 0:
                    StoreTileData( );
                    IncrementX( );
                    if(_cycles == 256) IncrementY( );
                    break;
            }
        }

        if(_cycles > 257 && _cycles <= 320 && (prerenderScanline || renderScanline)) _oamAddress = 0;

        if(_cycles == 257 && (renderScanline || prerenderScanline))
            _ppuAddress.Value = (ushort)((_ppuAddress.Value & 0x7be0) | (_t & 0x041f));

        if(_cycles >= 280 && _cycles <= 304 && _scanline == 261)
            _ppuAddress.Value = (ushort)((_ppuAddress.Value & 0x041f) | (_t & 0x7be0));

        if(_cycles == 260 && renderScanline && _ppuMask.ShowBackground != 0 && _ppuMask.ShowSprites != 0)
            emulator.Mapper.IrqTick( );
    }

    public void Save(BinaryWriter writer)
    {
        writer.Write(_oam);
        writer.Write(_paletteTable);
        writer.Write(_spriteIndicies);
        writer.Write(_sprites);
        writer.Write(_vram);
        writer.Write(_attributeTableByte);
        writer.Write(_cycles);
        writer.Write(_f);
        writer.Write(_internalDataBuffer);
        writer.Write((int)Mirroring);
        writer.Write(_nameTableByte);
        writer.Write(_numSprites);
        writer.Write(_oamAddress);
        _ppuAddress.Save(writer);
        _ppuCtrl.Save(writer);
        _ppuMask.Save(writer);
        _ppuScroll.Save(writer);
        _ppuStatus.Save(writer);
        writer.Write(_t);
        writer.Write(_tileBitfieldHi);
        writer.Write(_tileBitfieldLo);
        writer.Write(_tileShiftRegister);
        writer.Write(_w);
    }

    public void Load(BinaryReader reader)
    {
        reader.Read(_oam);
        reader.Read(_paletteTable);
        reader.Read(_spriteIndicies);
        reader.Read(_sprites);
        reader.Read(_vram);
        _attributeTableByte = reader.ReadByte( );
        _cycles = reader.ReadInt32( );
        _f = reader.ReadByte( );
        _internalDataBuffer = reader.ReadByte( );
        Mirroring = (VramMirroring)reader.ReadInt32( );
        _nameTableByte = reader.ReadByte( );
        _numSprites = reader.ReadInt32( );
        _oamAddress = reader.ReadByte( );
        _ppuAddress.Load(reader);
        _ppuCtrl.Load(reader);
        _ppuMask.Load(reader);
        _ppuScroll.Load(reader);
        _ppuStatus.Load(reader);
        _t = reader.ReadUInt16( );
        _tileBitfieldHi = reader.ReadByte( );
        _tileBitfieldLo = reader.ReadByte( );
        _tileShiftRegister = reader.ReadUInt64( );
        _w = reader.ReadByte( );
    }

    #endregion Public Methods

    #region Private Methods

    private static ushort GetPaletteRamIndex(ushort address)
    {
        var index = (ushort)((address - 0x3f00) % 32);

        // Mirrors $3F10, $3F14, $3F18, $3F1C to $3F00, $3F14, $3F08 $3F0C
        if(index >= 16 && (index - 16) % 4 == 0) return (ushort)(index - 16);

        return index;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int CoarseX( ) => _ppuAddress.Value & 0x1f;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int CoarseY( ) => (_ppuAddress.Value >> 5) & 0x1f;

    private void EvaluateSprites( )
    {
        Array.Clear(_sprites, 0, _sprites.Length);  // 清除精灵数据
        Array.Clear(_spriteIndicies, 0, _spriteIndicies.Length);    // 清除精灵索引

        var h = _ppuCtrl.SpriteSizeFlag == 0 ? 7 : 15;

        _numSprites = 0;
        var y = _scanline;
        for(int i = _oamAddress; i < 256; i += 4)
        {
            var spriteYTop = _oam[i];
            var offset = y - spriteYTop;
            if(offset > h || offset < 0) continue;

            if(_numSprites == 8)
            {
                _ppuStatus.SpriteOverflow = 1;
                break;
            }

            Array.Copy(_oam, i, _sprites, _numSprites * 4, 4);
            _spriteIndicies[_numSprites] = (byte)((i - _oamAddress) / 4);
            _numSprites++;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int FineY( ) => (_ppuAddress.Value >> 12) & 0x7;

    private byte GetBackgroundPixelData( )
    {
        var xPos = _cycles - 1;

        if(_ppuMask.ShowBackground == 0) return 0;
        if(_ppuMask.ShowBackgroundInLeftmost8PixelsOfScreen == 0 && xPos < 8) return 0;

        return (byte)((_tileShiftRegister >> (_ppuScroll.Value * 4)) & 0xf);
    }

    private int GetSpritePatternPixel(ushort patternAddr, int xPos, int yPos, bool flipHoriz = false,
        bool flipVert = false)
    {
        var h = _ppuCtrl.SpriteSizeFlag == 0 ? 7 : 15;

        xPos = flipHoriz ? 7 - xPos : xPos;
        yPos = flipVert ? h - yPos : yPos;

        ushort yAddr;
        if(yPos <= 7) yAddr = (ushort)(patternAddr + yPos);
        else yAddr = (ushort)(patternAddr + 16 + (yPos - 8));

        var pattern = new byte[2];
        pattern[0] = ReadData(yAddr);
        pattern[1] = ReadData((ushort)(yAddr + 8));

        var loBit = (byte)((pattern[0] >> (7 - xPos)) & 1);
        var hiBit = (byte)((pattern[1] >> (7 - xPos)) & 1);

        return ((hiBit << 1) | loBit) & 0x03;
    }

    private byte GetSpritePixelData(out int spriteIndex)
    {
        var xPos = _cycles - 1;
        var yPos = _scanline - 1;

        spriteIndex = 0;

        if(_ppuMask.ShowSprites == 0) return 0;
        if(_ppuMask.ShowSpritesInLeftmost8PixelsOfScreen == 0 && xPos < 8) return 0;

        var currSpritePatternTableAddr = _ppuCtrl.SpritePatternTableAddress;

        for(var i = 0; i < _numSprites * 4; i += 4)
        {
            var spriteXLeft = _sprites[i + 3];
            var offset = xPos - spriteXLeft;

            if(offset >= 0 && offset <= 7)
            {
                var yOffset = yPos - _sprites[i];

                byte patternIndex;

                if(_ppuCtrl.SpriteSizeFlag == 1)
                {
                    currSpritePatternTableAddr = (ushort)((_sprites[i + 1] & 1) * 0x1000);
                    patternIndex = (byte)(_sprites[i + 1] & 0xFE);
                }
                else
                {
                    patternIndex = _sprites[i + 1];
                }

                var patternAddress = (ushort)(currSpritePatternTableAddr + patternIndex * 16);

                var flipHoriz = (_sprites[i + 2] & 0x40) != 0;
                var flipVert = (_sprites[i + 2] & 0x80) != 0;
                var colorNum = GetSpritePatternPixel(patternAddress, offset, yOffset, flipHoriz, flipVert);

                if(colorNum == 0) continue;

                var paletteNum = (byte)(_sprites[i + 2] & 0x03);
                spriteIndex = i / 4;
                return (byte)(((paletteNum << 2) | colorNum) & 0xF);
            }
        }

        return 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void IncrementX( )
    {
        if((_ppuAddress.Value & 0x001f) == 31)
        {
            _ppuAddress.Value = (ushort)(_ppuAddress.Value & ~0x001f);
            _ppuAddress.Value = (ushort)(_ppuAddress.Value ^ 0x0400);
        }
        else
        {
            _ppuAddress.Value++;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void IncrementY( )
    {
        if((_ppuAddress.Value & 0x7000) != 0x7000)
        {
            _ppuAddress.Value += 0x1000;
        }
        else
        {
            _ppuAddress.Value = (ushort)(_ppuAddress.Value & ~0x7000);
            var y = (_ppuAddress.Value & 0x03E0) >> 5;
            switch(y)
            {
                case 29:
                    y = 0;
                    _ppuAddress.Value = (ushort)(_ppuAddress.Value ^ 0x0800);
                    break;

                case 31:
                    y = 0;
                    break;

                default:
                    y += 1;
                    break;
            }

            _ppuAddress.Value = (ushort)((_ppuAddress.Value & ~0x03E0) | (y << 5));
        }
    }

    private byte LookupColor(byte data, IReadOnlyList<ushort> colorPaletteAddresses)
    {
        var colorNum = data & 0x3;
        var paletteNum = (data >> 2) & 0x3;

        if(colorNum == 0) return ReadData(0x3f00);

        var paletteAddress = colorPaletteAddresses[paletteNum];
        paletteAddress += (ushort)(colorNum - 1);
        return ReadData(paletteAddress);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int MirrorVramAddress(ushort address)
    {
        var index = (address - 0x2000) % 0x1000;
        switch(Mirroring)
        {
            case VramMirroring.Vertical:
                if(index >= 0x800) index -= 0x800;
                break;

            case VramMirroring.Horizontal:
                if(index > 0x800) index = (index - 0x800) % 0x400 + 0x400;
                else index %= 0x400;
                break;

            case VramMirroring.SingleLower:
                index %= 0x400;
                break;

            case VramMirroring.SingleUpper:
                index = index % 400 + 0x400;
                break;

            case VramMirroring.Unknown:
            default:
                throw new ArgumentOutOfRangeException(nameof(address), Mirroring, "VRAM镜像类型无效。");
        }

        return index;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private byte ReadData(ushort address)
    {
        address %= 0x4000;
        switch(address)
        {
            case < 0x2000:
                return emulator.Mapper.ReadByte(address);
            case >= 0x2000 and <= 0x3eff:
                if(address >= 0x3000) address -= 0x1000;
                return _vram[MirrorVramAddress(address)];
            case >= 0x3f00 and <= 0x3fff:
                if(address >= 0x3F20) address = (ushort)(address % 0x20 + 0x3F00);
                return _paletteTable[GetPaletteRamIndex(address)];
            default:
                throw new AccessViolationException($"无效的PPU地址读取:{address: x4}");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private byte ReadOamData( ) => _oam[_oamAddress];

    private byte ReadPpuData( )
    {
        var data = ReadData(_ppuAddress.Value);
        if(_ppuAddress.Value < 0x3f00)
        {
            // ReSharper disable once SwapViaDeconstruction
            (data, _internalDataBuffer) = (_internalDataBuffer, data);
        }
        else
        {
            _internalDataBuffer = ReadData((ushort)(_ppuAddress.Value - 0x1000));
        }

        _ppuAddress.Value += (ushort)_ppuCtrl.VramAddressIncrement;

        return data;
    }

    private byte ReadStatus( )
    {
        var retVal = _ppuStatus.Value;

        // Clear VBlankStarted flag.
        _ppuStatus.VBlankStarted = 0;

        _w = 0;

        return retVal;
    }

    private void RenderPixel( )
    {
        var bgPixelData = GetBackgroundPixelData( );

        var spritePixelData = GetSpritePixelData(out var spriteScanlineIndex);
        var isSpriteZero = _spriteIndicies[spriteScanlineIndex] == 0;

        var bgColorNum = bgPixelData & 0x03;
        var spriteColorNum = spritePixelData & 0x03;

        byte color;

        if(bgColorNum == 0)
        {
            color = spriteColorNum == 0
                ? LookupColor(bgPixelData, _backgroundColorPaletteAddresses)
                : LookupColor(spritePixelData, _spriteColorPaletteAddresses);
        }
        else
        {
            if(spriteColorNum == 0)
            {
                color = LookupColor(bgPixelData, _backgroundColorPaletteAddresses);
            }
            else
            {
                if(isSpriteZero) _ppuStatus.SpriteZeroHit = 1;

                var priority = (_sprites[spriteScanlineIndex * 4 + 2] >> 5) & 1;
                color = priority == 1
                    ? LookupColor(bgPixelData, _backgroundColorPaletteAddresses)
                    : LookupColor(spritePixelData, _spriteColorPaletteAddresses);
            }
        }

        _bmp[_scanline * 256 + (_cycles - 1)] = color;
    }

    private void StoreTileData( )
    {
        var palette = (byte)((_attributeTableByte >> ((CoarseX( ) & 0x2) | ((CoarseY( ) & 0x2) << 1))) & 0x3);

        ulong data = 0;

        for(var i = 0; i < 8; i++)
        {
            var loColorBit = (byte)((_tileBitfieldLo >> (7 - i)) & 1);
            var hiColorBit = (byte)((_tileBitfieldHi >> (7 - i)) & 1);
            var colorNum = (byte)((hiColorBit << 1) | (loColorBit & 0x03));

            var fullPixelData = (byte)(((palette << 2) | colorNum) & 0xF);

            data |= (uint)(fullPixelData << (4 * i));
        }

        _tileShiftRegister &= 0xffffffff;
        _tileShiftRegister |= data << 32;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteAddressRegister(byte data) => _ppuAddress.Set(data, ref _w, ref _t);

    private void WriteControlRegister(byte data)
    {
        _ppuCtrl.Set(data);
        _t = (ushort)((_t & 0xf3ff) | ((data & 0x03) << 10));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteData(ushort address, byte data)
    {
        address %= 0x4000;
        switch(address)
        {
            case < 0x2000:
                emulator.Mapper.WriteByte(address, data);
                break;

            case >= 0x2000 and <= 0x3eff:
                if(address >= 0x3000) address -= 0x1000;
                _vram[MirrorVramAddress(address)] = data;
                break;

            case >= 0x3f00 and <= 0x3fff:
                if(address >= 0x3F20) address = (ushort)(address % 0x20 + 0x3F00);
                _paletteTable[GetPaletteRamIndex(address)] = data;
                break;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteMaskRegister(byte data) => _ppuMask.Set(data);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteOamAddress(byte data) => _oamAddress = data;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteOamData(byte data)
    {
        _oam[_oamAddress] = data;
        _oamAddress = (_oamAddress + 1).WrapToByte( );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteOamDma(byte data)
    {
        var startAddr = (ushort)(data << 8);
        emulator.Bus.DirectMemoryRead(_oam, _oamAddress, startAddr, 256);

        emulator.Cpu.AddIdleCycles(513);

        if(emulator.Cpu.Cycles % 2 == 1) emulator.Cpu.AddIdleCycles(1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WritePpuData(byte data)
    {
        WriteData(_ppuAddress.Value, data);
        _ppuAddress.Value += (ushort)_ppuCtrl.VramAddressIncrement;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteScrollRegister(byte data)
    {
        _ppuScroll.Set(data, ref _w, ref _t);
    }

    #endregion Private Methods

}