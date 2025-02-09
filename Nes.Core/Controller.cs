// ============================================================================
//  _ __   ___  ___  ___ _ __ ___  _   _
// | '_ \ / _ \/ __|/ _ \ '_ ` _ \| | | |
// | | | |  __/\__ \  __/ | | | | | |_| |
// |_| |_|\___||___/\___|_| |_| |_|\__,_|
//
// NES Emulator by daxnet, 2024
// MIT License
// ============================================================================

using System.IO;

namespace Nes.Core;

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

    private readonly bool[,] _buttonStates = new bool[2, 8];

    private readonly byte[] _buttonIndex = new byte[2];

    private bool _strobe = true;

    #endregion Private Fields

    #region Public Methods

    public byte ReadControllerInput(int Px)
    {
        if(Px != 1 && Px != 2) return 0;
        if(_buttonIndex[Px - 1] > 7) return 1;

        var state = _buttonStates[Px - 1, _buttonIndex[Px - 1]];
        if(!_strobe) _buttonIndex[Px - 1]++;

        return (byte)(state ? 1 : 0);
    }

    public void SetButtonState(int Px, Buttons button, bool state)
    {
        if(Px != 1 && Px != 2) return;
        if((int)button < 0 || (int)button > 7) return;
        _buttonStates[Px - 1, (int)button] = state;
    }

    public void WriteControllerInput(byte data)
    {
        _strobe = (data & 1) == 1;
        if(_strobe)
        {
            _buttonIndex[0] = 0;
            _buttonIndex[1] = 0;
        }
    }

    public void Save(BinaryWriter writer)
    {
        writer.Write(_strobe);
        writer.Write(_buttonIndex);
    }

    public void Load(BinaryReader reader)
    {
        _strobe = reader.ReadBoolean( );
        reader.Read(_buttonIndex);
    }

    #endregion Public Methods
}