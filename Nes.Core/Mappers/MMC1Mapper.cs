using System.IO;

namespace Nes.Core.Mappers;

[Mapper(1, "MMC1")]
internal sealed class MMC1Mapper : Mapper
{
    // 更新PRG-ROMbank偏移
    private const int m_PrgBankSize = 0x4000; // 16KB
    // 更新CHR-ROMbank偏移
    private const int m_ChrBankSize = 0x1000; // 4KB

    private readonly byte[] m_ChrRom;
    private readonly int m_ChrRomSize;
    private readonly int m_ChrBankNum;
    private readonly byte[] m_PrgRom;
    private readonly int m_PrgRomSize;
    private readonly int m_PrgBankNum;
    private readonly byte[] m_PrgRam = new byte[0x2000];

    private byte m_ShiftCount = 0; // 移位计数器
    private byte m_ShiftRegister = 0x10; // 初始值为0x10，表示移位寄存器为空

    // 控制寄存器和其他目标寄存器
    private byte m_ControlRegister = 0x0C; // 默认值，PRG-ROM模式为3，固定最后一个bank
    private byte m_ChrBank0 = 0x00;
    private byte m_ChrBank1 = 0x00;
    private byte m_PrgBank = 0x00;

    // bank偏移
    private readonly int[] m_PrgBankOffsets = new int[2];
    private readonly int[] m_ChrBankOffsets = new int[2];

    public MMC1Mapper(Emulator emulator) : base(emulator)
    {
        m_ChrRom = emulator.InstalledCartridge!.ChrData;
        m_ChrRomSize = m_ChrRom.Length;
        m_ChrBankNum = m_ChrRomSize / m_ChrBankSize;
        m_PrgRom = emulator.InstalledCartridge!.PrgRom;
        m_PrgRomSize = m_PrgRom.Length;
        m_PrgBankNum = m_PrgRomSize / m_PrgBankSize;

        // 初始化PRG-ROM和CHR-ROM的bank偏移
        UpdateBankOffsets( );
    }

    public override byte ReadByte(ushort address)
    {
        return address switch
        {
            < 0x1000 => m_ChrRom[m_ChrBankOffsets[0] + address],
            < 0x2000 => m_ChrRom[m_ChrBankOffsets[1] + address - 0x1000],
            < 0x6000 => 0,
            < 0x8000 => m_PrgRam[address - 0x6000],
            < 0xC000 => m_PrgRom[m_PrgBankOffsets[0] + (address - 0x8000)],
            <= 0xFFFF => m_PrgRom[m_PrgBankOffsets[1] + (address - 0xC000)],
        };
    }

    public override void WriteByte(ushort address, byte value)
    {
        switch(address)
        {
            case < 0x1000:
                m_ChrRom[m_ChrBankOffsets[0] + address] = value;
                break;
            case < 0x2000:
                m_ChrRom[m_ChrBankOffsets[1] + address - 0x1000] = value;
                break;
            case < 0x6000:
                break;
            case < 0x8000:
                m_PrgRam[address - 0x6000] = value;
                break;
            default:
                if((value & 0x80) != 0)
                {
                    // 重置移位寄存器，设置为0x10
                    m_ShiftRegister = 0x10;
                    m_ControlRegister |= 0x0C; // 默认控制寄存器值
                    m_ShiftCount = 0;
                    UpdateBankOffsets( ); // 更新bank偏移
                }
                else
                {
                    // 将值的第0位移入移位寄存器
                    m_ShiftRegister >>= 1; // 右移移位寄存器
                    m_ShiftRegister |= (byte)((value & 0x01) << 4); // 将新值的第0位移入移位寄存器的最高位

                    if(++m_ShiftCount == 5)
                    {
                        byte targetRegister = (byte)((address & 0x6000) >> 13); // 根据地址选择目标寄存器
                        switch(targetRegister)
                        {
                            case 0x00: // $8000-$9FFF：控制寄存器
                                m_ControlRegister = (byte)(m_ShiftRegister & 0x1F);
                                break;
                            case 0x01: // $A000-$BFFF：CHR Bank 0
                                m_ChrBank0 = (byte)(m_ShiftRegister & 0x1F);
                                break;
                            case 0x02: // $C000-$DFFF：CHR Bank 1
                                m_ChrBank1 = (byte)(m_ShiftRegister & 0x1F);
                                break;
                            case 0x03: // $E000-$FFFF：PRG Bank
                                m_PrgBank = (byte)(m_ShiftRegister & 0x0F);
                                break;
                        }
                        UpdateBankOffsets( ); // 更新bank偏移
                        m_ShiftRegister = 0x10;
                        m_ShiftCount = 0;
                    }
                }
                break;
        }
    }

    private void UpdateBankOffsets( )
    {
        m_emulator.Ppu.Mirroring = (m_ControlRegister & 0x03) switch
        {
            0 => VramMirroring.SingleLower,
            1 => VramMirroring.SingleUpper,
            2 => VramMirroring.Vertical,
            3 => VramMirroring.Horizontal,
            _ => VramMirroring.Unknown,
        };

        m_PrgBank %= (byte)m_PrgBankNum;

        switch((m_ControlRegister >> 2) & 0x03) // PRG-ROM模式
        {
            case 0:
            case 1:
                m_PrgBankOffsets[0] = (m_PrgBank & 0xFE) * m_PrgBankSize;
                m_PrgBankOffsets[1] = (m_PrgBank | 0x01) * m_PrgBankSize;
                break;
            case 2:
                m_PrgBankOffsets[0] = 0;
                m_PrgBankOffsets[1] = m_PrgBank * m_PrgBankSize;
                break;
            case 3:
                m_PrgBankOffsets[0] = m_PrgBank * m_PrgBankSize;
                m_PrgBankOffsets[1] = m_PrgRomSize - m_PrgBankSize; // 固定最后一个bank
                break;
        }

        m_ChrBank0 %= (byte)m_ChrBankNum;
        m_ChrBank1 %= (byte)m_ChrBankNum;

        switch((m_ControlRegister >> 4) & 0x01) // CHR-ROM模式
        {
            case 0: // 8KB切换模式
                m_ChrBankOffsets[0] = (m_ChrBank0 & 0xFE) * m_ChrBankSize;
                m_ChrBankOffsets[1] = (m_ChrBank0 | 0x01) * m_ChrBankSize;
                break;
            case 1: // 两个4KB切换模式
                m_ChrBankOffsets[0] = m_ChrBank0 * m_ChrBankSize;
                m_ChrBankOffsets[1] = m_ChrBank1 * m_ChrBankSize;
                break;
        }
    }

    public override void Save(BinaryWriter writer)
    {
        writer.Write(m_PrgRam);
        writer.Write(m_ShiftCount);
        writer.Write(m_ShiftRegister);
        writer.Write(m_ControlRegister);
        writer.Write(m_ChrBank0);
        writer.Write(m_ChrBank1);
        writer.Write(m_PrgBank);
        foreach(var offset in m_PrgBankOffsets)
            writer.Write(offset);
        foreach(var offset in m_ChrBankOffsets)
            writer.Write(offset);
    }

    public override void Load(BinaryReader reader)
    {
        reader.Read(m_PrgRam);
        m_ShiftCount = reader.ReadByte( );
        m_ShiftRegister = reader.ReadByte( );
        m_ControlRegister = reader.ReadByte( );
        m_ChrBank0 = reader.ReadByte( );
        m_ChrBank1 = reader.ReadByte( );
        m_PrgBank = reader.ReadByte( );
        for(int i = 0; i < m_PrgBankOffsets.Length; i++)
            m_PrgBankOffsets[i] = reader.ReadInt32( );
        for(int i = 0; i < m_ChrBankOffsets.Length; i++)
            m_ChrBankOffsets[i] = reader.ReadInt32( );
    }
}