﻿using Nes.Core.Mappers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Nes.Core;

/// <summary>
/// NES仿真器
/// </summary>
public class Emulator
{
    #region Public Events

    /// <summary>
    /// 画帧事件
    /// </summary>
    public event EventHandler<byte[]>? DrawFrame;

    #endregion Public Events

    #region Internal Methods

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void OnDrawFrame(byte[] frameBitmapData)
    {
        DrawFrame?.Invoke(this, frameBitmapData);
        _frameflip = !_frameflip;
    }

    #endregion Internal Methods

    #region Private Fields

    /// <summary>
    /// 已注册的Mapper类型
    /// </summary>
    private static readonly List<MapperRegistry> _registeredMapperTypes =
    [
        new MapperRegistry(0x00, "NROM", e => new NRomMapper(e)),
        new MapperRegistry(0x01, "MMC1", e => new MMC1Mapper(e)),
        new MapperRegistry(0x02, "UxROM", e => new UxRomMapper(e)),
        new MapperRegistry(0x03, "CnROM", e => new CnRomMapper(e)),
        new MapperRegistry(0x04, "MMC3", e => new MMC3Mapper(e)),
    ];

    private readonly Lazy<Bus> _bus;

    private readonly Lazy<Controller> _controller;

    private readonly Lazy<Cpu> _cpu;

    private readonly Lazy<Ppu> _ppu;

    private readonly Lazy<Apu> _apu;

    private bool _frameflip;

    private Mapper? _mapper;

    #endregion Private Fields

    #region Public Constructors

    public Emulator( ) : this(e => new Cpu(e), e => new Bus(e), e => new Ppu(e), ( ) => new Controller( ), ( ) => new Apu( ))
    {
    }

    public Emulator(Func<Emulator, Cpu> cpuResolver, Func<Emulator, Bus> busResolver)
        : this(cpuResolver, busResolver, e => new Ppu(e), ( ) => new Controller( ), ( ) => new Apu( ))
    {
    }

    public Emulator(Func<Emulator, Cpu> cpuResolver,
        Func<Emulator, Bus> busResolver,
        Func<Emulator, Ppu> ppuResolver,
        Func<Controller> controllerResolver,
        Func<Apu> apuResolver)
    {
        _cpu = new Lazy<Cpu>(( ) => cpuResolver(this));
        _bus = new Lazy<Bus>(( ) => busResolver(this));
        _ppu = new Lazy<Ppu>(( ) => ppuResolver(this));
        _apu = new Lazy<Apu>(apuResolver);
        _controller = new Lazy<Controller>(controllerResolver);

        Apu.SampleRate = 44100;  // 音频采样率
        //设置DMC读取内存的方法
        Apu.Dmc.ReadMemorySample = (address) =>
        {
            Cpu.AddIdleCycles(4);
            return Bus.ReadByte(address);
        };
        //处理APU中断请求
        Apu.TriggerInterruptRequest = Cpu.TriggerIrqInterrupt;
        Apu.Dmc.TriggerInterruptRequest = Cpu.TriggerIrqInterrupt;
    }

    #endregion Public Constructors

    #region Public Properties

    public Bus Bus => _bus.Value;

    public Controller Controller => _controller.Value;

    public Cpu Cpu => _cpu.Value;

    public Cartridge? InstalledCartridge { get; private set; }

    public Mapper Mapper => _mapper ??
                            throw new InvalidOperationException("映射器未初始化，请确保已成功读取卡带文件。");

    public Ppu Ppu => _ppu.Value;

    public Apu Apu => _apu.Value;

    public bool IsPaused { get; set; }

    #endregion Public Properties

    #region Public Methods

    public static IEnumerable<MapperRegistry> GetRegisteredMapperInfo( ) => _registeredMapperTypes;

    /// <summary>
    /// 反汇编代码
    /// </summary>
    /// <returns>反汇编后的代码</returns>
    /// <exception cref="InvalidOperationException">未安装卡带</exception>
    public string Disassemble( )
    {
        if(InstalledCartridge is null)
            throw new InvalidOperationException("未安装卡带。");

        Cpu.Reset( );
        Bus.Reset( );
        var nmiVector = Bus.ReadWord(0xfffa);
        var irqVector = Bus.ReadWord(0xfffe);
        var sb = new StringBuilder( );
        sb.AppendLine($"; NMI vector:   ${nmiVector:x4}");
        sb.AppendLine($"; RESET vector: ${Cpu.PC:x4}");
        sb.AppendLine($"; IRQ vector:   ${irqVector:x4}");
        sb.AppendLine( );
        var baseAddress = 0x8000 + InstalledCartridge.PrgRom.Length % 0x8000;
        if(Cpu.PC < baseAddress) baseAddress = 0x8000;

        var programCode = InstalledCartridge.PrgRom;
        var pc = 0;
        byte? prevCode = default;
        while(pc < programCode.Length)
        {
            var opcode = programCode[pc];
            if(opcode == prevCode && opcode == 0) break;

            prevCode = opcode;
            var instructionSize = Cpu.GetInstructionSize(opcode);
            var offset = (ushort)(pc + baseAddress);
            var instruction = new byte[instructionSize];
            Array.Copy(programCode, pc, instruction, 0, instructionSize);
            sb.AppendLine(Cpu.Disassemble(offset, instruction));
            pc += instructionSize;
        }

        return sb.ToString( );
    }


    /// <summary>
    /// 模拟器模拟一帧画面
    /// </summary>
    public void ExecuteStep( )
    {
        var orig = _frameflip;
        while(orig == _frameflip)
        {
            var cpuCycles = Cpu.Step( );
            var ppuCycles = cpuCycles * 3;

            for(var i = 0; i < ppuCycles; i++)
            {
                Ppu.Step( );
            }

            for(var i = 0; i < cpuCycles; i++)
            {
                Apu.Step( );
            }
        }
    }

    /// <summary>
    /// 重置模拟器
    /// </summary>
    public void Reset( )
    {
        _frameflip = false;
        Cpu.Reset( );   // 重置CPU
        Bus.Reset( );   // 重置总线
        Ppu.Reset( );   // 重置PPU
    }

    /// <summary>
    /// 安装游戏卡带
    /// </summary>
    /// <param name="cartridge">卡带</param>
    /// <exception cref="NotSupportedException">模拟器不支持当前卡带</exception>
    public void InstallCartridge(Cartridge cartridge)
    {
        InstalledCartridge = cartridge;
        var mapperRegistry = _registeredMapperTypes.FirstOrDefault(r => r.Number == InstalledCartridge.MapperNum)
                             ?? throw new NotSupportedException(
                                 $"不支持当前Mapper{InstalledCartridge.MapperNum}");
        _mapper = mapperRegistry.Factory(this);
    }

    /// <summary>
    /// 检查是否支持指定的Mapper
    /// </summary>
    /// <param name="mapperNumber">Mapper号</param>
    /// <returns></returns>
    public static bool IsMapperSupported(int mapperNumber) => _registeredMapperTypes.Any(r => r.Number == mapperNumber);

    /// <summary>
    /// 打开NES游戏文件
    /// </summary>
    /// <param name="nesFilePath">NES文件路径</param>
    public void Open(string nesFilePath)
    {
        InstallCartridge(new Cartridge(nesFilePath)); // 安装游戏卡带
        Reset( );   // 重置模拟器
    }

    /// <summary>
    /// 移除游戏卡带
    /// </summary>
    public void RemoveCartridge( )
    {
        InstalledCartridge = null;
        _mapper = null;
    }

    /// <summary>
    /// 存档
    /// </summary>
    public void Save(BinaryWriter writer)
    {
        writer.Write(_frameflip);
        InstalledCartridge?.Save(writer);
        Mapper.Save(writer);
        Controller.Save(writer);
        Cpu.Save(writer);
        Bus.Save(writer);
        Ppu.Save(writer);
    }

    /// <summary>
    /// 读档
    /// </summary>
    public void Load(BinaryReader reader)
    {
        _frameflip = reader.ReadBoolean( );
        InstalledCartridge?.Load(reader);
        Mapper.Load(reader);
        Controller.Load(reader);
        Cpu.Load(reader);
        Bus.Load(reader);
        Ppu.Load(reader);
    }

    #endregion Public Methods
}