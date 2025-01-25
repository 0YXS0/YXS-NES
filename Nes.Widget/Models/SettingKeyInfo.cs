using System.Windows.Input;

namespace Nes.Console.Models;

/// <summary>
/// 控制键
/// </summary>
public class ControlKey
{
    /// <summary>
    /// 将按键转换为字符串
    /// </summary>
    /// <param name="key">按键值</param>
    /// <returns>按键值对应的字符串</returns>
    public static string KeyTypeToString(KeyType key)
    {
        return key switch
        {
            KeyType.MouseLeft => "鼠标左键",
            KeyType.MouseMiddle => "鼠标中键",
            KeyType.MouseRight => "鼠标右键",
            KeyType.Space => "空格键",
            KeyType.A => "按键A",
            KeyType.B => "按键B",
            KeyType.C => "按键C",
            KeyType.D => "按键D",
            KeyType.E => "按键E",
            KeyType.F => "按键F",
            KeyType.G => "按键G",
            KeyType.H => "按键H",
            KeyType.I => "按键I",
            KeyType.J => "按键J",
            KeyType.K => "按键K",
            KeyType.L => "按键L",
            KeyType.M => "按键M",
            KeyType.N => "按键N",
            KeyType.O => "按键O",
            KeyType.P => "按键P",
            KeyType.Q => "按键Q",
            KeyType.R => "按键R",
            KeyType.S => "按键S",
            KeyType.T => "按键T",
            KeyType.U => "按键U",
            KeyType.V => "按键V",
            KeyType.W => "按键W",
            KeyType.X => "按键X",
            KeyType.Y => "按键Y",
            KeyType.Z => "按键Z",
            _ => string.Empty,
        };
    }

    public static KeyType ToKeyType(Key key)
    {
        return key switch
        {
            >= Key.A and <= Key.Z => (KeyType)key,
            Key.Space => KeyType.Space,
            _ => KeyType.None,
        };
    }

    public static KeyType ToKeyType(MouseButton button)
    {
        return button switch
        {
            MouseButton.Left => KeyType.MouseLeft,
            MouseButton.Middle => KeyType.MouseMiddle,
            MouseButton.Right => KeyType.MouseRight,
            _ => KeyType.None,
        };
    }

    public static KeyType ToKeyType(string str)
    {
        return str switch
        {
            "鼠标左键" => KeyType.MouseLeft,
            "鼠标中键" => KeyType.MouseMiddle,
            "鼠标右键" => KeyType.MouseRight,
            "空格键" => KeyType.Space,
            "按键A" => KeyType.A,
            "按键B" => KeyType.B,
            "按键C" => KeyType.C,
            "按键D" => KeyType.D,
            "按键E" => KeyType.E,
            "按键F" => KeyType.F,
            "按键G" => KeyType.G,
            "按键H" => KeyType.H,
            "按键I" => KeyType.I,
            "按键J" => KeyType.J,
            "按键K" => KeyType.K,
            "按键L" => KeyType.L,
            "按键M" => KeyType.M,
            "按键N" => KeyType.N,
            "按键O" => KeyType.O,
            "按键P" => KeyType.P,
            "按键Q" => KeyType.Q,
            "按键R" => KeyType.R,
            "按键S" => KeyType.S,
            "按键T" => KeyType.T,
            "按键U" => KeyType.U,
            "按键V" => KeyType.V,
            "按键W" => KeyType.W,
            "按键X" => KeyType.X,
            "按键Y" => KeyType.Y,
            "按键Z" => KeyType.Z,
            _ => KeyType.None,
        };
    }

    /// <summary>
    /// 按键类型
    /// </summary>
    public enum KeyType
    {
        MouseLeft = 0,  // 鼠标左键
        MouseMiddle = 1,    // 鼠标中键
        MouseRight = 2, // 鼠标右键
        Space = 18,  // 空格键
        A = 44,
        B,
        C,
        D,
        E,
        F,
        G,
        H,
        I,
        J,
        K,
        L,
        M,
        N,
        O,
        P,
        Q,
        R,
        S,
        T,
        U,
        V,
        W,
        X,
        Y,
        Z = 69,
        None = 0xFF,
    }
}
