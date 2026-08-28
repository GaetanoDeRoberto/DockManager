namespace DockManager.Models;

public class DockConfig
{
    public string Id { get; set; } = "";

    public string Name { get; set; } = "";

    public double Left { get; set; }

    public double Top { get; set; }

    public double Width { get; set; } = 500;

    public double Height { get; set; } = 90;

    // =====================================================
    // IMPOSTAZIONI DOCK
    // =====================================================

    public double IconSize { get; set; } = 48;

    public double IconSpacing { get; set; } = 4;

    public double MagnifyMaximum { get; set; } = 1.55;

    public double MagnifyRadius { get; set; } = 150;

    public double MagnifySmoothness { get; set; } = 0.22;

    public double DockOpacity { get; set; } = 0.85;

    public string DockBackground { get; set; } = "#202020";

    public string DockBorder { get; set; } = "#3A3A3A";

    public bool IsVertical { get; set; } = false;

    public List<DockItem> Items { get; set; } = new();
}

public class DockItem
{
    public string Type { get; set; } = "item";

    public string Path { get; set; } = "";

    public string Name { get; set; } = "";

    public string Icon { get; set; } = "";

    public List<DockItem> Items { get; set; } = new();

    public bool IsGroup =>
        Type.Equals(
            "group",
            StringComparison.OrdinalIgnoreCase);
}