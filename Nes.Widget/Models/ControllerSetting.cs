//using System.Xml.Serialization;

//namespace NesEmu.Console.Models;

//public sealed class ControllerSetting
//{
//    public static readonly ControllerSetting Default = new()
//    {
//        Name = "Controller_1",
//        A = "key.K",
//        B = "key.J",
//        Select = "key.B",
//        Start = "key.N",
//        Up = "key.W",
//        Down = "key.S",
//        Left = "key.A",
//        Right = "key.D"
//    };

//    [XmlAttribute]
//    public string Name { get; set; } = string.Empty;

//    [XmlElement] 
//    public string A { get; set; } = string.Empty;

//    [XmlElement] 
//    public string B { get; set; } = string.Empty;

//    [XmlElement] 
//    public string Down { get; set; } = string.Empty;

//    [XmlElement] 
//    public string Left { get; set; } = string.Empty;

//    [XmlElement] 
//    public string Right { get; set; } = string.Empty;

//    [XmlElement] 
//    public string Select { get; set; } = string.Empty;

//    [XmlElement] 
//    public string Start { get; set; } = string.Empty;

//    [XmlElement] 
//    public string Up { get; set; } = string.Empty;
//}