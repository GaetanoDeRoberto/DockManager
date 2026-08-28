using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace DockManager;

public partial class ColorPickerWindow : Window
{
    public Color SelectedColor { get; private set; }

    private double hue = 0;
    private double saturation = 1;
    private double value = 1;

    private bool updating;

    public ColorPickerWindow(
        string initialColor)
    {
        InitializeComponent();

        if (!TryParseColor(
                initialColor,
                out Color color))
        {
            color = Colors.DarkGray;
        }

        SelectedColor = color;

        RgbToHsv(
            color,
            out hue,
            out saturation,
            out value);

        UpdateColorArea();
        UpdateColor();
    }

    // =====================================================
    // SELEZIONE TONALITÀ
    // =====================================================

    private void HueBar_MouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        UpdateHue(e);
        HueBar.CaptureMouse();
    }

    private void HueBar_MouseMove(
        object sender,
        MouseEventArgs e)
    {
        if (HueBar.IsMouseCaptured &&
            e.LeftButton == MouseButtonState.Pressed)
        {
            UpdateHue(e);
        }
    }

    private void HueBar_MouseLeftButtonUp(
        object sender,
        MouseButtonEventArgs e)
    {
        HueBar.ReleaseMouseCapture();
    }

    private void UpdateHue(
        MouseEventArgs e)
    {
        Point position =
            e.GetPosition(HueBar);

        double y =
            Math.Clamp(
                position.Y,
                0,
                HueBar.ActualHeight);

        hue =
            y /
            HueBar.ActualHeight *
            360.0;

        UpdateColorArea();
        UpdateColor();
    }

    // =====================================================
    // SELEZIONE SATURAZIONE / LUMINOSITÀ
    // =====================================================

    private void ColorArea_MouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        UpdateSaturationValue(e);
        ColorArea.CaptureMouse();
    }

    private void ColorArea_MouseMove(
        object sender,
        MouseEventArgs e)
    {
        if (ColorArea.IsMouseCaptured &&
            e.LeftButton == MouseButtonState.Pressed)
        {
            UpdateSaturationValue(e);
        }
    }

    private void ColorArea_MouseLeftButtonUp(
        object sender,
        MouseButtonEventArgs e)
    {
        ColorArea.ReleaseMouseCapture();
    }

    private void UpdateSaturationValue(
        MouseEventArgs e)
    {
        Point position =
            e.GetPosition(ColorArea);

        double width =
            ColorArea.ActualWidth;

        double height =
            ColorArea.ActualHeight;

        saturation =
            Math.Clamp(
                position.X / width,
                0,
                1);

        value =
            Math.Clamp(
                1 - position.Y / height,
                0,
                1);

        UpdateColor();
    }

    // =====================================================
    // AGGIORNA GRADIENTE
    // =====================================================

    private void UpdateColorArea()
    {
        Color hueColor =
            HsvToColor(
                hue,
                1,
                1);

        LinearGradientBrush saturationBrush =
            new()
            {
                StartPoint =
                    new Point(0, 0),

                EndPoint =
                    new Point(1, 0)
            };

        saturationBrush.GradientStops.Add(
            new GradientStop(
                Colors.White,
                0));

        saturationBrush.GradientStops.Add(
            new GradientStop(
                hueColor,
                1));

        SaturationLayer.Background =
            saturationBrush;
    }

    // =====================================================
    // AGGIORNA COLORE
    // =====================================================

    private void UpdateColor()
    {
        Color color =
            HsvToColor(
                hue,
                saturation,
                value);

        SelectedColor =
            color;

        ColorPreview.Background =
            new SolidColorBrush(color);

        if (!updating)
        {
            updating = true;

            HexTextBox.Text =
                $"#{color.R:X2}" +
                $"{color.G:X2}" +
                $"{color.B:X2}";

            updating = false;
        }
    }

    // =====================================================
    // HEX
    // =====================================================

    private void HexTextBox_LostFocus(
        object sender,
        RoutedEventArgs e)
    {
        UpdateFromHex();
    }

    private void HexTextBox_KeyDown(
        object sender,
        KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            UpdateFromHex();
        }
    }

    private void UpdateFromHex()
    {
        if (!TryParseColor(
                HexTextBox.Text,
                out Color color))
        {
            HexTextBox.Text =
                $"#{SelectedColor.R:X2}" +
                $"{SelectedColor.G:X2}" +
                $"{SelectedColor.B:X2}";

            return;
        }

        RgbToHsv(
            color,
            out hue,
            out saturation,
            out value);

        UpdateColorArea();
        UpdateColor();
    }

    // =====================================================
    // OK / ANNULLA
    // =====================================================

    private void Ok_Click(
        object sender,
        RoutedEventArgs e)
    {
        UpdateFromHex();

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(
        object sender,
        RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    // =====================================================
    // HEX → COLOR
    // =====================================================

    private static bool TryParseColor(
        string? text,
        out Color color)
    {
        color = Colors.Transparent;

        if (string.IsNullOrWhiteSpace(text))
            return false;

        text =
            text.Trim()
                .TrimStart('#');

        if (text.Length != 6)
            return false;

        if (!byte.TryParse(
                text.Substring(0, 2),
                NumberStyles.HexNumber,
                CultureInfo.InvariantCulture,
                out byte r))
        {
            return false;
        }

        if (!byte.TryParse(
                text.Substring(2, 2),
                NumberStyles.HexNumber,
                CultureInfo.InvariantCulture,
                out byte g))
        {
            return false;
        }

        if (!byte.TryParse(
                text.Substring(4, 2),
                NumberStyles.HexNumber,
                CultureInfo.InvariantCulture,
                out byte b))
        {
            return false;
        }

        color =
            Color.FromRgb(
                r,
                g,
                b);

        return true;
    }

    // =====================================================
    // HSV → RGB
    // =====================================================

    private static Color HsvToColor(
        double h,
        double s,
        double v)
    {
        double c = v * s;
        double x =
            c *
            (1 -
             Math.Abs(
                 (h / 60.0 % 2) - 1));

        double m = v - c;

        double r;
        double g;
        double b;

        if (h < 60)
        {
            r = c;
            g = x;
            b = 0;
        }
        else if (h < 120)
        {
            r = x;
            g = c;
            b = 0;
        }
        else if (h < 180)
        {
            r = 0;
            g = c;
            b = x;
        }
        else if (h < 240)
        {
            r = 0;
            g = x;
            b = c;
        }
        else if (h < 300)
        {
            r = x;
            g = 0;
            b = c;
        }
        else
        {
            r = c;
            g = 0;
            b = x;
        }

        return Color.FromRgb(
            (byte)Math.Round((r + m) * 255),
            (byte)Math.Round((g + m) * 255),
            (byte)Math.Round((b + m) * 255));
    }

    // =====================================================
    // RGB → HSV
    // =====================================================

    private static void RgbToHsv(
        Color color,
        out double h,
        out double s,
        out double v)
    {
        double r = color.R / 255.0;
        double g = color.G / 255.0;
        double b = color.B / 255.0;

        double max =
            Math.Max(
                r,
                Math.Max(g, b));

        double min =
            Math.Min(
                r,
                Math.Min(g, b));

        double delta =
            max - min;

        v = max;

        s =
            max == 0
                ? 0
                : delta / max;

        if (delta == 0)
        {
            h = 0;
            return;
        }

        if (max == r)
        {
            h =
                60 *
                (((g - b) / delta) % 6);
        }
        else if (max == g)
        {
            h =
                60 *
                (((b - r) / delta) + 2);
        }
        else
        {
            h =
                60 *
                (((r - g) / delta) + 4);
        }

        if (h < 0)
            h += 360;
    }
}