using System.IO;

namespace Nes.Core.Mappers;

class MMC3Mapper : Mapper
{
    private readonly byte[] m_ChrRom;
    private readonly byte[] m_PrgRom;
    private readonly byte[] m_PrgRam = new byte[0x2000];

    protected int[] programBankOffsets;
    protected int[] characterBankOffsets;

    private byte registerIndex;
    private byte[] registers;
    private byte programBankMode;
    private byte characterBankMode;
    private byte irqReload;
    private byte irqCounter;
    private bool irqEnable;

    public MMC3Mapper(Emulator emulator) : base(emulator)
    {
        m_ChrRom = emulator.InstalledCartridge!.ChrData;
        m_PrgRom = emulator.InstalledCartridge!.PrgRom;

        registers = new byte[8];
        programBankOffsets = new int[4];
        characterBankOffsets = new int[8];

        programBankOffsets[0] = GetProgramBankOffset(0);
        programBankOffsets[1] = GetProgramBankOffset(1);
        programBankOffsets[2] = GetProgramBankOffset(-2);
        programBankOffsets[3] = GetProgramBankOffset(-1);
    }

    public override byte ReadByte(ushort address)
    {
        if(address < 0x2000)
        {
            int bank = address / 0x0400;
            int offset = address % 0x0400;

            int flatAddress = characterBankOffsets[bank] + offset;

            return m_ChrRom[flatAddress];
        }
        else if(address >= 0x8000)
        {
            address -= 0x8000;
            int bank = address / 0x2000;
            int offset = address % 0x2000;

            return m_PrgRom[programBankOffsets[bank] + offset];
        }
        else if(address >= 0x6000)
            return m_PrgRam[(ushort)(address - 0x6000)];
        else
        {
            return (byte)(address >> 8); // return open bus
        }
    }

    public override void WriteByte(ushort address, byte value)
    {
        if(address < 0x2000)
        {
            int bank = address / 0x0400;
            int offset = address % 0x0400;

            int flatAddress = characterBankOffsets[bank] + offset;

            m_ChrRom[flatAddress] = value;
        }
        else if(address >= 0x8000)
            WriteRegister(address, value);
        else if(address >= 0x6000)
            m_PrgRam[(ushort)(address - 0x6000)] = value;
    }

    public void SaveState(BinaryWriter binaryWriter)
    {
        binaryWriter.Write(registerIndex);
        binaryWriter.Write(registers);
        binaryWriter.Write(programBankMode);
        binaryWriter.Write(characterBankMode);
        for(int index = 0; index < 4; index++)
            binaryWriter.Write(programBankOffsets[index]);
        for(int index = 0; index < 8; index++)
            binaryWriter.Write(characterBankOffsets[index]);
        binaryWriter.Write(irqReload);
        binaryWriter.Write(irqCounter);
        binaryWriter.Write(irqEnable);
    }

    public void LoadState(BinaryReader binaryReader)
    {
        registerIndex = binaryReader.ReadByte( );
        registers = binaryReader.ReadBytes(8);
        programBankMode = binaryReader.ReadByte( );
        characterBankMode = binaryReader.ReadByte( );
        for(int index = 0; index < 4; index++)
            programBankOffsets[index] = binaryReader.ReadInt32( );
        for(int index = 0; index < 8; index++)
            characterBankOffsets[index] = binaryReader.ReadInt32( );
        irqReload = binaryReader.ReadByte( );
        irqCounter = binaryReader.ReadByte( );
        irqEnable = binaryReader.ReadBoolean( );
    }

    public override void IrqTick( )
    {
        if(irqCounter == 0)
            irqCounter = irqReload;
        else
        {
            --irqCounter;
            if(irqCounter == 0 && irqEnable)
                m_emulator.Cpu.TriggerIrqInterrupt( );
        }
    }

    private void WriteRegister(ushort address, byte value)
    {
        if(address <= 0x9FFF && address % 2 == 0)
            WriteBankSelect(value);
        else if(address <= 0x9FFF && address % 2 == 1)
            WriteBankData(value);
        else if(address <= 0xBFFF && address % 2 == 0)
            WriteMirror(value);
        else if(address <= 0xBFFF && address % 2 == 1)
            WriteProtect(value);
        else if(address <= 0xDFFF && address % 2 == 0)
            WriteIRQLatch(value);
        else if(address <= 0xDFFF && address % 2 == 1)
            WriteIRQReload(value);
        else if(address <= 0xFFFF && address % 2 == 0)
            WriteIRQDisable(value);
        else if(address <= 0xFFFF && address % 2 == 1)
            WriteIRQEnable(value);
    }

    private void WriteBankSelect(byte value)
    {
        programBankMode = (byte)((value >> 6) & 1);
        characterBankMode = (byte)((value >> 7) & 1);
        registerIndex = (byte)(value & 7);
        UpdateOffsets( );
    }

    private void WriteBankData(byte value)
    {
        registers[registerIndex] = value;
        UpdateOffsets( );
    }

    private void WriteMirror(byte value)
    {
        m_emulator.InstalledCartridge!.Mirroring = (value & 1) == 0 ?
            VramMirroring.SingleLower : VramMirroring.Horizontal;
    }

    private void WriteProtect(byte value)
    {
    }

    private void WriteIRQLatch(byte value)
    {
        irqReload = value;
    }

    private void WriteIRQReload(byte value)
    {
        irqCounter = 0;
    }

    private void WriteIRQDisable(byte value)
    {
        irqEnable = false;
    }

    private void WriteIRQEnable(byte value)
    {
        irqEnable = true;
    }

    private int GetProgramBankOffset(int index)
    {
        if(index >= 0x80)
            index -= 0x100;

        index %= m_PrgRom.Length / 0x2000;
        int offset = index * 0x2000;
        if(offset < 0)
            offset += m_PrgRom.Length;

        return offset;
    }

    private int GetCharacterBankOffset(int index)
    {
        if(index >= 0x80)
            index -= 0x100;

        index %= m_ChrRom.Length / 0x0400;

        int offset = index * 0x0400;
        if(offset < 0)
            offset += m_ChrRom.Length;

        return offset;
    }

    private void UpdateOffsets( )
    {
        if(programBankMode == 0)
        {
            programBankOffsets[0] = GetProgramBankOffset(registers[6]);
            programBankOffsets[1] = GetProgramBankOffset(registers[7]);
            programBankOffsets[2] = GetProgramBankOffset(-2);
            programBankOffsets[3] = GetProgramBankOffset(-1);
        }
        else // == 1
        {
            programBankOffsets[0] = GetProgramBankOffset(-2);
            programBankOffsets[1] = GetProgramBankOffset(registers[7]);
            programBankOffsets[2] = GetProgramBankOffset(registers[6]);
            programBankOffsets[3] = GetProgramBankOffset(-1);
        }

        if(characterBankMode == 0)
        {
            characterBankOffsets[0] = GetCharacterBankOffset(registers[0] & 0xFE);
            characterBankOffsets[1] = GetCharacterBankOffset(registers[0] | 0x01);
            characterBankOffsets[2] = GetCharacterBankOffset(registers[1] & 0xFE);
            characterBankOffsets[3] = GetCharacterBankOffset(registers[1] | 0x01);
            characterBankOffsets[4] = GetCharacterBankOffset(registers[2]);
            characterBankOffsets[5] = GetCharacterBankOffset(registers[3]);
            characterBankOffsets[6] = GetCharacterBankOffset(registers[4]);
            characterBankOffsets[7] = GetCharacterBankOffset(registers[5]);
        }
        else // == 1
        {
            characterBankOffsets[0] = GetCharacterBankOffset(registers[2]);
            characterBankOffsets[1] = GetCharacterBankOffset(registers[3]);
            characterBankOffsets[2] = GetCharacterBankOffset(registers[4]);
            characterBankOffsets[3] = GetCharacterBankOffset(registers[5]);
            characterBankOffsets[4] = GetCharacterBankOffset(registers[0] & 0xFE);
            characterBankOffsets[5] = GetCharacterBankOffset(registers[0] | 0x01);
            characterBankOffsets[6] = GetCharacterBankOffset(registers[1] & 0xFE);
            characterBankOffsets[7] = GetCharacterBankOffset(registers[1] | 0x01);
        }
    }
}
