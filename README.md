# MipInEx
MipInEx is a modding framework for Unity games. Support for other game engines will be added in the future.

What sets MipInEx apart from [BepInEx](https://github.com/BepInEx/BepInEx) is it's expanded scope.

|                                                                      | MipInEx         | BepInEx                      |
|----------------------------------------------------------------------|-----------------|------------------------------|
| Patching Game Code                                                   | ✔️              | ✔️                           |
| Logging                                                              | ✔️              | ✔️                           |
| Configuration                                                        | ✔️              | ✔️                           |
| Plugins                                                              | ✔️              | ✔️                           |
| Plugin Unloading                                                     | ✔️              | ⚠️ No way to really do so    |
| Internal Plugins                                                     | ✔️              | ❌                           |
| Unity Asset Bundles                                                  | ✔️              | ❌ Requires manually loading |
| [WWise Sound Banks](https://www.audiokinetic.com/en/products/wwise/) | In Development  | ❌ Requires manually loading |
## Key Differences

### Plugin Structure
#### MipInEx
```cs
using MipInEx;

[MipInPlugin]
internal sealed class EntryPoint : ModRootPluginBase
{
    protected override void Load()
    {
        this.Logger.LogInfo("Mod loaded!");
    }

    protected override void Unload()
    {
        this.Logger.LogInfo("Mod unloaded!");
    }
}
```
Note: passing `GUID`, `NAME`, and `VERSION` into the `MipInPlugin` attribute isn't a requirement, as it will fallback to the manifest `GUID`, `NAME`, and `VERSION`.
#### BepInEx
```cs
using BepInEx;

[BepInPlugin(GUID, NAME, VERSION)]
internal sealed class EntryPoint : BaseUnityPlugin
{
    public void Awake()
    {
        this.LogSource.LogInfo("Mod loaded!");
    }

    public void OnDestroy()
    {
        this.LogSource.LogInfo("Mod unloaded!");
    }
}
```

### Patching
#### MipInEx
```cs
using MipInEx;
using DummyGame;

[MipInPlugin]
internal sealed class EntryPoint : ModRootPluginBase
{
    protected override void Load()
    {
        this.Patcher.PatchAll(typeof(Patches));
    }

    protected override void Unload()
    {
        this.Patcher.UnpatchSelf();
    }
}

internal static class Patches
{
    [HarmonyPatch(typeof(DummyBehaviour), "Start")]
    [HarmonyPostfix]
    [HarmonyWrapSafe]
    public static void TestPatch(DummyBehaviour __instance)
    {
        __instance.DoSomething();
    }
}
```

#### BepInEx
```cs
using BepInEx;
using DummyGame;

[BepInPlugin(GUID, NAME, VERSION)]
internal sealed class EntryPoint : BaseUnityPlugin
{
    // Option 1
    private Harmony patcher = null!;

    public void Awake()
    {
        this.patcher = new HarmonyPatcher(GUID);
        this.patcher.PatchAll(typeof(Patches));
    }

    public void OnDestroy()
    {
        this.patcher.UnpatchSelf();
    }

    // Option 2
    private readonly Harmony patcher;

    public EntryPoint()
    {
        this.patcher = new Harmony(GUID);
    }

    public void Awake()
    {
        this.patcher.PatchAll(typeof(Patches));
    }

    public void OnDestroy()
    {
        this.patcher.UnpatchSelf();
    }
}

internal static class Patches
{
    [HarmonyPatch(typeof(DummyBehaviour), "Start")]
    [HarmonyPostfix]
    [HarmonyWrapSafe]
    public static void TestPatch(DummyBehaviour __instance)
    {
        __instance.DoSomething();
    }
}
```
Notice how you have to specifically create the Harmony Patcher instance. `MipInEx` does this for you.

## Used libraries
- HarmonyX, forked and modified from [BepInEx/HarmonyX](https://github.com/BepInEx/HarmonyX) - v2.10.1
- [0x0ade/MonoMod](https://github.com/0x0ade/MonoMod) - v22.5.1.1
- [jbevain/cecil](https://github.com/jbevain/cecil) - v0.10.4

## License
The MipInEx project is licensed under the MIT license.
