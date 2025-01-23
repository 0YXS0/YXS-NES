// ============================================================================
//  _ __   ___  ___  ___ _ __ ___  _   _
// | '_ \ / _ \/ __|/ _ \ '_ ` _ \| | | |
// | | | |  __/\__ \  __/ | | | | | |_| |
// |_| |_|\___||___/\___|_| |_| |_|\__,_|
//
// NES Emulator by daxnet, 2024
// MIT License
// ============================================================================

namespace NesEmu.Core;

public enum TvSystem
{
    // ReSharper disable once InconsistentNaming
    NTSC = 0,

    // ReSharper disable once InconsistentNaming
    PAL,

    Unknown = 0xff
}