using System.Drawing;
using MelonLoader.Logging;
using Pastel;

namespace MelonLoader.Bootstrap.Logging;

internal static class PastelExtensions
{
    private static string _colorReset = new ReadOnlySpan<char>().Pastel(Color.FromArgb(255, 211, 211, 211));

    internal static string Pastel(this string input, ColorARGB color)
    {
        return input.AsSpan().Pastel(color);
    }

    internal static string Pastel(in this ReadOnlySpan<char> input, ColorARGB color)
    {
        // TODO: loader config option to disable color
        return input.Pastel(Color.FromArgb(color.R, color.G, color.B)) + _colorReset;
    }
}
