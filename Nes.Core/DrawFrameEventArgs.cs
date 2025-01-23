// ============================================================================
//  _ __   ___  ___  ___ _ __ ___  _   _
// | '_ \ / _ \/ __|/ _ \ '_ ` _ \| | | |
// | | | |  __/\__ \  __/ | | | | | |_| |
// |_| |_|\___||___/\___|_| |_| |_|\__,_|
//
// NES Emulator by daxnet, 2024
// MIT License
// ============================================================================

using System;

namespace NesEmu.Core;

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