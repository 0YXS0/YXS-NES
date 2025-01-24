//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Xml.Schema;
//using System.Xml.Serialization;

//namespace NesEmu.Console.Models
//{
//    [XmlRoot]
//    public sealed class ConsoleSettings
//    {
//        public static readonly ConsoleSettings Default = new ConsoleSettings
//        {
//            SelectedViewMode = 2,
//            SelectedColorPalette = "Default",
//            Controllers =
//            [
//                ControllerSetting.Default, 
//            ]
//        };

//        public int SelectedViewMode { get; set; }

//        [XmlElement]
//        public string? SelectedColorPalette { get; set; }

//        [XmlArray]
//        [XmlArrayItem("Controller")]
//        public List<ControllerSetting>? Controllers { get; set; }

//        private const string FileName = "settings.xml";

//        public static void Save(ConsoleSettings settings)
//        {
//            var xmlSerializer = new XmlSerializer(typeof(ConsoleSettings));
//            using var fileStream = new FileStream(FileName, FileMode.Create, FileAccess.Write);
//            xmlSerializer.Serialize(fileStream, settings);
//        }

//        public static ConsoleSettings Load()
//        {
//            if (!File.Exists(FileName))
//            {
//                return Default;
//            }

//            using var fileStream = new FileStream(FileName, FileMode.Open, FileAccess.Read);
//            var xmlSerializer = new XmlSerializer(typeof(ConsoleSettings));
//            if (xmlSerializer.Deserialize(fileStream) is ConsoleSettings settings)
//            {
//                return settings;
//            }

//            return Default;
//        }
//    }
//}
