using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Nes.Core;

public class Cpu
{

    #region Internal Fields

    // internal const ushort StackPageOffset = 0x100;

    internal const byte StackResetValue = 0xfd; // 栈指针初始值

    internal const byte StatusFlagResetValue = 0x24;    // CPU标志寄存器初始值

    #endregion Internal Fields

    #region Protected Fields

    protected readonly Bus _bus;    // 总线

    /// CPU内部寄存器
    protected byte _a;
    protected CpuFlags _flag;   // CPU标志寄存器
    protected ushort _pc;
    protected byte _sp;
    protected byte _x;
    protected byte _y;

    #endregion Protected Fields

    #region Private Fields

    /// <summary>
    /// 操作码的寻址模式映射表
    /// </summary>
    private static readonly byte[] _addressingModes =
    [
        0, 11, 0, 11, 3, 3, 3, 3, 0, 2, 1, 2, 7, 7, 7, 7,
        6, 12, 0, 12, 4, 4, 4, 4, 0, 9, 0, 9, 8, 8, 8, 8,
        7, 11, 0, 11, 3, 3, 3, 3, 0, 2, 1, 2, 7, 7, 7, 7,
        6, 12, 0, 12, 4, 4, 4, 4, 0, 9, 0, 9, 8, 8, 8, 8,
        0, 11, 0, 11, 3, 3, 3, 3, 0, 2, 1, 2, 7, 7, 7, 7,
        6, 12, 0, 12, 4, 4, 4, 4, 0, 9, 0, 9, 8, 8, 8, 8,
        0, 11, 0, 11, 3, 3, 3, 3, 0, 2, 1, 2, 10, 7, 7, 7,
        6, 12, 0, 12, 4, 4, 4, 4, 0, 9, 0, 9, 8, 8, 8, 8,
        2, 11, 2, 11, 3, 3, 3, 3, 0, 2, 0, 2, 7, 7, 7, 7,
        6, 12, 0, 12, 4, 4, 5, 5, 0, 9, 0, 9, 8, 8, 9, 9,
        2, 11, 2, 11, 3, 3, 3, 3, 0, 2, 0, 2, 7, 7, 7, 7,
        6, 12, 0, 12, 4, 4, 5, 5, 0, 9, 0, 9, 8, 8, 9, 9,
        2, 11, 2, 11, 3, 3, 3, 3, 0, 2, 0, 2, 7, 7, 7, 7,
        6, 12, 0, 12, 4, 4, 4, 4, 0, 9, 0, 9, 8, 8, 8, 8,
        2, 11, 2, 11, 3, 3, 3, 3, 0, 2, 0, 2, 7, 7, 7, 7,
        6, 12, 0, 12, 4, 4, 4, 4, 0, 9, 0, 9, 8, 8, 8, 8
    ];

    /// <summary>
    /// 操作码的执行所需的CPU周期数映射表
    /// </summary>
    private static readonly byte[] _instructionCycles =
    [
        7, 6, 0, 8, 3, 3, 5, 5, 3, 2, 2, 2, 4, 4, 6, 6,
        2, 5, 0, 8, 4, 4, 6, 6, 2, 4, 2, 7, 4, 4, 7, 7,
        6, 6, 0, 8, 3, 3, 5, 5, 4, 2, 2, 2, 4, 4, 6, 6,
        2, 5, 0, 8, 4, 4, 6, 6, 2, 4, 2, 7, 4, 4, 7, 7,
        6, 6, 0, 8, 3, 3, 5, 5, 3, 2, 2, 2, 3, 4, 6, 6,
        2, 5, 0, 8, 4, 4, 6, 6, 2, 4, 2, 7, 4, 4, 7, 7,
        6, 6, 0, 8, 3, 3, 5, 5, 4, 2, 2, 2, 5, 4, 6, 6,
        2, 5, 0, 8, 4, 4, 6, 6, 2, 4, 2, 7, 4, 4, 7, 7,
        2, 6, 2, 6, 3, 3, 3, 3, 2, 2, 2, 2, 4, 4, 4, 4,
        2, 6, 0, 6, 4, 4, 4, 4, 2, 5, 2, 5, 5, 5, 5, 5,
        2, 6, 2, 6, 3, 3, 3, 3, 2, 2, 2, 2, 4, 4, 4, 4,
        2, 5, 0, 5, 4, 4, 4, 4, 2, 4, 2, 4, 4, 4, 4, 4,
        2, 6, 2, 8, 3, 3, 5, 5, 2, 2, 2, 2, 4, 4, 6, 6,
        2, 5, 0, 8, 4, 4, 6, 6, 2, 4, 2, 7, 4, 4, 7, 7,
        2, 6, 2, 8, 3, 3, 5, 5, 2, 2, 2, 2, 4, 4, 6, 6,
        2, 5, 0, 8, 4, 4, 6, 6, 2, 4, 2, 7, 4, 4, 7, 7
    ];

    /// <summary>
    /// 当操作码执行跨页时，额外需要的CPU周期数映射表
    /// </summary>
    private static readonly byte[] _instructionPageCycles =
    [
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        1, 1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 1, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        1, 1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 1, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        1, 1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 1, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        1, 1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 1, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        1, 1, 0, 1, 0, 0, 0, 0, 0, 1, 0, 1, 1, 1, 1, 1,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        1, 1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 1, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        1, 1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 1, 0, 0
    ];

    /// <summary>
    /// 操作码及其操作数的字节数映射表
    /// </summary>
    private static readonly byte[] _instructionSizes =
    [
        1, 2, 1, 2, 2, 2, 2, 2, 1, 2, 1, 2, 3, 3, 3, 3,
        2, 2, 1, 2, 2, 2, 2, 2, 1, 3, 1, 3, 3, 3, 3, 3,
        3, 2, 1, 2, 2, 2, 2, 2, 1, 2, 1, 2, 3, 3, 3, 3,
        2, 2, 1, 2, 2, 2, 2, 2, 1, 3, 1, 3, 3, 3, 3, 3,
        1, 2, 1, 2, 2, 2, 2, 2, 1, 2, 1, 2, 3, 3, 3, 3,
        2, 2, 1, 2, 2, 2, 2, 2, 1, 3, 1, 3, 3, 3, 3, 3,
        1, 2, 1, 2, 2, 2, 2, 2, 1, 2, 1, 2, 3, 3, 3, 3,
        2, 2, 1, 2, 2, 2, 2, 2, 1, 3, 1, 3, 3, 3, 3, 3,
        2, 2, 2, 2, 2, 2, 2, 2, 1, 2, 1, 2, 3, 3, 3, 3,
        2, 2, 1, 2, 2, 2, 2, 2, 1, 3, 1, 3, 3, 3, 3, 3,
        2, 2, 2, 2, 2, 2, 2, 2, 1, 2, 1, 2, 3, 3, 3, 3,
        2, 2, 1, 2, 2, 2, 2, 2, 1, 3, 1, 3, 3, 3, 3, 3,
        2, 2, 2, 2, 2, 2, 2, 2, 1, 2, 1, 2, 3, 3, 3, 3,
        2, 2, 1, 2, 2, 2, 2, 2, 1, 3, 1, 3, 3, 3, 3, 3,
        2, 2, 2, 2, 2, 2, 2, 2, 1, 2, 1, 2, 3, 3, 3, 3,
        2, 2, 1, 2, 2, 2, 2, 2, 1, 3, 1, 3, 3, 3, 3, 3
    ];

    private readonly Opcode[] _opcodes; // 操作码表--汇编指令表

    private int _idleCycles;    // 空闲周期数

    /// <summary>
    /// NMI(不可屏蔽中断)触发标志
    /// </summary>
    private bool _nmiInterruptTriggered;

    /// <summary>
    /// IRQ(可屏蔽中断)触发标志
    /// </summary>
    private bool _irqInterruptTriggered;

    #endregion Private Fields

    #region Public Constructors

    public Cpu(Emulator emulator)
    {
        _bus = emulator.Bus;
        _opcodes =
        [
            //     00    01    02    03    04    05    06    07    08    09    0A    0B    0C    0D    0E    0F
            /*00*/ _brk, _ora, _stp, _slo, _nop, _ora, _asl, _slo, _php, _ora, _asl, _anc, _nop, _ora, _asl, _slo,
            /*10*/ _bpl, _ora, _stp, _slo, _nop, _ora, _asl, _slo, _clc, _ora, _nop, _slo, _nop, _ora, _asl, _slo,
            /*20*/ _jsr, _and, _stp, _rla, _bit, _and, _rol, _rla, _plp, _and, _rol, _anc, _bit, _and, _rol, _rla,
            /*30*/ _bmi, _and, _stp, _rla, _nop, _and, _rol, _rla, _sec, _and, _nop, _rla, _nop, _and, _rol, _rla,
            /*40*/ _rti, _eor, _stp, _sre, _nop, _eor, _lsr, _sre, _pha, _eor, _lsr, _alr, _jmp, _eor, _lsr, _sre,
            /*50*/ _bvc, _eor, _stp, _sre, _nop, _eor, _lsr, _sre, _cli, _eor, _nop, _sre, _nop, _eor, _lsr, _sre,
            /*60*/ _rts, _adc, _stp, _rra, _nop, _adc, _ror, _rra, _pla, _adc, _ror, _arr, _jmp, _adc, _ror, _rra,
            /*70*/ _bvs, _adc, _stp, _rra, _nop, _adc, _ror, _rra, _sei, _adc, _nop, _rra, _nop, _adc, _ror, _rra,
            /*80*/ _nop, _sta, _nop, _sax, _sty, _sta, _stx, _sax, _dey, _nop, _txa, _xaa, _sty, _sta, _stx, _sax,
            /*90*/ _bcc, _sta, _stp, _ahx, _sty, _sta, _stx, _sax, _tya, _sta, _txs, _tas, _shy, _sta, _shx, _ahx,
            /*A0*/ _ldy, _lda, _ldx, _lax, _ldy, _lda, _ldx, _lax, _tay, _lda, _tax, _lax, _ldy, _lda, _ldx, _lax,
            /*B0*/ _bcs, _lda, _stp, _lax, _ldy, _lda, _ldx, _lax, _clv, _lda, _tsx, _las, _ldy, _lda, _ldx, _lax,
            /*C0*/ _cpy, _cmp, _nop, _dcp, _cpy, _cmp, _dec, _dcp, _iny, _cmp, _dex, _axs, _cpy, _cmp, _dec, _dcp,
            /*D0*/ _bne, _cmp, _stp, _dcp, _nop, _cmp, _dec, _dcp, _cld, _cmp, _nop, _dcp, _nop, _cmp, _dec, _dcp,
            /*E0*/ _cpx, _sbc, _nop, _isc, _cpx, _sbc, _inc, _isc, _inx, _sbc, _nop, _sbc, _cpx, _sbc, _inc, _isc,
            /*F0*/ _beq, _sbc, _stp, _isc, _nop, _sbc, _inc, _isc, _sed, _sbc, _nop, _isc, _nop, _sbc, _inc, _isc
        ];
    }

    #endregion Public Constructors

    #region Private Delegates

    private delegate void Opcode(AddressingMode mode, ushort address);

    #endregion Private Delegates

    #region Public Properties

    /// <summary>
    /// 获取CPU的A寄存器值
    /// </summary>
    public byte A => _a;

    public long Cycles { get; private set; }

    /// <summary>
    /// 获取CPU的标志寄存器值
    /// </summary>
    public CpuFlags Flag => _flag;

    /// <summary>
    /// 获取CPU的PC寄存器值
    /// </summary>
    public ushort PC => _pc;

    /// <summary>
    /// 获取CPU的SP寄存器值
    /// </summary>
    public byte SP => _sp;

    /// <summary>
    /// 获取CPU的X寄存器值
    /// </summary>
    public byte X => _x;

    /// <summary>
    /// 获取CPU的Y寄存器值
    /// </summary>
    public byte Y => _y;

    #endregion Public Properties

    #region Public Methods

    /// <summary>
    /// 反汇编
    /// </summary>
    public string Disassemble(ushort offset, byte[] instruction)
    {
        var sb = new StringBuilder( );
        sb.Append($"${offset:x4}".PadRight(9, ' '));
        sb.Append(string.Join(" ", instruction.Select(c => $"{c:x2}")).PadRight(10, ' '));
        sb.Append(GetInstructionName(instruction[0]));
        sb.Append(' ');
        var addressingMode = (AddressingMode)_addressingModes[instruction[0]];
        if(addressingMode != AddressingMode.Implicit)
        {
            var operand = 0;
            if(instruction.Length == 2)
            {
                operand = instruction[1];
            }
            else if(instruction.Length == 3)
            {
                var lo = instruction[1];
                var hi = instruction[2];
                operand = (hi << 8) | lo;
            }

            sb.Append(Utils.FormatAddressByMode(addressingMode, (ushort)operand).PadRight(8, ' '));
            if(addressingMode == AddressingMode.Relative) sb.Append($"; ${(ushort)(offset + (sbyte)operand + 2):x4}");
        }

        return sb.ToString( );
    }

    /// <summary>
    /// 获取操作码对应的寻址模式
    /// </summary>
    /// <param name="opcode">操作码</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AddressingMode GetInstructionAddressingMode(byte opcode)
    {
        return (AddressingMode)_addressingModes[opcode];
    }

    /// <summary>
    /// 获取操作码对应的汇编指令名称
    /// </summary>
    /// <param name="opcode">操作码</param>
    /// <returns>操作码对应的汇编指令名称</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string GetInstructionName(byte opcode)
    {
        return _opcodes[opcode].Method.Name[1..].ToUpperInvariant( );
    }

    /// <summary>
    /// 获取操作码及其操作数共占的字节数
    /// </summary>
    /// <param name="opcode">操作码</param>
    /// <returns>字节数</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetInstructionSize(byte opcode)
    {
        return _instructionSizes[opcode];
    }

    /// <summary>
    /// 重置CPU
    /// </summary>
    public void Reset( )
    {
        _a = _x = _y = 0;
        _sp = StackResetValue;
        _flag = new CpuFlags(StatusFlagResetValue);
        // NMI中断地址:0xFFFA,0xFFFB   RESET中断地址:0xFFFC,0xFFFD   IRQ中断地址:0xFFFE,0xFFFF
        _pc = _bus.ReadWord(0xfffc);
        Cycles = 0;
        _idleCycles = 0;
        _nmiInterruptTriggered = false;
        _irqInterruptTriggered = false;
    }

    /// <summary>
    /// CPU执行一步操作
    /// </summary>
    /// <returns>操作码消耗的周期数</returns>
    public long Step( )
    {
        if(_idleCycles > 0)
        {/// 如果CPU处于空闲状态，则减少空闲周期数并返回
            _idleCycles--;
            return 1;
        }

        if(_nmiInterruptTriggered)
        {/// 发生NMI(不可屏蔽中断)
            Interrupt(InterruptType.Nmi);   // 执行中断
            _nmiInterruptTriggered = false; // 清除NMI触发标志
        }

        if(_irqInterruptTriggered && !_flag.I)
        {/// 发生IRQ(可屏蔽中断)
            Interrupt(InterruptType.Irq);   // 执行中断
            _irqInterruptTriggered = false; // 清除IRQ触发标志
        }

        var origCycles = Cycles;
        var opcode = _bus.ReadByte(_pc);    // 通过PC寻址获取操作码
        var mode = (AddressingMode)_addressingModes[opcode];    // 获取操作码对应的寻址模式
        // 根据寻址模式解析出操作数的真实地址
        var address = ResolveAddress(mode, (ushort)(_pc + 1), out var pageCrossed);
        var instructionSize = _instructionSizes[opcode];    // 获取操作码及其操作数共占的字节数

        //var instruction = new byte[instructionSize];
        //for(var idx = 0; idx < instructionSize; idx++)
        //_ = _bus.ReadByte((ushort)(_pc + idx));

        _pc += (ushort)instructionSize; // 更新PC指针
        Cycles += _instructionCycles[opcode];   // 更新CPU周期数
        if(pageCrossed) Cycles += _instructionPageCycles[opcode];   // 如果操作码执行跨页，则额外增加CPU周期数

        _opcodes[opcode](mode, address);    // 执行操作码对应的汇编指令

        var opcodeCycles = Cycles - origCycles; // 执行当前操作码所需的CPU周期数

        return opcodeCycles;    // 返回当前操作码执行所需的CPU周期数
    }

    /// <summary>
    /// 触发NMI(不可屏蔽中断)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void TriggerNmiInterrupt( )
    {
        _nmiInterruptTriggered = true;
    }


    /// <summary>
    /// 触发IRQ(可屏蔽中断)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void TriggerIrqInterrupt( )
    {
        _irqInterruptTriggered = true;
    }

    /// <summary>
    /// 存档
    /// </summary>
    public void Save(BinaryWriter writer)
    {
        writer.Write(_a);
        writer.Write(_flag.Value);
        writer.Write(_pc);
        writer.Write(_sp);
        writer.Write(_x);
        writer.Write(_y);
        writer.Write(Cycles);
        writer.Write(_idleCycles);
        writer.Write(_nmiInterruptTriggered);
        writer.Write(_irqInterruptTriggered);
    }

    /// <summary>
    /// 读档
    /// </summary>
    public void Load(BinaryReader reader)
    {
        _a = reader.ReadByte( );
        _flag = new CpuFlags(reader.ReadByte( ));
        _pc = reader.ReadUInt16( );
        _sp = reader.ReadByte( );
        _x = reader.ReadByte( );
        _y = reader.ReadByte( );
        Cycles = reader.ReadInt64( );
        _idleCycles = reader.ReadInt32( );
        _nmiInterruptTriggered = reader.ReadBoolean( );
        _irqInterruptTriggered = reader.ReadBoolean( );
    }

    #endregion Public Methods

    #region Internal Methods

    /// <summary>
    /// 添加空闲周期数
    /// </summary>
    /// <param name="c">周期数</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AddIdleCycles(int c)
    {
        _idleCycles += c;
    }

    #endregion Internal Methods

    #region Private Methods // 私有方法

    /// <summary>
    /// 判断新旧地址是否跨页
    /// </summary>
    /// <param name="addressA">新地址</param>
    /// <param name="addressB">旧地址</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsPageCrossed(ushort addressA, ushort addressB)
    {
        return (addressA & 0xff) != (addressB & 0xff);
    }

    #region Opcode Implementations  // 操作码实现

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _adc(AddressingMode mode, ushort address)
    {
        var value = _bus.ReadByte(address);
        var sum = _a + value + _flag.C;
        _flag.C = sum > byte.MaxValue;
        var result = (byte)sum;
        _flag.V = ((value ^ result) & (_a ^ result) & 0x80) != 0;
        _a = result;
        SetZeroAndNegativeFlags(result);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _ahx(AddressingMode mode, ushort address)
    {
        var result = (byte)(_a & _x);
        var hiByteAddr = (byte)(address >> 8);
        result = (byte)(result & (hiByteAddr + 1));
        _bus.WriteByte(address, result);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _alr(AddressingMode mode, ushort address)
    {
        var value = _bus.ReadByte(address);
        _a = (byte)(value & _a);
        _flag.C = Bit.HasSet(_a, 0);
        _a >>= 1;
        SetZeroAndNegativeFlags(_a);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _anc(AddressingMode mode, ushort address)
    {
        _and(mode, address);
        _flag.C = Bit.HasSet(_a, 7);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _and(AddressingMode mode, ushort address)
    {
        var value = _bus.ReadByte(address);
        _a = (byte)(_a & value);
        SetZeroAndNegativeFlags(_a);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _arr(AddressingMode mode, ushort address)
    {
        var value = _bus.ReadByte(address);
        _a = (byte)((value & _a) >> 1);
        _flag.V = !(Bit.Get(_a, 5) == Bit.Get(_a, 6));
        _flag.C = Bit.HasSet(_a, 6);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _asl(AddressingMode mode, ushort address)
    {
        byte shift(byte val)
        {
            _flag.C = Bit.HasSet(val, 7);
            var ret = (byte)(val << 1);
            SetZeroAndNegativeFlags(ret);
            return ret;
        }

        if(mode == AddressingMode.Accumulator)
            _a = shift(_a);
        else
            _bus.WriteByte(address, shift(_bus.ReadByte(address)));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _axs(AddressingMode mode, ushort address)
    {
        _x &= _a;
        var value = _bus.ReadByte(address);
        _x -= value;
        _flag.Z = _x == 0;
        _flag.C = _x >= 0;
        _flag.N = _x != 0 && (_x & 0x80) > 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _bcc(AddressingMode mode, ushort address)
    {
        if(!_flag.C) Branch(address);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _bcs(AddressingMode mode, ushort address)
    {
        if(_flag.C) Branch(address);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _beq(AddressingMode mode, ushort address)
    {
        if(_flag.Z) Branch(address);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _bit(AddressingMode mode, ushort address)
    {
        var val = _bus.ReadByte(address);
        var result = (byte)(val & _a);
        _flag.Z = result == 0;
        _flag.V = Bit.Get(val, 6);
        _flag.N = Bit.Get(val, 7);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _bmi(AddressingMode mode, ushort address)
    {
        if(_flag.N) Branch(address);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _bne(AddressingMode mode, ushort address)
    {
        if(!_flag.Z) Branch(address);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _bpl(AddressingMode mode, ushort address)
    {
        if(!_flag.N) Branch(address);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _brk(AddressingMode mode, ushort address)
    {
        Interrupt(InterruptType.Brk);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _bvc(AddressingMode mode, ushort address)
    {
        if(!_flag.V) Branch(address);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _bvs(AddressingMode mode, ushort address)
    {
        if(_flag.V) Branch(address);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _clc(AddressingMode mode, ushort address)
    {
        _flag.C = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _cld(AddressingMode mode, ushort address)
    {
        _flag.D = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _cli(AddressingMode mode, ushort address)
    {
        _flag.I = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _clv(AddressingMode mode, ushort address)
    {
        _flag.V = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _cmp(AddressingMode mode, ushort address)
    {
        var value = _bus.ReadByte(address);
        var result = _a - value;
        _flag.Z = result == 0;
        _flag.C = result >= 0;
        _flag.N = result != 0 && (result & 0x80) > 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _cpx(AddressingMode mode, ushort address)
    {
        var value = _bus.ReadByte(address);
        var result = _x - value;
        _flag.Z = result == 0;
        _flag.C = result >= 0;
        _flag.N = (result & 0x80) > 0 && result != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _cpy(AddressingMode mode, ushort address)
    {
        var value = _bus.ReadByte(address);
        var result = _y - value;
        _flag.Z = result == 0;
        _flag.C = result >= 0;
        _flag.N = (result & 0x80) > 0 && result != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _dcp(AddressingMode mode, ushort address)
    {
        var value = _bus.ReadByte(address);
        value--;
        _bus.WriteByte(address, value);
        _cmp(mode, address);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _dec(AddressingMode mode, ushort address)
    {
        var val = _bus.ReadByte(address);
        val--;
        _bus.WriteByte(address, val);
        SetZeroAndNegativeFlags(val);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _dex(AddressingMode mode, ushort address)
    {
        _x--;
        SetZeroAndNegativeFlags(_x);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _dey(AddressingMode mode, ushort address)
    {
        _y--;
        SetZeroAndNegativeFlags(_y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _eor(AddressingMode mode, ushort address)
    {
        var value = _bus.ReadByte(address);
        _a = (byte)(_a ^ value);
        SetZeroAndNegativeFlags(_a);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _inc(AddressingMode mode, ushort address)
    {
        var val = _bus.ReadByte(address);
        val++;
        _bus.WriteByte(address, val);
        SetZeroAndNegativeFlags(val);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _inx(AddressingMode mode, ushort address)
    {
        _x++;
        SetZeroAndNegativeFlags(_x);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _iny(AddressingMode mode, ushort address)
    {
        _y++;
        SetZeroAndNegativeFlags(_y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _isc(AddressingMode mode, ushort address)
    {
        _inc(mode, address);
        _sbc(mode, address);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _jmp(AddressingMode mode, ushort address)
    {
        _pc = address;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _jsr(AddressingMode mode, ushort address)
    {
        StackPushWord((ushort)(_pc - 1));
        _pc = address;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _las(AddressingMode mode, ushort address)
    {
        var value = (byte)(_bus.ReadByte(address) & _sp);
        _a = _x = _sp = value;
        SetZeroAndNegativeFlags(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _lax(AddressingMode mode, ushort address)
    {
        _lda(mode, address);
        _tax(mode, address);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _lda(AddressingMode mode, ushort address)
    {
        _a = _bus.ReadByte(address);
        SetZeroAndNegativeFlags(_a);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _ldx(AddressingMode mode, ushort address)
    {
        _x = _bus.ReadByte(address);
        SetZeroAndNegativeFlags(_x);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _ldy(AddressingMode mode, ushort address)
    {
        _y = _bus.ReadByte(address);
        SetZeroAndNegativeFlags(_y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _lsr(AddressingMode mode, ushort address)
    {
        byte shift(byte val)
        {
            _flag.C = Bit.HasSet(val, 0);
            var ret = (byte)(val >> 1);
            SetZeroAndNegativeFlags(ret);
            return ret;
        }

        if(mode == AddressingMode.Accumulator)
            _a = shift(_a);
        else
            _bus.WriteByte(address, shift(_bus.ReadByte(address)));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _nop(AddressingMode mode, ushort address)
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _ora(AddressingMode mode, ushort address)
    {
        var value = _bus.ReadByte(address);
        _a = (byte)(_a | value);
        SetZeroAndNegativeFlags(_a);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _pha(AddressingMode mode, ushort address)
    {
        StackPushByte(_a);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _php(AddressingMode mode, ushort address)
    {
        StackPushByte(_flag.Value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _pla(AddressingMode mode, ushort address)
    {
        _a = StackPullByte( );
        SetZeroAndNegativeFlags(_a);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _plp(AddressingMode mode, ushort address)
    {
        _flag.Value = StackPullByte( );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _rla(AddressingMode mode, ushort address)
    {
        _rol(mode, address);
        _and(mode, address);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _rol(AddressingMode mode, ushort address)
    {
        byte shift(byte val)
        {
            var c = _flag.C ? 1 : 0;
            _flag.C = Bit.HasSet(val, 7);
            var ret = (byte)((val << 1) | c);
            SetZeroAndNegativeFlags(ret);
            return ret;
        }

        if(mode == AddressingMode.Accumulator)
            _a = shift(_a);
        else
            _bus.WriteByte(address, shift(_bus.ReadByte(address)));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _ror(AddressingMode mode, ushort address)
    {
        byte shift(byte val)
        {
            var c = _flag.C ? 0x80 : 0;
            _flag.C = Bit.HasSet(val, 0);
            var ret = (byte)((val >> 1) | c);
            SetZeroAndNegativeFlags(ret);
            return ret;
        }

        if(mode == AddressingMode.Accumulator)
            _a = shift(_a);
        else
            _bus.WriteByte(address, shift(_bus.ReadByte(address)));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _rra(AddressingMode mode, ushort address)
    {
        _ror(mode, address);
        _adc(mode, address);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _rti(AddressingMode mode, ushort address)
    {
        _flag.Value = StackPullByte( );
        _pc = StackPullWord( );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _rts(AddressingMode mode, ushort address)
    {
        _pc = (ushort)(StackPullWord( ) + 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _sax(AddressingMode mode, ushort address)
    {
        var result = (byte)(_x & _a);
        _bus.WriteByte(address, result);
        SetZeroAndNegativeFlags(result);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _sbc(AddressingMode mode, ushort address)
    {
        var value = _bus.ReadByte(address);
        var v = (byte)~value;
        var sum = _a + v + _flag.C;
        _flag.C = sum > byte.MaxValue;
        var result = (byte)sum;
        _flag.V = ((v ^ result) & (_a ^ result) & 0x80) != 0;
        _a = result;
        SetZeroAndNegativeFlags(result);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _sec(AddressingMode mode, ushort address)
    {
        _flag.C = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _sed(AddressingMode mode, ushort address)
    {
        _flag.D = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _sei(AddressingMode mode, ushort address)
    {
        _flag.I = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _shx(AddressingMode mode, ushort address)
    {
        var v = (byte)((address >> 8) + 1);
        _bus.WriteByte(address, (byte)(_x & v));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _shy(AddressingMode mode, ushort address)
    {
        var v = (byte)((address >> 8) + 1);
        _bus.WriteByte(address, (byte)(_y & v));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _slo(AddressingMode mode, ushort address)
    {
        _asl(mode, address);
        _ora(mode, address);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _sre(AddressingMode mode, ushort address)
    {
        _lsr(mode, address);
        _eor(mode, address);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _sta(AddressingMode mode, ushort address)
    {
        _bus.WriteByte(address, _a);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _stp(AddressingMode mode, ushort address)
    {
        // stops program counter.
        _pc--; // STP instruction size is 1, so decrease by 1.
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _stx(AddressingMode mode, ushort address)
    {
        _bus.WriteByte(address, _x);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _sty(AddressingMode mode, ushort address)
    {
        _bus.WriteByte(address, _y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _tas(AddressingMode mode, ushort address)
    {
        _sp = (byte)(_a & _x);
        _bus.WriteByte(address, (byte)(_sp & ((address >> 8) + 1)));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _tax(AddressingMode mode, ushort address)
    {
        _x = _a;
        SetZeroAndNegativeFlags(_x);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _tay(AddressingMode mode, ushort address)
    {
        _y = _a;
        SetZeroAndNegativeFlags(_y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _tsx(AddressingMode mode, ushort address)
    {
        _x = _sp;
        SetZeroAndNegativeFlags(_x);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _txa(AddressingMode mode, ushort address)
    {
        _a = _x;
        SetZeroAndNegativeFlags(_a);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _txs(AddressingMode mode, ushort address)
    {
        _sp = _x;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _tya(AddressingMode mode, ushort address)
    {
        _a = _y;
        SetZeroAndNegativeFlags(_a);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _xaa(AddressingMode mode, ushort address)
    {
        var v = _bus.ReadByte(address);
        _a = (byte)((_a | 0xee) & _x & v);
        SetZeroAndNegativeFlags(_a);
    }

    #endregion Opcode Implementations   

    private void Branch(ushort address)
    {
        Cycles++;
        if(IsPageCrossed(_pc, address)) Cycles++;

        _pc = address;
    }

    /// <summary>
    /// 中断处理
    /// </summary>
    /// <param name="interrupt">中断类型</param>
    private void Interrupt(InterruptType interrupt)
    {
        // 将PC寄存器的值压入栈
        StackPushWord(_pc);

        if(interrupt == InterruptType.Brk)
            _flag.Bit4 = true;  // 若是BRK中断，置位标志寄存器的第4位

        // 将标志寄存器的值压入栈
        StackPushByte(_flag.Value);

        // 设置PC寄存器的值为中断向量地址
        _pc = interrupt switch
        {
            InterruptType.Brk => _bus.ReadWord(0xfffe),
            InterruptType.Irq => _bus.ReadWord(0xfffe),
            InterruptType.Nmi => _bus.ReadWord(0xfffa),
            _ => _pc
        };

        // 置位标志寄存器的I位
        if(interrupt != InterruptType.Brk)
            _flag.I = true;
    }

    /// <summary>
    /// 根据寻址模式解析出真实地址
    /// </summary>
    /// <param name="mode">寻址模式</param>
    /// <param name="address">地址</param>
    /// <param name="pageCrossed">在使用寄存器寻址时生成的地址是否跨页标志</param>
    /// <returns>真实地址</returns>
    /// <exception cref="NotSupportedException">未知寻址模式</exception> 
    private ushort ResolveAddress(AddressingMode mode, ushort address, out bool pageCrossed)
    {
        pageCrossed = false;
        ushort result = 0;
        byte hi, lo;
        switch(mode)
        {
            case AddressingMode.Accumulator:
            case AddressingMode.Implicit:
                break;

            case AddressingMode.Relative:
                result = (ushort)(_pc + (sbyte)_bus.ReadByte(address) + 2);
                break;

            case AddressingMode.Immediate:
                result = address;
                break;

            case AddressingMode.ZeroPage:
                result = _bus.ReadByte(address);
                break;

            case AddressingMode.ZeroPageX:
                result = (_bus.ReadByte(address) + _x).WrapToByte( );
                break;

            case AddressingMode.ZeroPageY:
                result = (_bus.ReadByte(address) + _y).WrapToByte( );
                break;

            case AddressingMode.Absolute:
                result = _bus.ReadWord(address);
                break;

            case AddressingMode.AbsoluteX:
                result = (_bus.ReadWord(address) + _x).WrapToWord( );
                pageCrossed = IsPageCrossed((ushort)(address - _x), _x);
                break;

            case AddressingMode.AbsoluteY:
                result = (_bus.ReadWord(address) + _y).WrapToWord( );
                pageCrossed = IsPageCrossed((ushort)(address - _y), _y);
                break;

            case AddressingMode.Indirect:
                var pointer = _bus.ReadWord(address);
                // NES的硬件bug，间接寻址时地址无法跨页
                var hiAddress = (ushort)((pointer & 0xff) == 0xff ? pointer - 0xff : pointer + 1);
                result = ((_bus.ReadByte(hiAddress) << 8) | _bus.ReadByte(pointer)).WrapToWord( );
                break;

            case AddressingMode.IndexedIndirect:
                pointer = (_bus.ReadByte(address) + _x).WrapToByte( );
                lo = _bus.ReadByte(pointer);
                hi = _bus.ReadByte((pointer + 1).WrapToByte( ));
                result = (ushort)((hi << 8) | lo);
                break;

            case AddressingMode.IndirectIndexed:
                var baseAddress = _bus.ReadByte(address);
                lo = _bus.ReadByte(baseAddress);
                hi = _bus.ReadByte((baseAddress + 1).WrapToByte( ));
                pointer = (ushort)((hi << 8) | lo);
                result = (pointer + _y).WrapToWord( );
                pageCrossed = IsPageCrossed((ushort)(address - _y), address);
                break;

            default:
                throw new NotSupportedException($"{mode} 不支持。");
        }

        return result;
    }

    /// <summary>
    /// 根据值设置零标志位和负标志位
    /// </summary>
    private void SetZeroAndNegativeFlags(byte value)
    {
        _flag.Z = value == 0;
        _flag.N = Bit.HasSet(value, 7);
    }

    /// <summary>
    /// 从栈中弹出一个字节
    /// </summary>
    private byte StackPullByte( )
    {
        _sp++;
        return _bus.ReadByte((ushort)(0x100 | _sp));
    }

    /// <summary>
    /// 从栈中弹出一个字
    /// </summary>
    private ushort StackPullWord( )
    {
        var lo = StackPullByte( );
        var hi = StackPullByte( );
        return (ushort)((hi << 8) | lo);
    }

    /// <summary>
    /// 将一个字节压入栈
    /// </summary>
    private void StackPushByte(byte value)
    {
        //6502堆栈是一个递减堆栈（在按下时减小SP，在弹出时增大SP）。
        //6502堆栈使用空堆栈，这意味着SP将在按下后和弹出前移动。
        _bus.WriteByte((ushort)(0x100 | _sp), value);
        _sp--;
    }

    /// <summary>
    /// 将一个字压入栈
    /// </summary>
    private void StackPushWord(ushort value)
    {
        var hi = (byte)(value >> 8);
        var lo = (byte)(value & 0xff);
        StackPushByte(hi);
        StackPushByte(lo);
    }

    #endregion Private Methods
}


