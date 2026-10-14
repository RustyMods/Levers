using System.IO;
using System.Linq;
using System.Text;
using BepInEx;
using HarmonyLib;
using Levers;
using PieceManager;

namespace Levers;

public static class ReadMeBuilder
{
    private static bool buildReadme = false;
    private static bool exportSprites = false;

    static ReadMeBuilder()
    {
        LeversPlugin.instance._harmony.Patch(
            AccessTools.DeclaredMethod(typeof(ZNetScene), nameof(ZNetScene.Awake)),
            postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(ReadMeBuilder), nameof(Build))));
    }

    public static void Init(bool exportIcons = true)
    {
        buildReadme = true;
        exportSprites = exportIcons;
    }

    public static void ExportLocalization()
    {
        var path = Path.Combine(Paths.ConfigPath, LeversPlugin.ModName);
        Directory.CreateDirectory(path);
        StringBuilder sb = new StringBuilder();
        
        foreach (var build in BuildPiece.registeredPieces)
        {
            var name = build.Prefab.GetComponent<Piece>().m_name;
            var localized = Localization.instance.Localize(name);
            sb.AppendLine(name.Replace("$", string.Empty) + ": " + localized);    
        }
        
        File.WriteAllText(Path.Combine(path, "English.yml"), sb.ToString());
    }
    
    public static void Build()
    {
        ExportLocalization();
        BuildAlt();
        // BuildTable();
    }

    public static void BuildTable()
    {
        if (!buildReadme) return;
            
        var path = Path.Combine(Paths.ConfigPath, LeversPlugin.ModName);
        Directory.CreateDirectory(path);
        
        var sb = new StringBuilder();
        sb.AppendLine($"# {LeversPlugin.ModName}");
        sb.AppendLine();
        
        foreach (var build in BuildPiece.registeredPieces)
        {
            var component = build.Prefab.GetComponent<Piece>();
            var internalId = component.name;
            var unLocalizedName = component.m_name;
            var unLocalizedDescription = component.m_description;
            var iconId = component.name + ".png";
            var iconUrl =
                $"https://github.com/RustyMods/{LeversPlugin.ModName}/blob/master/Icons/" +
                iconId;
            var resources = build.RequiredItems.Requirements
                .Select(r => $"<li>{r.itemName} x {r.amount}</li>");
            
            if (exportSprites)
            {
                IconExport.ExportSprite(component.m_icon, path, component.name);
            }
        
            sb.AppendLine("<table width=\"100%\">");
            sb.AppendLine("  <tr>");
            sb.AppendLine(
                $"    <td rowspan=\"4\" width=\"128\" align=\"center\" valign=\"middle\"><img src=\"{iconUrl}\" width=\"128\" height=\"128\" alt=\"{internalId}\"></td>");
            sb.AppendLine($"    <td><b>Internal ID:</b> <code>{internalId}</code></td>");
            sb.AppendLine("  </tr>");
            sb.AppendLine($"  <tr><td><b>Name:</b> {unLocalizedName}</td></tr>");
            sb.AppendLine($"  <tr><td><b>Description:</b> {unLocalizedDescription}</td></tr>");
            sb.AppendLine("  <tr><td><b>Resources:</b>");
            sb.AppendLine("    <ul>");
            foreach (var resource in resources)
            {
                sb.AppendLine($"      {resource}");
            }
        
            sb.AppendLine("    </ul>");
            sb.AppendLine("  </td></tr>");
            sb.AppendLine("</table>");
            sb.AppendLine();
        }
        
        File.WriteAllText(Path.Combine(path, "README.md"), sb.ToString());
    }
    public static void BuildAlt()
    {
        if (!buildReadme) return;

        var path = Path.Combine(Paths.ConfigPath, LeversPlugin.ModName);
        Directory.CreateDirectory(path);

        var sb = new StringBuilder();
        sb.AppendLine($"# {LeversPlugin.ModName}");
        sb.AppendLine();
        sb.AppendLine("| Icon | Internal ID | Name | Description | Resources |");
        sb.AppendLine("|:---:|:---|:---|:---|:---|");

        foreach (var build in BuildPiece.registeredPieces)
        {
            var component = build.Prefab.GetComponent<Piece>();
            var internalId = component.name;
            var unLocalizedName = component.m_name;
            var unLocalizedDescription = component.m_description;
            var iconUrl = $"https://raw.githubusercontent.com/RustyMods/{LeversPlugin.ModName}/master/Icons/" + component.name + ".png";
            var resources = string.Join(", ", build.RequiredItems.Requirements.Select(r => $"{r.itemName} x {r.amount}"));

            if (exportSprites)
            {
                IconExport.ExportSprite(component.m_icon, path, component.name);
            }

            sb.AppendLine($"| ![{internalId}]({iconUrl}) | `{internalId}` | {Clean(unLocalizedName)} | {Clean(unLocalizedDescription)} | {Clean(resources)} |");
        }

        File.WriteAllText(Path.Combine(path, "README.md"), sb.ToString());
    }

    // Pipes and line breaks would break a table row
    private static string Clean(string text) => string.IsNullOrEmpty(text) ? "" : text.Replace("|", "\\|").Replace("\r", "").Replace("\n", " ");
}