using DockManager.Models;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DockManager;

public partial class GroupWindow : Window
{
    // Gruppo attualmente visualizzato
    public DockItem Group { get; }

    private readonly DockConfig config;
    private Point magnifyMousePosition;

    private bool magnifyMouseInside;

    private bool magnifyRenderingActive;

    private const double MagnifySmoothness = 0.22;

    public GroupWindow(
        DockItem group,
        DockConfig dockConfig)
    {
        InitializeComponent();

        Group = group;
        config = dockConfig;

        Title = group.Name;


        Loaded += GroupWindow_Loaded;

        GroupPanel.Orientation =
            config.IsVertical
                ? Orientation.Vertical
                : Orientation.Horizontal;

        LoadItems();

        GroupPanel.MouseMove +=
            GroupPanel_MouseMove;

        GroupPanel.MouseLeave +=
            GroupPanel_MouseLeave;
    }

    // =====================================================
    // POSIZIONE STACK
    // =====================================================

    private void GroupWindow_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        ApplyAppearance();
        PositionWindow();
    }

    private void PositionWindow()
    {
        Window? owner = Owner;

        if (owner == null)
            return;

        if (!config.IsVertical)
        {
            // =============================================
            // DOCK ORIZZONTALE
            // =============================================

            // Centra la sotto-Dock rispetto alla Dock madre
            Left =
                owner.Left +
                (owner.Width - Width) / 2;

            // La sotto-Dock si apre sotto la Dock
            Top =
                owner.Top +
                owner.Height +
                10;

            // Se non c'è spazio sotto,
            // la apriamo sopra.
            double screenHeight =
                SystemParameters.WorkArea.Bottom;

            if (Top + Height > screenHeight)
            {
                Top =
                    owner.Top -
                    Height -
                    10;
            }
        }
        else
        {
            // =============================================
            // DOCK VERTICALE
            // =============================================

            // Centra la sotto-Dock verticalmente
            // rispetto alla Dock madre.
            Top =
                owner.Top +
                (owner.Height - Height) / 2;

            // Se la Dock madre è a sinistra
            // dello schermo, apriamo la sotto-Dock
            // a destra.
            if (owner.Left <
                SystemParameters.WorkArea.Width / 2)
            {
                Left =
                    owner.Left +
                    owner.Width +
                    10;
            }
            else
            {
                // Dock madre a destra:
                // apriamo la sotto-Dock a sinistra.
                Left =
                    owner.Left -
                    Width -
                    10;
            }
        }

        // =============================================
        // LIMITI SCHERMO
        // =============================================

        double screenLeft =
            SystemParameters.WorkArea.Left;

        double screenTop =
            SystemParameters.WorkArea.Top;

        double screenRight =
            SystemParameters.WorkArea.Right;

        double screenBottom =
            SystemParameters.WorkArea.Bottom;

        if (Left < screenLeft + 5)
        {
            Left =
                screenLeft + 5;
        }

        if (Top < screenTop + 5)
        {
            Top =
                screenTop + 5;
        }

        if (Left + Width >
            screenRight - 5)
        {
            Left =
                screenRight -
                Width -
                5;
        }

        if (Top + Height >
            screenBottom - 5)
        {
            Top =
                screenBottom -
                Height -
                5;
        }
    }

    // =====================================================
    // DRAG & DROP
    // =====================================================

    protected override void OnDragOver(
        DragEventArgs e)
    {
        base.OnDragOver(e);

        if (e.Data.GetDataPresent(
                DataFormats.FileDrop))
        {
            e.Effects =
                DragDropEffects.Copy;

            e.Handled = true;
        }
        else
        {
            e.Effects =
                DragDropEffects.None;
        }
    }

    protected override void OnDrop(
    DragEventArgs e)
    {
        base.OnDrop(e);

        if (!e.Data.GetDataPresent(
                DataFormats.FileDrop))
            return;

        string[] files =
            (string[])e.Data.GetData(
                DataFormats.FileDrop);

        foreach (string file in files)
        {
            AddItemToGroup(file);
        }

        e.Handled = true;
    }

    private void AddItemToGroup(
    string path)
    {
        if (!File.Exists(path) &&
            !Directory.Exists(path))
            return;

        bool alreadyExists =
            Group.Items.Any(item =>
                item.Path.Equals(
                    path,
                    StringComparison.OrdinalIgnoreCase));

        if (alreadyExists)
            return;

        DockItem item = new()
        {
            Type = "item",
            Path = path,
            Name = Path.GetFileName(path)
        };

        Group.Items.Add(item);

        SaveConfig();

        LoadItems();
    }
    // =====================================================
    // AGGIORNA IMPOSTAZIONI
    // =====================================================

    public void ApplySettings()
    {
        ApplyAppearance();

        GroupPanel.Orientation =
            config.IsVertical
                ? Orientation.Vertical
                : Orientation.Horizontal;

        LoadItems();
        PositionWindow();
    }

    private void ApplyAppearance()
    {
        if (Owner is DockWindow dock)
        {
            GroupBorder.Background =
                dock.DockBorder.Background;

            GroupBorder.BorderBrush =
                dock.DockBorder.BorderBrush;

            GroupBorder.BorderThickness =
                dock.DockBorder.BorderThickness;
        }
    }

    // =====================================================
    // CARICAMENTO ELEMENTI
    // =====================================================

    private void LoadItems()
    {
        GroupPanel.Children.Clear();

        if (Group.Items.Count == 0)
        {
            TextBlock empty = new()
            {
                Text = "Gruppo vuoto",

                Foreground =
                    Brushes.White,

                FontSize = 14,

                Margin =
                    new Thickness(20),

                VerticalAlignment =
                    VerticalAlignment.Center
            };

            GroupPanel.Children.Add(empty);

            Width = 140;
            Height = Math.Max(90, config.IconSize + 37);

            return;
        }

        foreach (DockItem item in Group.Items)
        {
            if (!item.IsGroup)
            {
                CreateButton(item);
            }
        }

        UpdateWindowSize();
    }

    // =====================================================
    // CREA ELEMENTO
    // =====================================================

    private void CreateButton(
        DockItem item)
    {
        Button button = new()
        {
            Width = config.IconSize + 17,

            Height = config.IconSize + 17,

            Margin =
         new Thickness(
             config.IconSpacing),

            Background =
         Brushes.Transparent,

            BorderThickness =
         new Thickness(0),

            Style =
    (Style)FindResource("GroupButtonStyle"),

            ToolTip =
         string.IsNullOrWhiteSpace(item.Name)
             ? Path.GetFileName(item.Path)
             : item.Name,

            Tag = item
        };

        Image image = new()
        {
            Width = config.IconSize,

            Height = config.IconSize,

            Source =
                GetIcon(item)
        };

        button.Content = image;

        button.Click += (s, e) =>
        {
            OpenItem(item.Path);
        };

        CreateContextMenu(
            button,
            item);

        GroupPanel.Children.Add(button);
    }

    // =====================================================
    // MENU ELEMENTO
    // =====================================================

    private void CreateContextMenu(
        Button button,
        DockItem item)
    {
        ContextMenu menu = new();

        MenuItem open = new()
        {
            Header = "Apri"
        };

        open.Click += (s, e) =>
        {
            OpenItem(item.Path);
        };

        MenuItem remove = new()
        {
            Header = "Rimuovi dal gruppo"
        };

        remove.Click += (s, e) =>
        {
            RemoveItem(item);
        };

        MenuItem changeIcon = new()
        {
            Header = "Cambia icona..."
        };

        changeIcon.Click += (s, e) =>
        {
            ChangeIcon(item);
        };

        MenuItem resetIcon = new()
        {
            Header = "Ripristina icona originale"
        };

        resetIcon.Click += (s, e) =>
        {
            item.Icon = null;
            SaveConfig();
            LoadItems();
        };

        MenuItem properties = new()
        {
            Header = "Proprietà"
        };

        properties.Click += (s, e) =>
        {
            ShowProperties(item.Path);
        };

        menu.Items.Add(open);

        menu.Items.Add(
            new Separator());

        menu.Items.Add(remove);
        menu.Items.Add(changeIcon);
        menu.Items.Add(resetIcon);

        menu.Items.Add(
            new Separator());

        menu.Items.Add(properties);

        button.ContextMenu = menu;
    }

    // =====================================================
    // APERTURA ELEMENTO
    // =====================================================

    private void OpenItem(string path)
    {
        try
        {
            Process.Start(
                new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });

            // Chiudiamo lo stack
            // dopo aver aperto l'elemento.
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Impossibile aprire:\n\n{path}\n\n{ex.Message}",
                "DockManager",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    // =====================================================
    // RIMOZIONE ELEMENTO
    // =====================================================

    private void RemoveItem(
        DockItem item)
    {
        MessageBoxResult result =
            MessageBox.Show(
                $"Rimuovere \"{item.Name}\" dal gruppo?",
                "DockManager",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
            return;

        Group.Items.Remove(item);

        SaveConfig();
        LoadItems();
    }

    // =====================================================
    // CAMBIA ICONA
    // =====================================================

    private void ChangeIcon(
        DockItem item)
    {
        OpenFileDialog dialog = new()
        {
            Title = "Seleziona una nuova icona",
            Filter =
                "Icone e applicazioni|*.ico;*.png;*.exe;*.lnk|" +
                "Icone (*.ico)|*.ico|" +
                "Immagini PNG (*.png)|*.png|" +
                "Programmi (*.exe)|*.exe|" +
                "Collegamenti (*.lnk)|*.lnk|" +
                "Tutti i file|*.*"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            item.Icon = IconService.ImportIcon(dialog.FileName);
            SaveConfig();
            LoadItems();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Impossibile importare l'icona:\n\n{ex.Message}",
                "DockManager",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void SaveConfig()
    {
        try
        {
            string folder = Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData),
                "DockManager",
                "docks");

            Directory.CreateDirectory(folder);

            string file = Path.Combine(
                folder,
                $"{config.Id}.json");

            string json = JsonSerializer.Serialize(
                config,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

            File.WriteAllText(file, json);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Errore durante il salvataggio:\n\n{ex.Message}",
                "DockManager",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    // =====================================================
    // PROPRIETÀ
    // =====================================================

    private void ShowProperties(
        string path)
    {
        string type =
            Directory.Exists(path)
                ? "Cartella"
                : "File";

        MessageBox.Show(
            $"Nome:\n{Path.GetFileName(path)}\n\n" +
            $"Percorso:\n{path}\n\n" +
            $"Tipo:\n{type}",
            "Proprietà",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    // =====================================================
    // DIMENSIONAMENTO
    // =====================================================

    private void UpdateWindowSize()
    {
        int count = Group.Items.Count;

        double buttonWidth =
            config.IconSize + 17;

        double buttonHeight =
            config.IconSize + 17;

        double buttonMargin =
            config.IconSpacing * 2;

        if (config.IsVertical)
        {
            // =============================================
            // VERTICALE
            // =============================================

            Width = Math.Max(
                120,
                buttonWidth +
                buttonMargin +
                20);

            Height = Math.Max(
                90,
                count *
                (buttonHeight + buttonMargin) +
                20);
        }
        else
        {
            // =============================================
            // ORIZZONTALE
            // =============================================

            Width = Math.Max(
                120,
                count *
                (buttonWidth + buttonMargin) +
                20);

            Height = Math.Max(
                90,
                buttonHeight +
                buttonMargin +
                20);
        }
    }

    // =====================================================
    // ICONA
    // =====================================================

    private ImageSource? GetIcon(
        DockItem item)
    {
        ImageSource? custom =
            IconService.Load(item.Icon, 64);

        return custom ?? GetWindowsIcon(item.Path);
    }

    // =====================================================
    // ICONA WINDOWS
    // =====================================================

    private ImageSource? GetWindowsIcon(
     string path)
    {
        return IconService.LoadWindowsIcon(path);
    }




    // =====================================================
    // MAGNIFY
    // =====================================================

    private void GroupPanel_MouseMove(
        object sender,
        MouseEventArgs e)
    {
        magnifyMousePosition =
            e.GetPosition(GroupPanel);

        magnifyMouseInside = true;

        StartMagnifyRendering();
    }


    private void GroupPanel_MouseLeave(
        object sender,
        MouseEventArgs e)
    {
        magnifyMouseInside = false;

        StartMagnifyRendering();
    }


    private void StartMagnifyRendering()
    {
        if (magnifyRenderingActive)
            return;

        magnifyRenderingActive = true;

        CompositionTarget.Rendering +=
            Magnify_Rendering;
    }


    private void StopMagnifyRendering()
    {
        if (!magnifyRenderingActive)
            return;

        magnifyRenderingActive = false;

        CompositionTarget.Rendering -=
            Magnify_Rendering;
    }


    private ScaleTransform GetMagnifyTransform(
        Button button)
    {
        if (button.RenderTransform
            is ScaleTransform scale)
        {
            return scale;
        }

        ScaleTransform newScale =
            new ScaleTransform(1.0, 1.0);

        button.RenderTransform =
            newScale;

        button.RenderTransformOrigin =
            new Point(0.5, 0.5);

        return newScale;
    }


    private void Magnify_Rendering(
        object? sender,
        EventArgs e)
    {
        int count =
            GroupPanel.Children.Count;

        if (count == 0)
        {
            StopMagnifyRendering();
            return;
        }

        bool needsAnimation =
            magnifyMouseInside;

        double mouseX =
            magnifyMousePosition.X;

        double radius =
            config.MagnifyRadius;

        double maximum =
            config.MagnifyMaximum;

        for (int i = 0; i < count; i++)
        {
            if (GroupPanel.Children[i]
                is not Button button)
            {
                continue;
            }

            ScaleTransform scale =
                GetMagnifyTransform(button);

            double targetScale = 1.0;

            if (magnifyMouseInside)
            {
                Point center =
                    button
                        .TransformToAncestor(GroupPanel)
                        .Transform(
                            new Point(
                                button.ActualWidth / 2,
                                button.ActualHeight / 2));

                double distance =
                    Math.Abs(
                        mouseX - center.X);

                if (distance < radius)
                {
                    double influence =
                        1.0 -
                        distance / radius;

                    influence =
                        influence *
                        influence *
                        (3.0 -
                         2.0 * influence);

                    targetScale =
                        1.0 +
                        influence *
                        (maximum - 1.0);
                }
            }

            double newScale =
                scale.ScaleX +
                (targetScale -
                 scale.ScaleX) *
                MagnifySmoothness;

            scale.ScaleX =
                newScale;

            scale.ScaleY =
                newScale;

            if (Math.Abs(newScale - 1.0) > 0.001 ||
                Math.Abs(targetScale - 1.0) > 0.001)
            {
                needsAnimation = true;
            }
        }

        if (!magnifyMouseInside &&
            !needsAnimation)
        {
            StopMagnifyRendering();
        }
    }
}