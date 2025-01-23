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

public sealed class Controller
{
    #region Public Enums

    public enum Buttons
    {
        A = 0,
        B,
        Select,
        Start,
        Up,
        Down,
        Left,
        Right
    }

    #endregion Public Enums

    #region Private Fields

    private readonly bool[] _buttonStates = new bool[8];

    private int _buttonIndex;

    private bool _strobe;

    #endregion Private Fields

    #region Public Methods

    public byte ReadControllerInput( )
    {
        if(_buttonIndex > 7) return 1;

        var state = _buttonStates[_buttonIndex];
        if(!_strobe) _buttonIndex++;

        return (byte)(state ? 1 : 0);
    }

    public void SetButtonState(Buttons button, bool state)
    {
        _buttonStates[(int)button] = state;
    }

    public void WriteControllerInput(byte data)
    {
        _strobe = (data & 1) == 1;
        if(_strobe) _buttonIndex = 0;
    }

    #endregion Public Methods
}