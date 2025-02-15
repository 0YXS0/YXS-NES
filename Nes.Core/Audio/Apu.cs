using Nes.Core.Audio.Filtering;
using Nes.Core.Audio.Generators;
using System;
using System.IO;

namespace Nes.Core;

public class Apu
{
    private float sampleRate;
    private ulong cycle;
    private byte framePeriod;
    private byte frameValue;
    private bool frameIrq;
    private readonly FilterChain filterChain;   // 过滤器链

    private static readonly float[] pulseTable = new float[31];
    private static readonly float[] tndTable = new float[203];

    private const uint CpuFrequency = 1789773;
    private const double FrameCounterRate = CpuFrequency / 240.0;

    /// <summary>
    /// 用于写入输出样本的处理程序
    /// </summary>
    public Action<float>? WriteSample { get; set; }

    /// <summary>
    /// 用于触发中断请求的处理程序
    /// </summary>
    public Action? TriggerInterruptRequest { get; set; }

    public Apu( )
    {
        Pulse1 = new PulseGenerator(1);
        Pulse2 = new PulseGenerator(2);
        Triangle = new TriangleGenerator( );
        Noise = new NoiseGenerator( );
        Dmc = new DmcGenerator( );

        filterChain = new FilterChain( );
    }

    public byte ReadRegister(ushort address)
    {
        return address switch
        {
            0x4015 => Status,
            _ => 0,
        };
    }

    public void WriteRegister(ushort address, byte value)
    {
        switch(address)
        {
            case 0x4000:
                Pulse1.Control = value;
                break;
            case 0x4001:
                Pulse1.Sweep = value;
                break;
            case 0x4002:
                Pulse1.TimerLow = value;
                break;
            case 0x4003:
                Pulse1.TimerHigh = value;
                break;
            case 0x4004:
                Pulse2.Control = value;
                break;
            case 0x4005:
                Pulse2.Sweep = value;
                break;
            case 0x4006:
                Pulse2.TimerLow = value;
                break;
            case 0x4007:
                Pulse2.TimerHigh = value;
                break;
            case 0x4008:
                Triangle.Control = value;
                break;
            case 0x400A:
                Triangle.TimerLow = value;
                break;
            case 0x400B:
                Triangle.TimerHigh = value;
                break;
            case 0x400C:
                Noise.Control = value;
                break;
            case 0x400E:
                Noise.ModeAndPeriod = value;
                break;
            case 0x400F:
                Noise.Length = value;
                break;
            case 0x4010:
                Dmc.Control = value;
                break;
            case 0x4011:
                Dmc.SampleValue = value;
                break;
            case 0x4012:
                Dmc.SampleAddress = value;
                break;
            case 0x4013:
                Dmc.SampleLength = value;
                break;
            case 0x4015:
                Control = value;
                break;
            case 0x4017:
                FrameCounter = value;
                break;
            default:
                // Handle other cases or do nothing
                break;
        }
    }

    /// <summary>
    /// Status register
    /// </summary>
    public byte Status
    {
        get
        {
            byte result = 0;
            if(Pulse1.LengthValue > 0)
                result |= 1;
            if(Pulse2.LengthValue > 0)
                result |= 2;
            if(Triangle.LengthValue > 0)
                result |= 4;
            if(Noise.LengthValue > 0)
                result |= 8;
            if(Dmc.CurrentLength > 0)
                result |= 16;
            return result;
        }
    }

    /// <summary>
    /// Control register
    /// </summary>
    public byte Control
    {
        set
        {
            Pulse1.Enabled = (value & 1) == 1;
            Pulse2.Enabled = (value & 2) == 2;
            Triangle.Enabled = (value & 4) == 4;
            Noise.Enabled = (value & 8) == 8;
            Dmc.Enabled = (value & 16) == 16;

            if(!Pulse1.Enabled)
                Pulse1.LengthValue = 0;

            if(!Pulse2.Enabled)
                Pulse2.LengthValue = 0;

            if(!Triangle.Enabled)
                Triangle.LengthValue = 0;

            if(!Noise.Enabled)
                Noise.LengthValue = 0;

            if(!Dmc.Enabled)
            {
                Dmc.CurrentLength = 0;
            }
            else // DMC enabled
            {
                if(Dmc.CurrentLength == 0)
                    Dmc.Restart( );
            }
        }
    }

    public byte FrameCounter
    {
        set
        {
            framePeriod = (byte)(4 + ((value >> 7) & 1));
            frameIrq = ((value >> 6) & 1) == 0;

            // clock immediatly when $80 is written to frame counter port
            if(value == 0x80)
            {
                if(Pulse1.LengthEnabled)
                    Pulse1.LengthValue = 0;

                if(Pulse2.LengthEnabled)
                    Pulse2.LengthValue = 0;

                if(Triangle.LengthEnabled)
                    Triangle.LengthValue = 0;

                if(Noise.LengthEnabled)
                    Noise.LengthValue = 0;

                Dmc.CurrentLength = 0;
            }
        }
    }

    /// <summary>
    /// Sets the supported sample rate and configures the pass filters accordingly
    /// </summary>
    public float SampleRate
    {
        get { return sampleRate; }
        set
        {
            sampleRate = CpuFrequency / value;

            filterChain.Filters.Clear( );
            filterChain.Filters.Add(FirstOrderFilter.CreateHighPassFilter(value, 90f));
            filterChain.Filters.Add(FirstOrderFilter.CreateHighPassFilter(value, 440f));
            filterChain.Filters.Add(FirstOrderFilter.CreateLowPassFilter(value, 14000f));
        }
    }

    /// <summary>
    /// Current output
    /// </summary>
    public float Output
    {
        get
        {
            byte pulseOutput1 = Pulse1.Output;
            byte pulseOutput2 = Pulse2.Output;
            float pulseOutput = pulseTable[pulseOutput1 + pulseOutput2];

            byte triangleOutput = Triangle.Output;
            byte noiseOutput = Noise.Output;
            byte dmcOutput = Dmc.Output;
            float tndOutput = tndTable[3 * triangleOutput + 2 * noiseOutput + dmcOutput];

            return pulseOutput + tndOutput;
        }
    }

    // wave generators

    public PulseGenerator Pulse1 { get; private set; }
    public PulseGenerator Pulse2 { get; private set; }
    public TriangleGenerator Triangle { get; private set; }
    public NoiseGenerator Noise { get; private set; }
    public DmcGenerator Dmc { get; private set; }

    public void Step( )
    {
        ulong lastCycle = cycle;
        ++cycle;
        ulong nextCycle = cycle;

        StepTimer( );

        int lastCycleFrame = (int)((double)lastCycle / FrameCounterRate);
        int nextCycleFrame = (int)((double)nextCycle / FrameCounterRate);

        if(lastCycleFrame != nextCycleFrame)
            StepFrameCounter( );

        int lastCycleSample = (int)((double)lastCycle / SampleRate);
        int nextCycleSample = (int)((double)nextCycle / SampleRate);

        if(lastCycleSample != nextCycleSample)
        {
            float filteredOutput = filterChain.Apply(Output);
            WriteSample?.Invoke(filteredOutput);
        }
    }

    public void Save(BinaryWriter writer)
    {
        writer.Write(cycle);
        writer.Write(framePeriod);
        writer.Write(frameValue);
        writer.Write(frameIrq);

        Pulse1.SaveState(writer);
        Pulse2.SaveState(writer);
        Triangle.SaveState(writer);
        Noise.SaveState(writer);
        Dmc.SaveState(writer);
    }

    public void Load(BinaryReader reader)
    {
        cycle = reader.ReadUInt64( );
        framePeriod = reader.ReadByte( );
        frameValue = reader.ReadByte( );
        frameIrq = reader.ReadBoolean( );

        Pulse1.LoadState(reader);
        Pulse2.LoadState(reader);
        Triangle.LoadState(reader);
        Noise.LoadState(reader);
        Dmc.LoadState(reader);
    }

    private void StepTimer( )
    {
        if(cycle % 2 == 0)
        {
            Pulse1.StepTimer( );
            Pulse2.StepTimer( );
            Noise.StepTimer( );
            Dmc.StepTimer( );
        }
        Triangle.StepTimer( );
    }

    private void StepFrameCounter( )
    {
        if(framePeriod == 4)
        {
            ++frameValue;
            frameValue %= 4;

            StepEnvelope( );

            if(frameValue == 1)
            {
                StepSweep( );
                StepLength( );
            }
            else if(frameValue == 3)
            {
                StepSweep( );
                StepLength( );
                if(frameIrq)
                    TriggerInterruptRequest?.Invoke( );
            }
        }
        else if(framePeriod == 5)
        {
            ++frameValue;
            frameValue %= 5;

            if(frameValue != 4)
                StepEnvelope( );

            if(frameValue == 0 || frameValue == 2)
            {
                StepSweep( );
                StepLength( );
            }
        }
    }

    private void StepEnvelope( )
    {
        Pulse1.StepEnvelope( );
        Pulse2.StepEnvelope( );
        Triangle.StepCounter( );
        Noise.StepEnvelope( );
    }

    private void StepSweep( )
    {
        Pulse1.StepSweep( );
        Pulse2.StepSweep( );
    }

    private void StepLength( )
    {
        Pulse1.StepLength( );
        Pulse2.StepLength( );
        Triangle.StepLength( );
        Noise.StepLength( );
    }


    /// <summary>
    /// Builds the pulse and triangle/noise/dmc (tnd) tables
    /// </summary>
    static Apu( )
    {
        for(int i = 0; i < 31; i++)
            pulseTable[i] = 95.52f / (8128.0f / (float)i + 100.0f);

        for(int i = 0; i < 203; i++)
            tndTable[i] = 163.67f / (24329.0f / (float)i + 100.0f);
    }
}
