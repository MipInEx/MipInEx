using MipInEx;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

ModManagerBase modManager = new();

Mod modA = new Mod(modManager, "ModA",
    ImmutableArray.Create(new ModDependencyInfo("ModG", true), new ModDependencyInfo("ModB", true)),
    ImmutableArray.Create(new ModIncompatibilityInfo("ModH")));
    //ImmutableArray<ModIncompatibilityInfo>.Empty);

Mod modB = new Mod(modManager, "ModB",
    ImmutableArray<ModDependencyInfo>.Empty,
    ImmutableArray.Create(new ModIncompatibilityInfo("ModC")));

Mod modC = new Mod(modManager, "ModC",
    ImmutableArray.Create(new ModDependencyInfo("ModF", false)),
    ImmutableArray<ModIncompatibilityInfo>.Empty);

//Mod modD = new Mod(modManager, "ModD",
//    ImmutableArray.Create(new ModDependencyInfo("ModE", false)),
//    ImmutableArray<ModIncompatibilityInfo>.Empty);

//Mod modE = new Mod(modManager, "ModE",
//    ImmutableArray.Create(new ModDependencyInfo("ModD", false)),
//    ImmutableArray<ModIncompatibilityInfo>.Empty);

Mod modD = new Mod(modManager, "ModD",
    ImmutableArray<ModDependencyInfo>.Empty,
    ImmutableArray.Create(new ModIncompatibilityInfo("ModE")));

Mod modE = new Mod(modManager, "ModE",
    ImmutableArray<ModDependencyInfo>.Empty,
    ImmutableArray.Create(new ModIncompatibilityInfo("ModD")));

Mod modF = new Mod(modManager, "ModF",
    ImmutableArray<ModDependencyInfo>.Empty,
    ImmutableArray<ModIncompatibilityInfo>.Empty);

Mod modG = new Mod(modManager, "ModG",
    ImmutableArray<ModDependencyInfo>.Empty,
    //ImmutableArray.Create(new ModDependencyInfo("ModH", true)),
    ImmutableArray<ModIncompatibilityInfo>.Empty);

Mod[] allMods = new Mod[]
{
    modA,
    modB,
    modC,
    modD,
    modE,
    modF,
    modG
};

Mod[] allModsToLoad = new Mod[]
{
    modA,
    modB,
    modC,
    modD,
    modF,
    modG
};

modManager.FullRegistry.AddMods(allMods);

Console.WriteLine("=== MODS ===");
foreach (Mod mod in allMods)
{
    mod.LogInfo();
    Console.WriteLine();
}

List<string> refreshedModGuids = new();
foreach (Mod mod in allMods)
{
    mod.RefreshIncompatibilitiesAndDependencies(refreshedModGuids);
}

Console.WriteLine("=== LOADING MODS ===");

modManager.EnqueueMods(allModsToLoad);
modManager.EmptyLoadQueue();

Console.WriteLine("=== LOADING MODS ===");

modManager.EnqueueMods(modE);
modManager.EmptyLoadQueue();

Console.ReadLine();