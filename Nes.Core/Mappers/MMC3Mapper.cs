using System.IO;

namespace Nes.Core.Mappers;

[Mapper(0x03, "MMC3")]
class MMC3Mapper : Mapper
{
    private readonly byte[] m_ChrRom;
    private readonly byte[] m_PrgRom;
    private readonly byte[] m_PrgRam = new byte[0x2000];

    private readonly int[] m_PrgRomBankOffset = new int[4];
    private readonly int[] m_ChrRomBankOffset = new int[8];

    private byte[] m_Registers = new byte[8];
    private byte m_RegisterIndex;
    private byte m_PrgRomBankMode;
    private byte m_ChrRomBankMode;
    private byte m_IrqReload;
    private byte m_IrqCount;
    private bool m_IsIrqEnable;

    public MMC3Mapper(Emulator emulator) : base(emulator)
    {
        m_ChrRom = emulator.InstalledCartridge!.ChrData;
        m_PrgRom = emulator.InstalledCartridge!.PrgRom;

        m_PrgRomBankOffset[0] = GetPrgRomBankOffset(0);
        m_PrgRomBankOffset[1] = GetPrgRomBankOffset(1);
        m_PrgRomBankOffset[2] = GetPrgRomBankOffset(-2);
        m_PrgRomBankOffset[3] = GetPrgRomBankOffset(-1);
    }

    public override byte ReadByte(ushort address)
    {
        if(address < 0x2000)
        {
            int bank = address / 0x0400;
            int offset = address % 0x0400;

            int flatAddress = m_ChrRomBankOffset[bank] + offset;

            return m_ChrRom[flatAddress];
        }
        else if(address >= 0x8000)
        {
            address -= 0x8000;
            int bank = address / 0x2000;
            int offset = address % 0x2000;

            return m_PrgRom[m_PrgRomBankOffset[bank] + offset];
        }
        else if(address >= 0x6000)
            return m_PrgRam[(ushort)(address - 0x6000)];
        else
            return 0;
    }

    public override void WriteByte(ushort address, byte value)
    {
        if(address < 0x2000)
        {
            int bank = address / 0x0400;
            int offset = address % 0x0400;

            int flatAddress = m_ChrRomBankOffset[bank] + offset;

            m_ChrRom[flatAddress] = value;
        }
        else if(address >= 0x8000)
            WriteRegister(address, value);
        else if(address >= 0x6000)
            m_PrgRam[(ushort)(address - 0x6000)] = value;
    }

    private void WriteRegister(ushort address, byte value)
    {
        if(address <= 0x9FFF && address % 2 == 0)
        {
            m_PrgRomBankMode = (byte)((value >> 6) & 1);
            m_ChrRomBankMode = (byte)((value >> 7) & 1);
            m_RegisterIndex = (byte)(value & 7);
            UpdateOffsets( );
        }
        else if(address <= 0x9FFF && address % 2 == 1)
        {
            m_Registers[m_RegisterIndex] = value;
            UpdateOffsets( );
        }
        else if(address <= 0xBFFF && address % 2 == 0)
        {
            m_emulator.Ppu.Mirroring = (value & 1) == 0 ?
                VramMirroring.Vertical : VramMirroring.Horizontal;
        }
        else if(address <= 0xBFFF && address % 2 == 1)
        { }
        else if(address <= 0xDFFF && address % 2 == 0)
            m_IrqReload = value;
        else if(address <= 0xDFFF && address % 2 == 1)
            m_IrqCount = 0;
        else if(address <= 0xFFFF && address % 2 == 0)
            m_IsIrqEnable = false;
        else if(address <= 0xFFFF && address % 2 == 1)
            m_IsIrqEnable = true;
    }

    private int GetPrgRomBankOffset(int index)
    {
        if(index >= 0x80) index -= 0x100;

        index %= m_PrgRom.Length / 0x2000;
        int offset = index * 0x2000;
        if(offset < 0)
            offset += m_PrgRom.Length;

        return offset;
    }

    private int GetChrRomBankOffset(int index)
    {
        if(index >= 0x80) index -= 0x100;

        index %= m_ChrRom.Length / 0x0400;
        int offset = index * 0x0400;
        if(offset < 0)
            offset += m_ChrRom.Length;

        return offset;
    }

    private void UpdateOffsets( )
    {
        if(m_PrgRomBankMode == 0)
        {
            m_PrgRomBankOffset[0] = GetPrgRomBankOffset(m_Registers[6]);
            m_PrgRomBankOffset[1] = GetPrgRomBankOffset(m_Registers[7]);
            m_PrgRomBankOffset[2] = GetPrgRomBankOffset(-2);
            m_PrgRomBankOffset[3] = GetPrgRomBankOffset(-1);
        }
        else // == 1
        {
            m_PrgRomBankOffset[0] = GetPrgRomBankOffset(-2);
            m_PrgRomBankOffset[1] = GetPrgRomBankOffset(m_Registers[7]);
            m_PrgRomBankOffset[2] = GetPrgRomBankOffset(m_Registers[6]);
            m_PrgRomBankOffset[3] = GetPrgRomBankOffset(-1);
        }

        if(m_ChrRomBankMode == 0)
        {
            m_ChrRomBankOffset[0] = GetChrRomBankOffset(m_Registers[0] & 0xFE);
            m_ChrRomBankOffset[1] = GetChrRomBankOffset(m_Registers[0] | 0x01);
            m_ChrRomBankOffset[2] = GetChrRomBankOffset(m_Registers[1] & 0xFE);
            m_ChrRomBankOffset[3] = GetChrRomBankOffset(m_Registers[1] | 0x01);
            m_ChrRomBankOffset[4] = GetChrRomBankOffset(m_Registers[2]);
            m_ChrRomBankOffset[5] = GetChrRomBankOffset(m_Registers[3]);
            m_ChrRomBankOffset[6] = GetChrRomBankOffset(m_Registers[4]);
            m_ChrRomBankOffset[7] = GetChrRomBankOffset(m_Registers[5]);
        }
        else // == 1
        {
            m_ChrRomBankOffset[0] = GetChrRomBankOffset(m_Registers[2]);
            m_ChrRomBankOffset[1] = GetChrRomBankOffset(m_Registers[3]);
            m_ChrRomBankOffset[2] = GetChrRomBankOffset(m_Registers[4]);
            m_ChrRomBankOffset[3] = GetChrRomBankOffset(m_Registers[5]);
            m_ChrRomBankOffset[4] = GetChrRomBankOffset(m_Registers[0] & 0xFE);
            m_ChrRomBankOffset[5] = GetChrRomBankOffset(m_Registers[0] | 0x01);
            m_ChrRomBankOffset[6] = GetChrRomBankOffset(m_Registers[1] & 0xFE);
            m_ChrRomBankOffset[7] = GetChrRomBankOffset(m_Registers[1] | 0x01);
        }
    }

    public override void IrqTick( )
    {
        if(m_IrqCount == 0)
            m_IrqCount = m_IrqReload;
        else
        {
            --m_IrqCount;
            if(m_IrqCount == 0 && m_IsIrqEnable)
                m_emulator.Cpu.TriggerIrqInterrupt( );
        }
    }

    public override void Save(BinaryWriter writer)
    {
        writer.Write(m_RegisterIndex);
        writer.Write(m_Registers);
        writer.Write(m_PrgRomBankMode);
        writer.Write(m_ChrRomBankMode);
        for(int index = 0; index < 4; index++)
            writer.Write(m_PrgRomBankOffset[index]);
        for(int index = 0; index < 8; index++)
            writer.Write(m_ChrRomBankOffset[index]);
        writer.Write(m_IrqReload);
        writer.Write(m_IrqCount);
        writer.Write(m_IsIrqEnable);
    }

    public override void Load(BinaryReader reader)
    {
        m_RegisterIndex = reader.ReadByte( );
        m_Registers = reader.ReadBytes(8);
        m_PrgRomBankMode = reader.ReadByte( );
        m_ChrRomBankMode = reader.ReadByte( );
        for(int index = 0; index < 4; index++)
            m_PrgRomBankOffset[index] = reader.ReadInt32( );
        for(int index = 0; index < 8; index++)
            m_ChrRomBankOffset[index] = reader.ReadInt32( );
        m_IrqReload = reader.ReadByte( );
        m_IrqCount = reader.ReadByte( );
        m_IsIrqEnable = reader.ReadBoolean( );
    }
}
