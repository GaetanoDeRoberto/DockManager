using DockManager.Models;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace DockManager;

public partial class App : Application
{
    private readonly List<DockWindow> docks = new();

    private string docksFolder = "";

    protected override void OnStartup(
        StartupEventArgs e)
    {
        base.OnStartup(e);

        docksFolder = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData),
            "DockManager",
            "docks");

        Directory.CreateDirectory(
            docksFolder);

        LoadDocks();

        // Se non esiste ancora nessuna dock,
        // creiamo quella principale.
        if (docks.Count == 0)
        {
            CreateNewDock();
        }
    }

    // =====================================================
    // CARICA TUTTE LE DOCK
    // =====================================================

    private void LoadDocks()
    {
        string[] files =
            Directory.GetFiles(
                docksFolder,
                "dock-*.json");

        foreach (string file in files)
        {
            try
            {
                string json =
                    File.ReadAllText(file);

                DockConfig? config =
                    JsonSerializer.Deserialize<DockConfig>(
                        json);

                if (config == null)
                    continue;

                DockWindow dock =
                    new(config);

                docks.Add(dock);

                dock.Closed += Dock_Closed;

                dock.Show();
            }
            catch
            {
                // Ignora configurazioni corrotte
            }
        }
    }

    // =====================================================
    // CREA NUOVA DOCK
    // =====================================================

    public void CreateNewDock()
    {
        string id =
            GenerateDockId();

        int number =
            docks.Count + 1;

        DockConfig config = new()
        {
            Id = id,

            Name =
                $"Dock {number}",

            Left =
                (SystemParameters.PrimaryScreenWidth -
                 500) / 2,

            Top =
                SystemParameters.PrimaryScreenHeight -
                120,

            Width = 500,

            Height = 90
        };

        DockWindow dock =
            new(config);

        docks.Add(dock);

        dock.Closed += Dock_Closed;

        dock.Show();

        SaveDockConfig(config);
    }

    // =====================================================
    // ID
    // =====================================================

    private string GenerateDockId()
    {
        int number = 1;

        while (true)
        {
            string id =
                $"dock-{number:000}";

            string file =
                Path.Combine(
                    docksFolder,
                    $"{id}.json");

            if (!File.Exists(file))
                return id;

            number++;
        }
    }

    // =====================================================
    // SALVATAGGIO
    // =====================================================

    private void SaveDockConfig(
        DockConfig config)
    {
        string file =
            Path.Combine(
                docksFolder,
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

    // =====================================================
    // ELIMINA DOCK
    // =====================================================

    public void DeleteDock(DockConfig config)
    {
        string file = Path.Combine(
            docksFolder,
            $"{config.Id}.json");

        try
        {
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
    $"Non è stato possibile eliminare la Dock:\n\n{ex.Message}",
    "DockManager",
    MessageBoxButton.OK,
    MessageBoxImage.Error);
        }
    }

    // =====================================================
    // CHIUSURA SINGOLA DOCK
    // =====================================================

    private void Dock_Closed(
        object? sender,
        EventArgs e)
    {
        if (sender is DockWindow dock)
        {
            docks.Remove(dock);
        }
    }

    // =====================================================
    // USCITA COMPLETA
    // =====================================================

    public void ShutdownApplication()
    {
        foreach (DockWindow dock in docks.ToList())
        {
            dock.Close();
        }

        Shutdown();
    }
}