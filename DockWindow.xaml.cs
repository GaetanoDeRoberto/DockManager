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

public partial class DockWindow : Window
{
    private readonly DockConfig config;

    private readonly string configFolder;

    private readonly string iconsFolder;

    private Button? draggedButton;

    private Point dragStartPoint;

    // =====================================================
    // MAGNIFY
    // =====================================================

    private Point magnifyMousePosition;

    private bool magnifyMouseInside;

    private bool magnifyRenderingActive;

    private bool isDeleted;

    private double MagnifyRadius =>
     config.MagnifyRadius;

    private double MagnifyMaximum =>
        config.MagnifyMaximum;

    private const double MagnifySmoothness = 0.22;


    public DockWindow(DockConfig dockConfig)
    {
        InitializeComponent();

        config = dockConfig;

        string dockManagerFolder =
    Path.Combine(
        Environment.GetFolderPath(
            Environment.SpecialFolder.ApplicationData),
        "DockManager");

        configFolder =
            Path.Combine(
                dockManagerFolder,
                "docks");

        iconsFolder =
            Path.Combine(
                dockManagerFolder,
                "icons");

        Directory.CreateDirectory(configFolder);
        Directory.CreateDirectory(iconsFolder);

        Left = config.Left;
        Top = config.Top;

        Width = config.Width;
        Height = config.Height;

        DockPanel.Orientation =
            config.IsVertical
                ? Orientation.Vertical
                : Orientation.Horizontal;

        LoadItems();

        ApplySettings();

        CreateContextMenu();

        // Magnify
        DockPanel.MouseMove += DockPanel_MouseMove;
        DockPanel.MouseLeave += DockPanel_MouseLeave;

    }

    // =====================================================
    // CARICAMENTO
    // =====================================================

    private void LoadItems()
    {
        foreach (DockItem item in config.Items)
        {
            if (item.IsGroup)
            {
                CreateGroupButton(item);
            }
            else if (
                item.Type == "shell" ||
                File.Exists(item.Path) ||
                Directory.Exists(item.Path))
            {
                CreateItemButton(
                    item,
                    DockPanel);
            }
        }

        UpdateDockSize();
    }

    // =====================================================
    // MENU DOCK
    // =====================================================


    private void CreateContextMenu()
    {
        ContextMenu menu = new();

        menu.Background = new SolidColorBrush(
            Color.FromRgb(32, 32, 32));

        menu.Foreground = Brushes.White;

        menu.BorderBrush = new SolidColorBrush(
            Color.FromRgb(58, 58, 58));

        menu.BorderThickness = new Thickness(1);

        menu.Foreground =
            Brushes.White;

        menu.Padding =
            new Thickness(4);

        MenuItem newItem = new()
        {
            Header = "Nuovo elemento..."
        };

        MenuItem thisPC = new()
        {
            Header = "Questo PC"
        };

        thisPC.Click += ThisPC_Click;

        newItem.Click += NewItem_Click;

        MenuItem newGroup = new()
        {
            Header = "Nuovo gruppo..."
        };

        newGroup.Click += NewGroup_Click;

        MenuItem newDock = new()
        {
            Header = "Nuova Dock"
        };

        newDock.Click += NewDock_Click;

        MenuItem settings = new()
        {
            Header = "Impostazioni..."
        };

        settings.Click += Settings_Click;

        MenuItem renameDock = new()
        {
            Header = "Rinomina questa Dock..."
        };

        renameDock.Click += RenameDock_Click;

        MenuItem deleteDock = new()
        {
            Header = "Elimina questa Dock"
        };

        deleteDock.Click += DeleteDock_Click;

        MenuItem savePosition = new()
        {
            Header = "Salva posizione"
        };

        savePosition.Click += (s, e) =>
        {
            SavePosition();
        };

        MenuItem close = new()
        {
            Header = "Chiudi questa Dock"
        };

        close.Click += CloseDock_Click;

        MenuItem exit = new()
        {
            Header = "Esci da DockManager"
        };

        exit.Click += Exit_Click;

        menu.Items.Add(newItem);
        menu.Items.Add(thisPC);
        menu.Items.Add(newGroup);

        menu.Items.Add(new Separator());

        menu.Items.Add(newDock);

        menu.Items.Add(new Separator());

        menu.Items.Add(renameDock);
        menu.Items.Add(deleteDock);

        menu.Items.Add(new Separator());

        menu.Items.Add(savePosition);
        menu.Items.Add(settings);

        menu.Items.Add(new Separator());

        menu.Items.Add(close);
        menu.Items.Add(exit);

        DockBorder.ContextMenu = menu;
    }



    // =====================================================
    // COMANDI MENU PERSONALIZZATO
    // =====================================================

    public void NewItemFromContextMenu()
    {
        NewItem_Click(
            this,
            new RoutedEventArgs());
    }

    public void NewGroupFromContextMenu()
    {
        NewGroup_Click(
            this,
            new RoutedEventArgs());
    }

    public void NewDockFromContextMenu()
    {
        NewDock_Click(
            this,
            new RoutedEventArgs());
    }

    public void RenameDockFromContextMenu()
    {
        RenameDock_Click(
            this,
            new RoutedEventArgs());
    }

    public void DeleteDockFromContextMenu()
    {
        DeleteDock_Click(
            this,
            new RoutedEventArgs());
    }

    public void SavePositionFromContextMenu()
    {
        SavePosition();
    }

    public void SettingsFromContextMenu()
    {
        Settings_Click(
            this,
            new RoutedEventArgs());
    }

    public void CloseDockFromContextMenu()
    {
        CloseDock_Click(
            this,
            new RoutedEventArgs());
    }

    public void ExitFromContextMenu()
    {
        Exit_Click(
            this,
            new RoutedEventArgs());
    }
    // =====================================================
    // NUOVO ELEMENTO
    // =====================================================

    private void NewItem_Click(
        object sender,
        RoutedEventArgs e)
    {
        OpenFileDialog dialog = new()
        {
            Title =
                "Seleziona un file o collegamento",

            Filter =
                "Tutti i file|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            AddItem(
                dialog.FileName);
        }
    }

    private void ThisPC_Click(
    object sender,
    RoutedEventArgs e)
    {
        const string thisPCPath =
            "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}";

        foreach (DockItem existing in config.Items)
        {
            if (existing.Path.Equals(
                    thisPCPath,
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        DockItem item = new()
        {
            Type = "shell",
            Path = thisPCPath,
            Name = "Questo PC"
        };

        config.Items.Add(item);

        CreateItemButton(
            item,
            DockPanel);

        SaveConfig();

        UpdateDockSize();
    }

    // =====================================================
    // NUOVO GRUPPO
    // =====================================================

    private void NewGroup_Click(
        object sender,
        RoutedEventArgs e)
    {
        InputDialog dialog =
            new(
                "Nome del gruppo:",
                "Nuovo gruppo");

        dialog.Owner = this;

        if (dialog.ShowDialog() != true)
            return;

        string name =
            dialog.Answer.Trim();

        if (string.IsNullOrWhiteSpace(name))
            return;

        DockItem group = new()
        {
            Type = "group",
            Name = name
        };

        config.Items.Add(group);

        CreateGroupButton(group);

        SaveConfig();

        UpdateDockSize();
    }

    // =====================================================
    // NUOVA DOCK
    // =====================================================

    private void NewDock_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (Application.Current is App app)
        {
            app.CreateNewDock();
        }
    }

    // =====================================================
    // IMPOSTAZIONI
    // =====================================================

    private void Settings_Click(
      object sender,
      RoutedEventArgs e)
    {
        SettingsWindow window =
            new(config);

        window.Owner = this;

        if (window.ShowDialog() == true)
        {
            ApplySettings();

            SaveConfig();
        }
    }

    private void ApplySettings()
    {
        DockPanel.Orientation =
            config.IsVertical
                ? Orientation.Vertical
                : Orientation.Horizontal;
        DockScrollViewer.HorizontalScrollBarVisibility =
            config.IsVertical
                ? ScrollBarVisibility.Disabled
                : ScrollBarVisibility.Hidden;

        DockScrollViewer.VerticalScrollBarVisibility =
            config.IsVertical
                ? ScrollBarVisibility.Hidden
                : ScrollBarVisibility.Disabled;

        foreach (UIElement element
                 in DockPanel.Children)
        {
            if (element is Button button)
            {
                button.Width =
                    config.IconSize + 17;

                button.Height =
                    config.IconSize + 17;

                button.Margin =
                    new Thickness(
                        config.IconSpacing);

                if (button.Content is Image image)
                {
                    image.Width =
                        config.IconSize;

                    image.Height =
                        config.IconSize;
                }
                else if (button.Content is TextBlock text)
                {
                    text.FontSize =
                        config.IconSize * 0.79;
                }
            }
            Color background =
                (Color)ColorConverter.ConvertFromString(
                    config.DockBackground);

            background.A =
                (byte)(config.DockOpacity * 255);

            DockBorder.Opacity = 1.0;

            DockBorder.Background =
                new SolidColorBrush(background);

            DockBorder.BorderBrush =
                new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString(
                        config.DockBorder));
        }

        UpdateDockSize();
        openGroupWindow?.ApplySettings();
    }

    // =====================================================
    // RINOMINA DOCK
    // =====================================================

    private void RenameDock_Click(
        object sender,
        RoutedEventArgs e)
    {
        InputDialog dialog =
            new(
                "Nome della Dock:",
                "Rinomina Dock");

        dialog.Owner = this;

        if (dialog.ShowDialog() != true)
            return;

        string name =
            dialog.Answer.Trim();

        if (string.IsNullOrWhiteSpace(name))
            return;

        config.Name = name;

        Title = name;

        SaveConfig();
    }


    // =====================================================
    // ELIMINA DOCK
    // =====================================================

    private void DeleteDock_Click(
        object sender,
        RoutedEventArgs e)
    {
        MessageBoxResult result =
            MessageBox.Show(
                $"Vuoi eliminare la Dock \"{config.Name}\"?",
                "Elimina Dock",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
            return;

        if (Application.Current is App app)
        {
            isDeleted = true;
            app.DeleteDock(config);
        }

        Close();
    }

    // =====================================================
    // CHIUSURA DOCK
    // =====================================================

    private void CloseDock_Click(
        object sender,
        RoutedEventArgs e)
    {
        SaveConfig();

        Close();
    }

    // =====================================================
    // USCITA
    // =====================================================

    private void Exit_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (Application.Current is App app)
        {
            app.ShutdownApplication();
        }
        else
        {
            Application.Current.Shutdown();
        }
    }

    // =====================================================
    // SPOSTAMENTO DOCK
    // =====================================================

    protected override void OnMouseLeftButtonDown(
        MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);

        if (e.OriginalSource == DockBorder)
        {
            DragMove();

            SavePosition();
        }
    }

    protected override void OnLocationChanged(
        EventArgs e)
    {
        base.OnLocationChanged(e);

        if (IsLoaded)
        {
            config.Left = Left;
            config.Top = Top;
        }
    }

    // =====================================================
    // AGGIUNTA ELEMENTO
    // =====================================================

    private void AddItem(
        string path)
    {
        foreach (DockItem existing in config.Items)
        {
            if (!existing.IsGroup &&
                existing.Path.Equals(
                    path,
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        DockItem item = new()
        {
            Type = "item",
            Path = path,
            Name = Path.GetFileName(path)
        };

        config.Items.Add(item);

        CreateItemButton(
            item,
            DockPanel);

        SaveConfig();

        UpdateDockSize();
    }

    // =====================================================
    // CREAZIONE ELEMENTO
    // =====================================================

    private void CreateItemButton(
        DockItem item,
        Panel parent)
    {
        Button button = new()
        {
            Width = config.IconSize + 17,
            Height = config.IconSize + 17,

            Margin =
                new Thickness(config.IconSpacing),

            Background =
                Brushes.Transparent,

            BorderThickness =
                new Thickness(0),

            Style =
                (Style)FindResource(
                    "DockButtonStyle"),

            Tag = item,

            ToolTip =
                string.IsNullOrWhiteSpace(item.Name)
                    ? Path.GetFileName(item.Path)
                    : item.Name,

            AllowDrop = true
        };

        Image image = new()
        {
            Source =
                GetItemIcon(item),

            Width = config.IconSize,
            Height = config.IconSize
        };

        button.Content = image;

        button.Click += (s, e) =>
        {
            OpenItem(item.Path);
        };

        button.PreviewMouseLeftButtonDown +=
            Button_MouseLeftButtonDown;

        button.PreviewMouseMove +=
            Button_MouseMove;

        button.Drop +=
            Button_Drop;

        CreateItemContextMenu(
            button,
            item);

        parent.Children.Add(button);
    }

    // =====================================================
    // CREAZIONE GRUPPO
    // =====================================================

    private void CreateGroupButton(
        DockItem group)
    {
        Button button = new()
        {
            Width = config.IconSize + 17,
            Height = config.IconSize + 17,

            Margin =
                new Thickness(config.IconSpacing),

            Background =
                Brushes.Transparent,

            BorderThickness =
                new Thickness(0),

            FocusVisualStyle = null,

            Style =
                (Style)FindResource(
                    "DockButtonStyle"),

            Tag = group,

            ToolTip = group.Name,

            AllowDrop = true
        };

        ImageSource? customIcon =
            IconService.Load(
                group.Icon,
                (int)config.IconSize);

        if (customIcon != null)
        {
            Image image = new()
            {
                Source = customIcon,
                Width = config.IconSize,
                Height = config.IconSize
            };

            button.Content = image;
        }
        else
        {
            TextBlock icon = new()
            {
                Text = "📁",
                FontSize = config.IconSize * 0.79,
                HorizontalAlignment =
                    HorizontalAlignment.Center,
                VerticalAlignment =
                    VerticalAlignment.Center
            };

            button.Content = icon;
        }

        button.Click += (s, e) =>
        {
            OpenGroup(group);
        };

        button.PreviewMouseLeftButtonDown +=
            Button_MouseLeftButtonDown;

        button.PreviewMouseMove +=
            Button_MouseMove;

        button.AllowDrop = true;

        button.Drop +=
            Group_Drop;

        CreateGroupContextMenu(
            button,
            group);

        DockPanel.Children.Add(button);
    }

    // =====================================================
    // MENU ELEMENTO
    // =====================================================

    private void CreateItemContextMenu(
        Button button,
        DockItem item)
    {
        ContextMenu menu = new();

        menu.Background = new SolidColorBrush(
            Color.FromRgb(32, 32, 32));

        menu.Foreground = Brushes.White;

        menu.BorderBrush = new SolidColorBrush(
            Color.FromRgb(58, 58, 58));

        menu.BorderThickness = new Thickness(1);

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
            Header = "Rimuovi dalla Dock"
        };

        remove.Click += (s, e) =>
        {
            RemoveItem(item);
        };

        MenuItem rename = new()
        {
            Header = "Rinomina..."
        };

        rename.Click += (s, e) =>
        {
            RenameItem(
                button,
                item);
        };

        MenuItem changeIcon = new()
        {
            Header = "Cambia icona..."
        };

        changeIcon.Click += (s, e) =>
        {
            ChangeIcon(item);
        };

        MenuItem restoreIcon = new()
        {
            Header = "Ripristina icona originale"
        };

        restoreIcon.Click += (s, e) =>
        {
            RestoreIcon(item);
        };

        MenuItem resetIcon = new()
        {
            Header = "Ripristina icona"
        };

        resetIcon.Click += (s, e) =>
        {
            ResetIcon(item);
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
        menu.Items.Add(rename);
        menu.Items.Add(changeIcon);
        menu.Items.Add(resetIcon);
        menu.Items.Add(restoreIcon);

        menu.Items.Add(
            new Separator());

        menu.Items.Add(properties);

        button.ContextMenu = menu;
    }

    // =====================================================
    // MENU GRUPPO
    // =====================================================

    private void CreateGroupContextMenu(
        Button button,
        DockItem group)
    {
        ContextMenu menu = new();

        menu.Background = new SolidColorBrush(
            Color.FromRgb(32, 32, 32));

        menu.Foreground = Brushes.White;

        menu.BorderBrush = new SolidColorBrush(
            Color.FromRgb(58, 58, 58));

        menu.BorderThickness = new Thickness(1);

        MenuItem open = new()
        {
            Header = "Apri gruppo"
        };

        open.Click += (s, e) =>
        {
            OpenGroup(group);
        };

        MenuItem addItem = new()
        {
            Header = "Aggiungi elemento..."
        };

        addItem.Click += (s, e) =>
        {
            AddItemToGroup(group);
        };

        MenuItem rename = new()
        {
            Header = "Rinomina gruppo..."
        };

        rename.Click += (s, e) =>
        {
            RenameGroup(
                button,
                group);
        };

        MenuItem changeIcon = new()
        {
            Header = "Cambia icona..."
        };

        changeIcon.Click += (s, e) =>
        {
            ChangeIcon(group);
        };

        MenuItem resetIcon = new()
        {
            Header = "Ripristina icona"
        };

        resetIcon.Click += (s, e) =>
        {
            ResetIcon(group);
        };

        MenuItem remove = new()
        {
            Header = "Elimina gruppo"
        };

        remove.Click += (s, e) =>
        {
            RemoveGroup(group);
        };

        menu.Items.Add(open);

        menu.Items.Add(
            new Separator());

        menu.Items.Add(addItem);
        menu.Items.Add(rename);
        menu.Items.Add(changeIcon);
        menu.Items.Add(resetIcon);

        menu.Items.Add(
            new Separator());

        menu.Items.Add(remove);

        button.ContextMenu = menu;
    }

    // =====================================================
    // APERTURA ELEMENTO
    // =====================================================

    private void OpenItem(string path)
    {
        try
        {
            if (path ==
           "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}")
            {
                Process.Start(
                    new ProcessStartInfo
                    {
                        FileName =
                            "explorer.exe",

                        Arguments =
                            "shell:MyComputerFolder",

                        UseShellExecute = true
                    });

                return;
            }
            Process.Start(
                new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Impossibile aprire:\n{path}\n\n{ex.Message}",
                "DockManager",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    // =====================================================
    // APERTURA GRUPPO
    // =====================================================

    private GroupWindow? openGroupWindow;

    private void OpenGroup(DockItem group)
    {
        // Se lo stack attualmente aperto
        // appartiene a questo gruppo, lo chiudiamo.
        if (openGroupWindow != null &&
            openGroupWindow.IsVisible)
        {
            if (openGroupWindow.Group == group)
            {
                openGroupWindow.Close();
                openGroupWindow = null;
                return;
            }

            // Se è un altro gruppo,
            // chiudiamo quello precedente.
            openGroupWindow.Close();
            openGroupWindow = null;
        }

        // Apriamo il nuovo gruppo
        openGroupWindow =
            new GroupWindow(group, config);

        openGroupWindow.Owner = this;

        openGroupWindow.Closed += (s, e) =>
        {
            openGroupWindow = null;
        };

        openGroupWindow.Show();
    }

    // =====================================================
    // AGGIUNGI ELEMENTO AL GRUPPO
    // =====================================================

    private void AddItemToGroup(
        DockItem group)
    {
        OpenFileDialog dialog = new()
        {
            Title =
                $"Aggiungi elemento a \"{group.Name}\"",

            Filter =
                "Tutti i file|*.*"
        };

        if (dialog.ShowDialog() != true)
            return;

        string path =
            dialog.FileName;

        foreach (DockItem item in group.Items)
        {
            if (item.Path.Equals(
                    path,
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        group.Items.Add(
            new DockItem
            {
                Type = "item",
                Path = path,
                Name = Path.GetFileName(path)
            });

        SaveConfig();
    }

    // =====================================================
    // RIMOZIONE ELEMENTO
    // =====================================================

    private void RemoveItem(
        DockItem item)
    {
        MessageBoxResult result =
            MessageBox.Show(
                $"Vuoi rimuovere dalla Dock:\n\n{item.Name}?",
                "Rimuovi elemento",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
            return;

        config.Items.Remove(item);

        RebuildDock();

        SaveConfig();
    }

    // =====================================================
    // RIMOZIONE GRUPPO
    // =====================================================

    private void RemoveGroup(
        DockItem group)
    {
        MessageBoxResult result =
            MessageBox.Show(
                $"Vuoi eliminare il gruppo \"{group.Name}\"?",
                "Elimina gruppo",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
            return;

        config.Items.Remove(group);

        RebuildDock();

        SaveConfig();
    }

    // =====================================================
    // RINOMINA ELEMENTO
    // =====================================================

    private void RenameItem(
        Button button,
        DockItem item)
    {
        InputDialog dialog =
            new(
                "Nuovo nome:",
                item.Name);

        dialog.Owner = this;

        if (dialog.ShowDialog() != true)
            return;

        string name =
            dialog.Answer.Trim();

        if (string.IsNullOrWhiteSpace(name))
            return;

        item.Name = name;

        button.ToolTip = name;

        SaveConfig();
    }

    // =====================================================
    // RINOMINA GRUPPO
    // =====================================================

    private void RenameGroup(
        Button button,
        DockItem group)
    {
        InputDialog dialog =
            new(
                "Nuovo nome del gruppo:",
                group.Name);

        dialog.Owner = this;

        if (dialog.ShowDialog() != true)
            return;

        string name =
            dialog.Answer.Trim();

        if (string.IsNullOrWhiteSpace(name))
            return;

        group.Name = name;

        button.ToolTip = name;

        SaveConfig();
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
            RebuildDock();
            SaveConfig();
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

    private void RestoreIcon(
        DockItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Icon))
            return;

        item.Icon = null;

        RebuildDock();

        SaveConfig();
    }

    private void ResetIcon(
    DockItem item)
    {
        item.Icon = null;

        RebuildDock();

        SaveConfig();
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
    // DRAG & DROP DA EXPLORER
    // =====================================================

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
            AddItem(file);
        }
    }

    // =====================================================
    // DRAG ICONA
    // =====================================================

    private void Button_MouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        draggedButton =
            sender as Button;

        dragStartPoint =
            e.GetPosition(null);
    }

    private void Button_MouseMove(
        object sender,
        MouseEventArgs e)
    {
        if (e.LeftButton !=
            MouseButtonState.Pressed)
            return;

        if (draggedButton == null)
            return;

        Point currentPoint =
            e.GetPosition(null);

        Vector difference =
            dragStartPoint -
            currentPoint;

        if (Math.Abs(difference.X) < 5 &&
            Math.Abs(difference.Y) < 5)
            return;

        Button button =
            draggedButton;

        draggedButton = null;

        if (button.Tag is DockItem item)
        {
            DragDrop.DoDragDrop(
                button,
                item,
                DragDropEffects.Move);
        }
    }

    // =====================================================
    // RIORDINAMENTO
    // =====================================================

    private void Button_Drop(
        object sender,
        DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(
                typeof(DockItem)))
            return;

        DockItem? draggedItem =
            e.Data.GetData(
                typeof(DockItem))
                as DockItem;

        Button? targetButton =
            sender as Button;

        if (draggedItem == null ||
            targetButton == null)
            return;

        DockItem? targetItem =
            targetButton.Tag as DockItem;

        if (targetItem == null ||
            draggedItem == targetItem)
            return;

        int oldIndex =
            config.Items.IndexOf(
                draggedItem);

        int newIndex =
            config.Items.IndexOf(
                targetItem);

        if (oldIndex < 0 ||
            newIndex < 0)
            return;

        config.Items.RemoveAt(oldIndex);

        if (oldIndex < newIndex)
            newIndex--;

        config.Items.Insert(
            newIndex,
            draggedItem);

        RebuildDock();

        SaveConfig();
    }

    // =====================================================
    // DRAG NEL GRUPPO
    // =====================================================

    private void Group_Drop(
     object sender,
     DragEventArgs e)
    {
        Button? targetButton =
            sender as Button;

        if (targetButton == null)
            return;

        DockItem? group =
            targetButton.Tag as DockItem;

        if (group == null ||
            !group.IsGroup)
            return;


        // =================================================
        // FILE/CARTELLE TRASCINATI DA EXPLORER
        // =================================================

        if (e.Data.GetDataPresent(
                DataFormats.FileDrop))
        {
            string[] files =
                (string[])e.Data.GetData(
                    DataFormats.FileDrop);

            foreach (string file in files)
            {
                bool alreadyExists =
                    group.Items.Any(x =>
                        x.Path.Equals(
                            file,
                            StringComparison.OrdinalIgnoreCase));

                if (alreadyExists)
                    continue;

                DockItem item = new()
                {
                    Type = "item",

                    Path = file,

                    Name =
                        Path.GetFileName(file)
                };

                group.Items.Add(item);
            }

            SaveConfig();

            e.Handled = true;

            return;
        }


        // =================================================
        // ELEMENTO PROVENIENTE DALLA DOCK
        // =================================================

        if (e.Data.GetDataPresent(
                typeof(DockItem)))
        {
            DockItem? draggedItem =
                e.Data.GetData(
                    typeof(DockItem)) as DockItem;

            if (draggedItem == null)
                return;

            // Non permettiamo gruppi dentro gruppi
            if (draggedItem.IsGroup)
                return;

            // Evita di trascinare qualcosa
            // dentro lo stesso gruppo
            if (group.Items.Contains(
                    draggedItem))
            {
                return;
            }

            // Evita duplicati
            if (group.Items.Any(x =>
                x.Path.Equals(
                    draggedItem.Path,
                    StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            // Rimuovi dalla Dock
            config.Items.Remove(
                draggedItem);

            // Inserisci nel gruppo
            group.Items.Add(
                draggedItem);

            SaveConfig();

            RebuildDock();

            e.Handled = true;
        }
    }

    // =====================================================
    // RICOSTRUZIONE
    // =====================================================

    private void RebuildDock()
    {
        DockPanel.Children.Clear();

        foreach (DockItem item in config.Items)
        {
            if (item.IsGroup)
            {
                CreateGroupButton(item);
            }
            else if (
                item.Type == "shell" ||
                File.Exists(item.Path) ||
                Directory.Exists(item.Path))
            {
                CreateItemButton(
                    item,
                    DockPanel);
            }
        }

        UpdateDockSize();
    }

    // =====================================================
    // DIMENSIONAMENTO
    // =====================================================

    private void UpdateDockSize()
    {
        double buttonSize =
            config.IconSize + 17 +
            (config.IconSpacing * 2);

        double padding = 24;

        if (config.IsVertical)
        {
            Width = Math.Max(
                90,
                buttonSize + padding);

            Height = Math.Max(
                120,
                DockPanel.Children.Count *
                buttonSize +
                padding);
        }
        else
        {
            Width = Math.Max(
                120,
                DockPanel.Children.Count *
                buttonSize +
                padding);

            Height = Math.Max(
                90,
                buttonSize + padding);
        }
    }

    // =====================================================
    // SALVATAGGIO POSIZIONE
    // =====================================================

    private void SavePosition()
    {
        config.Left = Left;
        config.Top = Top;

        config.Width = Width;
        config.Height = Height;

        SaveConfig();
    }

    // =====================================================
    // SALVATAGGIO JSON
    // =====================================================

    private void SaveConfig()
    {
        try
        {
            Directory.CreateDirectory(
                configFolder);

            string file =
                Path.Combine(
                    configFolder,
                    $"{config.Id}.json");

            string json =
                JsonSerializer.Serialize(
                    config,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });

            File.WriteAllText(
                file,
                json);
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
    // CHIUSURA
    // =====================================================

    protected override void OnClosed(
      EventArgs e)
    {
        CompositionTarget.Rendering -=
            Magnify_Rendering;

        if (!isDeleted)
        {
            SavePosition();
        }

        base.OnClosed(e);
    }

    // =====================================================
    // ICONE WINDOWS
    // =====================================================



    // =====================================================
    // ICONA ELEMENTO
    // =====================================================

    private ImageSource? GetItemIcon(
    DockItem item)
    {
        ImageSource? custom =
            IconService.Load(item.Icon, 64);

        return custom ?? GetIcon(item.Path);
    }

    private ImageSource? GetIcon(
        string path)
    {
        return IconService.LoadWindowsIcon(path);
    }



    // =====================================================
    // INPUT DIALOG
    // =====================================================

    private class InputDialog : Window
    {
        public string Answer { get; private set; } = "";

        private readonly TextBox textBox;

        public InputDialog(
            string message,
            string defaultValue)
        {
            Title = "DockManager";

            Width = 350;
            Height = 150;

            WindowStartupLocation =
                WindowStartupLocation.CenterOwner;

            ResizeMode =
                ResizeMode.NoResize;

            StackPanel panel = new()
            {
                Margin =
                    new Thickness(15)
            };

            panel.Children.Add(
                new TextBlock
                {
                    Text = message,

                    Margin =
                        new Thickness(
                            0, 0, 0, 8)
                });

            textBox = new TextBox
            {
                Text = defaultValue,

                Margin =
                    new Thickness(
                        0, 0, 0, 10)
            };

            panel.Children.Add(textBox);

            StackPanel buttons = new()
            {
                Orientation =
                    Orientation.Horizontal,

                HorizontalAlignment =
                    HorizontalAlignment.Right
            };

            Button ok = new()
            {
                Content = "OK",
                Width = 70,
                Margin = new Thickness(4)
            };

            ok.Click += (s, e) =>
            {
                Answer = textBox.Text;

                DialogResult = true;
            };

            Button cancel = new()
            {
                Content = "Annulla",
                Width = 70,
                Margin = new Thickness(4)
            };

            cancel.Click += (s, e) =>
            {
                DialogResult = false;
            };

            buttons.Children.Add(ok);
            buttons.Children.Add(cancel);

            panel.Children.Add(buttons);

            Content = panel;

            Loaded += (s, e) =>
            {
                textBox.Focus();
                textBox.SelectAll();
            };
        }
    }

    // =====================================================
    // MAGNIFY
    // =====================================================

    private void DockPanel_MouseMove(
        object sender,
        MouseEventArgs e)
    {
        magnifyMousePosition =
            e.GetPosition(DockPanel);

        magnifyMouseInside = true;
        StartMagnifyRendering();
    }

    private void DockPanel_MouseLeave(
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
        CompositionTarget.Rendering += Magnify_Rendering;
    }

    private void StopMagnifyRendering()
    {
        if (!magnifyRenderingActive)
            return;

        magnifyRenderingActive = false;
        CompositionTarget.Rendering -= Magnify_Rendering;
    }

    private ScaleTransform GetMagnifyTransform(
    Button button)
    {
        if (button.RenderTransform is ScaleTransform scale)
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
        int count = DockPanel.Children.Count;

        if (count == 0)
        {
            StopMagnifyRendering();
            return;
        }

        bool needsAnimation = magnifyMouseInside;

        double mousePosition =
            config.IsVertical
                ? magnifyMousePosition.Y
                : magnifyMousePosition.X;

        for (int i = 0; i < count; i++)
        {
            if (DockPanel.Children[i] is not Button button)
                continue;

            ScaleTransform scale =
                GetMagnifyTransform(button);

            double targetScale = 1.0;

            if (magnifyMouseInside)
            {
                Point center =
                    button.TransformToAncestor(DockPanel)
                          .Transform(
                              new Point(
                                  button.ActualWidth / 2,
                                  button.ActualHeight / 2));

                double centerPosition =
                    config.IsVertical
                        ? center.Y
                        : center.X;

                double distance =
                    Math.Abs(
                        mousePosition -
                        centerPosition);

                if (distance < MagnifyRadius)
                {
                    double influence =
                        1.0 -
                        distance /
                        MagnifyRadius;

                    influence =
                        influence *
                        influence *
                        (3.0 - 2.0 * influence);

                    targetScale =
                        1.0 +
                        influence *
                        (MagnifyMaximum - 1.0);
                }
            }

            double newScale =
                scale.ScaleX +
                (targetScale - scale.ScaleX) *
                MagnifySmoothness;

            scale.ScaleX = newScale;
            scale.ScaleY = newScale;

            if (Math.Abs(newScale - 1.0) > 0.001 ||
                Math.Abs(targetScale - 1.0) > 0.001)
            {
                needsAnimation = true;
            }
        }

        if (!magnifyMouseInside && !needsAnimation)
        {
            StopMagnifyRendering();
        }
    }

}