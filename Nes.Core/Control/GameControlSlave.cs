//using Nes.Core;
//using Nes.Core.Control.Palettes;
//using Nes.Widget.Models;
//using System.IO;

//namespace Nes.Core.Control;

///// <summary>
///// 从机游戏控制器
///// </summary>
//internal class GameControlSlave : GameControl
//{
//    public override event EventHandler? GameOpened;
//    public override event EventHandler? GameStopped;
//    public override event EventHandler? GameDrawFrame;

//    public override NesFileInfo? GameName { get; set; }

//    public override bool IsGameRunning { get; protected set; }

//    public override GameControlType Type => GameControlType.Salve;

//    public override byte[] Pixels => new byte[256 * 240 * 4];

//    public override ColorPalette SelectedColorPalette { get; set; }

//    public GameControlSlave( )
//    {
//        SelectedColorPalette = ColorPalette.GetColorPaletteByName("Default");
//    }

//    public override void OpenGame(string fileName)
//    {
//        throw new NotImplementedException( );
//    }

//    public override void PauseGame( )
//    {
//        throw new NotImplementedException( );
//    }

//    public override void ResetGame( )
//    {
//        throw new NotImplementedException( );
//    }

//    public override void ResumeGame( )
//    {
//        throw new NotImplementedException( );
//    }

//    public override void SetButtonState(int Px, Controller.Buttons btn, bool state)
//    {
//        throw new NotImplementedException( );
//    }

//    public override void StopGame( )
//    {
//        throw new NotImplementedException( );
//    }

//    public override void Save(BinaryWriter writer)
//    {
//        throw new NotImplementedException( );
//    }

//    public override void Load(BinaryReader reader)
//    {
//        throw new NotImplementedException( );
//    }
//}
