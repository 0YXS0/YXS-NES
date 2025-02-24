using System;

namespace Nes.Core;

public sealed class DrawFrameEventArgs(byte[] bitmapData) : EventArgs
{
    #region Public Properties

    public byte A { get; init; }
    public byte[] BitmapData { get; } = bitmapData;
    public CpuFlags CpuFlags { get; init; }
    public ushort PC { get; init; }
    public ushort PpuAddress { get; init; }
    public byte PpuControl { get; init; }
    public byte PpuMask { get; init; }
    public byte PpuScroll { get; init; }
    public byte PpuStatus { get; init; }
    public byte SP { get; init; }
    public byte X { get; init; }

    public byte Y { get; init; }

    #endregion Public Properties
}