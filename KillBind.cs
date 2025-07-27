using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using LethalCompanyInputUtils.Api;
using LethalCompanyInputUtils.BindingPathEnums;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KillBind;

public class ModKeyBindings : LcInputActions
{
    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
    [InputAction(KeyboardControl.K, Name = "Kill bind")]
    public InputAction KillKey { get; set; } = null!;
}

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("com.rune580.LethalCompanyInputUtils")]
public class KillBind : BaseUnityPlugin
{
    public static KillBind Instance { get; private set; } = null!;
    internal static new ManualLogSource Logger { get; private set; } = null!;
    internal static Harmony? Harmony { get; set; }

    private static readonly ModKeyBindings modKeyBindings = new();

    private ConfigEntry<CauseOfDeath>? causeOfDeath;
    public CauseOfDeath CauseOfDeathValue => causeOfDeath?.Value ?? CauseOfDeath.Unknown;

    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public enum DeathAnimationOptions
    {
        CauseOfDeath = -2,
        None = -1,
        Normal,
        HeadBurst,
        Spring,
        Electrocuted,
        ComedyMask,
        TragedyMask,
        Burnt,
        Sliced,
        HeadGone,
        Pieces,
    }

    private ConfigEntry<DeathAnimationOptions>? deathAnimation;
    public DeathAnimationOptions? DeathAnimationValue =>
        deathAnimation?.Value switch
        {
            DeathAnimationOptions.CauseOfDeath => CauseOfDeathValue switch
            {
                CauseOfDeath.Unknown => DeathAnimationOptions.HeadBurst,
                CauseOfDeath.Electrocution => DeathAnimationOptions.Electrocuted,
                CauseOfDeath.Burning => DeathAnimationOptions.Burnt,
                CauseOfDeath.Fan => DeathAnimationOptions.HeadBurst,
                CauseOfDeath.Snipped => DeathAnimationOptions.Sliced,
                _ => DeathAnimationOptions.Normal,
            },
            DeathAnimationOptions.None => null,
            _ => deathAnimation?.Value ?? DeathAnimationOptions.Normal,
        };

    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
    internal static Vector3 BodyVelocity { get; set; } = default;

    private void Awake()
    {
        Logger = base.Logger;
        Instance = this;

        causeOfDeath = Config.Bind(
            "General",
            "CauseOfDeath",
            CauseOfDeath.Unknown,
            "What cause of death to display for your corpse"
        );
        deathAnimation = Config.Bind(
            "General",
            "DeathAnimation",
            DeathAnimationOptions.CauseOfDeath,
            "What ragdoll to spawn (CauseOfDeath chooses automatically based on cause of death)"
        );

        Harmony ??= new Harmony(MyPluginInfo.PLUGIN_GUID);
        Logger.LogDebug("Patching...");
        Harmony.PatchAll();
        Logger.LogDebug("Finished patching!");

        modKeyBindings.KillKey.performed += KillBind_performed;

        Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
    }

    private static void KillBind_performed(InputAction.CallbackContext ctx)
    {
        if (
            !ctx.performed
            || GameNetworkManager.Instance?.localPlayerController == null
            || GameNetworkManager.Instance.localPlayerController.isPlayerDead
            || GameNetworkManager.Instance.localPlayerController.isTypingChat
            || GameNetworkManager.Instance.localPlayerController.quickMenuManager.isMenuOpen
            || GameNetworkManager.Instance.localPlayerController.inTerminalMenu
        )
            return;

        var deathAnimation = Instance.DeathAnimationValue;
        GameNetworkManager.Instance.localPlayerController.KillPlayer(
            BodyVelocity,
            deathAnimation != null,
            Instance.CauseOfDeathValue,
            Math.Clamp(
                (int)(deathAnimation ?? DeathAnimationOptions.Normal),
                0,
                GameNetworkManager
                    .Instance
                    .localPlayerController
                    .playersManager
                    .playerRagdolls
                    .Count - 1
            )
        );
    }

    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.Update))]
    internal class UpdatePatch
    {
        // ReSharper disable once UnusedMember.Local
        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions
        ) =>
            new CodeMatcher(instructions)
                .MatchForward(
                    false,
                    new CodeMatch(
                        OpCodes.Callvirt,
                        AccessTools.Method(
                            typeof(CharacterController),
                            nameof(CharacterController.Move)
                        )
                    )
                )
                .Advance(-2)
                .Insert(
                    new CodeInstruction(OpCodes.Dup),
                    new CodeInstruction(
                        OpCodes.Call,
                        AccessTools.PropertySetter(typeof(KillBind), nameof(BodyVelocity))
                    )
                )
                .InstructionEnumeration();
    }
}
