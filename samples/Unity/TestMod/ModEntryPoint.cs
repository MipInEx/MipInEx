using HarmonyLib;
using MipInEx;
using TestGame;

namespace TestMod;

[ModPlugin]
internal sealed class ModEntryPoint : ModRootPluginBase
{
    protected override void Load()
    {
        this.Patcher.PatchAll(typeof(Patches));

        if (this.ModManager.Registry.IsModLoaded("dev.Foo.OtherMod"))
        {
            this.LoadInternalPlugin("OtherMod.Additions");
        }

    }

    protected override void Unload()
    {
        this.Patcher.UnpatchSelf();
    }
}

[ModPluginInternal("OtherMod.Additions", "OtherModAdditions", "1.0.0")]
internal sealed class OtherModAdditions : ModInternalPluginBase<ModEntryPoint>
{
    protected override void Load()
    {
        this.Logger.LogDebug("Adding additions to 'OtherMod'");
    }
}

internal static class Patches
{
    [HarmonyPatch(typeof(GameInstanceManager), "InitializeSelf")]
    [HarmonyPostfix]
    [HarmonyWrapSafe]
    public static void BasicPatch(GameInstanceManager __instance)
    {
        __instance.gameObject.name = "Modded: " + __instance.gameObject.name;
    }
}


