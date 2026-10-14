using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using PieceManager;
using ServerSync;

namespace Levers;

[BepInPlugin(ModGUID, ModName, ModVersion)]
public class LeversPlugin : BaseUnityPlugin
{
    internal const string ModName = "Levers";
    internal const string ModVersion = "1.0.0";
    internal const string Author = "RustyMods";
    private const string ModGUID = Author + "." + ModName;
    private static readonly string ConfigFileName = ModGUID + ".cfg";
    private static readonly string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;
    internal static string ConnectionError = "";
    public readonly Harmony _harmony = new(ModGUID);

    public static readonly ManualLogSource LeversLogger = BepInEx.Logging.Logger.CreateLogSource(ModName);

    private static readonly ConfigSync ConfigSync = new(ModGUID)
        { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };

    public static LeversPlugin instance = null!;
    
    private static ConfigEntry<Toggle> _serverConfigLocked = null!;

    public enum Toggle
    {
        On = 1,
        Off = 0
    }

    public void Awake()
    {
        instance = this;
        
        _serverConfigLocked = config("1 - General", "Lock Configuration", Toggle.On,
            "If on, the configuration is locked and can be changed by server admins only.");
        _ = ConfigSync.AddLockingConfigEntry(_serverConfigLocked);

        SetupFloorPiece();
        SetupWallPiece();

        var leverKey = new LocalizeKey("$piece_lever");
        leverKey.English("Lever");
        
        // ReadMeBuilder.Init();
        
        var assembly = Assembly.GetExecutingAssembly();
        _harmony.PatchAll(assembly);
        SetupWatcher();
    }

    private static void SetupWallPiece()
    {
        var piece_wall_lever = new BuildPiece(
            "levers_bundle", 
            "piece_lever_wall");
        
        var piece = piece_wall_lever.Prefab.GetComponent<Piece>();
        piece.m_name = "$piece_wall_lever";
        piece.m_description = "$piece_wall_lever_desc";
        var wnt = piece_wall_lever.Prefab.GetComponent<WearNTear>();

        piece.m_placeEffect = new EffectListRef("sfx_build_hammer_wood", "vfx_Place_wood_pole");
        wnt.m_destroyedEffect = new EffectListRef("vfx_damaged_cart", "sfx_wood_destroyed");
        wnt.m_hitEffect = new EffectListRef("vfx_SawDust");
        
        piece_wall_lever.Prefab.AddComponent<Lever>();
        piece_wall_lever.Name.English("Wall Lever");
        piece_wall_lever.Description.English("");
        piece_wall_lever.Category.Set(BuildPieceCategory.Misc);
        piece_wall_lever.Usage.Set(Piece.UsageTagFlags.Building);
        piece_wall_lever.Crafting.Set(CraftingTable.Workbench);
        piece_wall_lever.RequiredItems.Add("Iron", 1, true);
        piece_wall_lever.RequiredItems.Add("Wood", 2, true);
        piece_wall_lever.Snapshot();

        var leverDat = new MeshData(
            piece_wall_lever.Prefab.transform.Find("model/Lever/Lever").gameObject, 
            "incinerator", 
            "Lever/Lever");
        leverDat.copyMaterials = true;

        var cogDat = new MeshData(
            piece_wall_lever.Prefab.transform.Find("model/Lever/cog").gameObject,
            "CharredCogwheel", 
            "default");
        cogDat.copyMaterials = true;
        
        var blocks = piece_wall_lever.Prefab.transform.Find("model/wood");
        for (int i = 0; i < blocks.transform.childCount; ++i)
        {
            var child = blocks.transform.GetChild(i);
            var dat = new MeshData(child.gameObject, "itemstandh", "New/block");
            dat.copyMaterials = true;
        }
        

    }

    private static void SetupFloorPiece()
    {
        var piece_floor_lever = new BuildPiece(
            "levers_bundle", 
            "piece_lever_floor");
        
        var piece = piece_floor_lever.Prefab.GetComponent<Piece>();
        piece.m_name = "$piece_floor_lever";
        piece.m_description = "$piece_floor_lever_desc";
        var wnt = piece_floor_lever.Prefab.GetComponent<WearNTear>();
        
        piece.m_placeEffect = new EffectListRef("sfx_build_hammer_wood", "vfx_Place_wood_pole");
        wnt.m_destroyedEffect = new EffectListRef("vfx_damaged_cart", "sfx_wood_destroyed");
        wnt.m_hitEffect = new EffectListRef("vfx_SawDust");
        
        piece_floor_lever.Prefab.AddComponent<Lever>();
        piece_floor_lever.Name.English("Floor Lever");
        piece_floor_lever.Description.English("");
        piece_floor_lever.Category.Set(BuildPieceCategory.Misc);
        piece_floor_lever.Usage.Set(Piece.UsageTagFlags.Building);
        piece_floor_lever.Crafting.Set(CraftingTable.Workbench);
        piece_floor_lever.RequiredItems.Add("Iron", 1, true);
        piece_floor_lever.RequiredItems.Add("Wood", 2, true);
        piece_floor_lever.Snapshot();

        var leverDat = new MeshData(
            piece_floor_lever.Prefab.transform.Find("model/Lever/Lever").gameObject, 
            "incinerator", 
            "Lever/Lever");
        leverDat.copyMaterials = true;

        var cogDat = new MeshData(
            piece_floor_lever.Prefab.transform.Find("model/Lever/cog").gameObject,
            "CharredCogwheel", 
            "default");
        cogDat.copyMaterials = true;
        
        var blocks = piece_floor_lever.Prefab.transform.Find("model/wood");
        for (int i = 0; i < blocks.transform.childCount; ++i)
        {
            var child = blocks.transform.GetChild(i);
            var dat = new MeshData(child.gameObject, "itemstandh", "New/block");
            dat.copyMaterials = true;
        }
        

    }

    private void OnDestroy()
    {
        Config.Save();
    }

    private void SetupWatcher()
    {
        FileSystemWatcher watcher = new(Paths.ConfigPath, ConfigFileName);
        watcher.Changed += ReadConfigValues;
        watcher.Created += ReadConfigValues;
        watcher.Renamed += ReadConfigValues;
        watcher.IncludeSubdirectories = true;
        watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
        watcher.EnableRaisingEvents = true;
    }

    private void ReadConfigValues(object sender, FileSystemEventArgs e)
    {
        if (!File.Exists(ConfigFileFullPath)) return;
        try
        {
            LeversLogger.LogDebug("ReadConfigValues called");
            Config.Reload();
        }
        catch
        {
            LeversLogger.LogError($"There was an issue loading your {ConfigFileName}");
            LeversLogger.LogError("Please check your config entries for spelling and format!");
        }
    }


    private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description,
        bool synchronizedSetting = true)
    {
        ConfigDescription extendedDescription =
            new(
                description.Description +
                (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"),
                description.AcceptableValues, description.Tags);
        var configEntry = Config.Bind(group, name, value, extendedDescription);
        //var configEntry = Config.Bind(group, name, value, description);

        var syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
        syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

        return configEntry;
    }

    private ConfigEntry<T> config<T>(string group, string name, T value, string description,
        bool synchronizedSetting = true)
    {
        return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
    }
}