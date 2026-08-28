using DockManager.Models;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace DockManager;

public partial class SettingsWindow : Window
{
    private readonly DockConfig config;

    public SettingsWindow(
        DockConfig dockConfig)
    {
        InitializeComponent();

        config = dockConfig;

        IconSizeSlider.Value =
            config.IconSize;

        SpacingSlider.Value =
            config.IconSpacing;

        MagnifyMaximumSlider.Value =
            config.MagnifyMaximum;

        MagnifyRadiusSlider.Value =
            config.MagnifyRadius;

        StartupCheckBox.IsChecked =
            StartupManager.IsEnabled();

        DockOpacitySlider.Value =
            config.DockOpacity * 100;

        DockBackgroundTextBox.Text =
            config.DockBackground;

        if (ColorConverter.ConvertFromString(
            config.DockBackground)
                is Color backgroundColor)
        {
            DockBackgroundPreview.Background =
                new SolidColorBrush(backgroundColor);
        }

        DockBorderTextBox.Text =
            config.DockBorder;

        HorizontalRadioButton.IsChecked =
            !config.IsVertical;

        VerticalRadioButton.IsChecked =
            config.IsVertical;

        if (ColorConverter.ConvertFromString(
             config.DockBorder)
                is Color borderColor)
        {
            DockBorderPreview.Background =
                new SolidColorBrush(borderColor);
        }

        UpdateValues();

        IconSizeSlider.ValueChanged +=
            Slider_ValueChanged;

        SpacingSlider.ValueChanged +=
            Slider_ValueChanged;

        MagnifyMaximumSlider.ValueChanged +=
            Slider_ValueChanged;

        MagnifyRadiusSlider.ValueChanged +=
            Slider_ValueChanged;
    }

    private void Slider_ValueChanged(
        object sender,
        RoutedPropertyChangedEventArgs<double> e)
    {
        UpdateValues();

    }

    private void UpdateValues()
    {
        IconSizeValue.Text =
            $"{IconSizeSlider.Value:0}";

        SpacingValue.Text =
            $"{SpacingSlider.Value:0}";

        MagnifyMaximumValue.Text =
            $"{MagnifyMaximumSlider.Value:0.00}";

        MagnifyRadiusValue.Text =
            $"{MagnifyRadiusSlider.Value:0}";

        DockOpacityValue.Text =
            $"{DockOpacitySlider.Value:0}%";
    }

    private void Apply_Click(
        object sender,
        RoutedEventArgs e)
    {
        config.IconSize =
            IconSizeSlider.Value;

        config.IconSpacing =
            SpacingSlider.Value;

        config.MagnifyMaximum =
            MagnifyMaximumSlider.Value;

        config.MagnifyRadius =
            MagnifyRadiusSlider.Value;

        config.DockOpacity =
            DockOpacitySlider.Value / 100.0;

        config.DockBackground =
            DockBackgroundTextBox.Text;

        config.DockBorder =
            DockBorderTextBox.Text;

        config.IsVertical =
            VerticalRadioButton.IsChecked == true;


        bool startupEnabled =
            StartupCheckBox.IsChecked == true;

        if (!StartupManager.SetEnabled(startupEnabled))
        {
            MessageBox.Show(
                "Non è stato possibile modificare l'avvio automatico di Windows.",
                "DockManager",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

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

    private void DockBackgroundPreview_Click(
    object sender,
    MouseButtonEventArgs e)
    {
        ColorPickerWindow picker =
            new(DockBackgroundTextBox.Text);

        picker.Owner = this;

        if (picker.ShowDialog() == true)
        {
            Color color =
                picker.SelectedColor;

            string hex =
                $"#{color.R:X2}" +
                $"{color.G:X2}" +
                $"{color.B:X2}";

            DockBackgroundTextBox.Text =
                hex;

            DockBackgroundPreview.Background =
                new SolidColorBrush(color);
        }
    }

    private void DockBorderPreview_Click(
    object sender,
    MouseButtonEventArgs e)
    {
        ColorPickerWindow picker =
            new(DockBorderTextBox.Text);

        picker.Owner = this;

        if (picker.ShowDialog() == true)
        {
            Color color =
                picker.SelectedColor;

            string hex =
                $"#{color.R:X2}" +
                $"{color.G:X2}" +
                $"{color.B:X2}";

            DockBorderTextBox.Text = hex;

            DockBorderPreview.Background =
                new SolidColorBrush(color);
        }
    }
    private void DockOpacitySlider_ValueChanged(
    object sender,
    RoutedPropertyChangedEventArgs<double> e)
    {
        if (DockOpacityValue != null)
        {
            DockOpacityValue.Text =
                $"{DockOpacitySlider.Value:0}%";
        }
    }
}