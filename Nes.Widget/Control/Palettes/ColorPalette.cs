// ============================================================================
using Color = System.Windows.Media.Color;

namespace Nes.Widget.Control.Palettes;

/// <summary>
/// 调色板基类
/// </summary>
internal abstract class ColorPalette
{
    private static readonly Dictionary<string, ColorPalette> m_palettes = new( )
    {
        { "Default", new DefaultPalette() },
        { "Default Grayscale", new DefaultGrayscale() },
        { "2C02_aps_ela_default", new _2C02_aps_ela_default() },
        { "2C02-2C07_aps_ela_persune_neutral", new _2C02_2C07_aps_ela_persune_neutral() },
        { "2C02G_aps_ela_NTSC_persune_GVUSB2_NTSC_M", new _2C02G_aps_ela_NTSC_persune_GVUSB2_NTSC_M() },
        { "2C02G_aps_ela_NTSC_persune_GVUSB2_NTSC_M_J", new _2C02G_aps_ela_NTSC_persune_GVUSB2_NTSC_M_J() },
        { "2C02G_aps_ela_NTSC_persune_tink", new _2C02G_aps_ela_NTSC_persune_tink() },
        { "2C02G_phs_aps_ela_NTSC", new _2C02G_phs_aps_ela_NTSC() },
        { "2C02G_phs_aps_ela_NTSC_1953", new _2C02G_phs_aps_ela_NTSC_1953() },
        { "2C02G_phs_aps_ela_NTSC-J", new _2C02G_phs_aps_ela_NTSC_J() },
        { "2C03_DeMarsh_1980s_RGB", new _2C03_DeMarsh_1980s_RGB() },
        { "2C05-99_composite_forple", new _2C05_99_composite_forple() },
        { "2C07_ela_PAL", new _2C07_ela_PAL() },
    };

    /// <summary>
    /// 获取调色板颜色
    /// </summary>
    protected abstract Color[] GetPaletteColors( );

    /// <summary>
    /// 调色板颜色
    /// </summary>
    public Color[] Colors { get => GetPaletteColors( ); }

    /// <summary>
    /// 获取所有调色板
    /// </summary>
    public static IEnumerable<KeyValuePair<string, ColorPalette>> Palettes { get => m_palettes; }

    /// <summary>
    /// 根据名称获取调色板
    /// </summary>
    /// <param name="name">调色板名称</param>
    public static ColorPalette GetColorPaletteByName(string name) => m_palettes[name];
}