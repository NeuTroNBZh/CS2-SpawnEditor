using System.Reflection;
using CounterStrikeSharp.API.Core;

namespace RetakeSpawnEditor;

// Runtime integration with CS2-SimpleAdmin (https://github.com/daffyyyy/CS2-SimpleAdmin).
// Uses reflection so no compile-time dependency on CS2-SimpleAdminApi.dll is needed.
// If SimpleAdmin is not installed, all methods are no-ops and the plugin runs standalone.
public static class SimpleAdminBridge
{
    private static dynamic? _api;
    private static SpawnEditorPlugin? _plugin;

    public static bool Available => _api != null;

    public static void TryInit(SpawnEditorPlugin plugin)
    {
        _plugin = plugin;
        _api = null;
        try
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var ifaceType = asm.GetType("CS2_SimpleAdminApi.ICS2_SimpleAdminApi");
                if (ifaceType == null) continue;

                var capField = ifaceType.GetField("PluginCapability", BindingFlags.Public | BindingFlags.Static);
                var cap = capField?.GetValue(null);
                if (cap == null) continue;

                var api = cap.GetType().GetMethod("Get")?.Invoke(cap, null);
                if (api != null) { _api = api; break; }
            }
        }
        catch { }

        Console.WriteLine(_api != null
            ? "[RetakeSpawnEditor] SimpleAdmin API found — menus will be registered."
            : "[RetakeSpawnEditor] SimpleAdmin not found — running standalone.");
    }

    public static void RegisterMenus()
    {
        if (_api == null || _plugin == null) return;
        try
        {
            var flag = _plugin!.Config.AdminFlag;
            _api.RegisterMenuCategory("spawneditor", "Spawn Editor", flag);

            _api.RegisterMenu("spawneditor", "se_toggle", "Toggle Visualization",
                (Func<CCSPlayerController, object>)(admin =>
                {
                    _plugin!.ToggleVisualizationForAdmin(admin);
                    return (object)_api.CreateMenuWithBack("Spawn Editor", "spawneditor", admin);
                }),
                flag, "css_se");

            _api.RegisterMenu("spawneditor", "se_add", "Add Spawn...",
                (Func<CCSPlayerController, object>)(admin =>
                {
                    object menu = (object)_api.CreateMenuWithBack("Add Spawn", "spawneditor", admin);
                    foreach (var (team, site) in new[] { ("T", "A"), ("T", "B"), ("CT", "A"), ("CT", "B") })
                    {
                        var t = team; var s = site;
                        _api.AddSubMenu(menu, $"{t} - Site {s}",
                            (Func<CCSPlayerController, object>)(a =>
                            {
                                _plugin!.AddSpawnForAdmin(a, t, s);
                                return (object)_api.CreateMenuWithBack("Spawn Editor", "spawneditor", a);
                            }));
                    }
                    return menu;
                }),
                flag, "css_se_add");

            _api.RegisterMenu("spawneditor", "se_del", "Delete Nearest Spawn",
                (Func<CCSPlayerController, object>)(admin =>
                {
                    _plugin!.DeleteNearestForAdmin(admin);
                    return (object)_api.CreateMenuWithBack("Spawn Editor", "spawneditor", admin);
                }),
                flag, "css_se_del");

            _api.RegisterMenu("spawneditor", "se_save", "Save Spawns",
                (Func<CCSPlayerController, object>)(admin =>
                {
                    _plugin!.SaveSpawnsForAdmin(admin);
                    return (object)_api.CreateMenuWithBack("Spawn Editor", "spawneditor", admin);
                }),
                flag, "css_se_save");

            _api.RegisterMenu("spawneditor", "se_reload", "Reload Spawns",
                (Func<CCSPlayerController, object>)(admin =>
                {
                    _plugin!.ReloadSpawnsForAdmin(admin);
                    return (object)_api.CreateMenuWithBack("Spawn Editor", "spawneditor", admin);
                }),
                flag, "css_se_reload");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RetakeSpawnEditor] Error registering SimpleAdmin menus: {ex.Message}");
        }
    }

    public static void UnregisterMenus()
    {
        if (_api == null) return;
        foreach (var id in new[] { "se_toggle", "se_add", "se_del", "se_save", "se_reload" })
        {
            try { _api.UnregisterMenu("spawneditor", id); } catch { }
        }
    }
}

