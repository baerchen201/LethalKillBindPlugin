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

public class KeyBindings : LcInputActions
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

    internal static readonly KeyBindings keyBindings = new();

    private ConfigEntry<CauseOfDeath> _causeOfDeath = null!;
    public CauseOfDeath causeOfDeath => _causeOfDeath.Value;

    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public enum DeathAnimation
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

    private ConfigEntry<DeathAnimation> _deathAnimation = null!;
    public DeathAnimation? deathAnimation =>
        _deathAnimation.Value switch
        {
            DeathAnimation.CauseOfDeath => causeOfDeath switch
            {
                CauseOfDeath.Unknown => DeathAnimation.HeadBurst,
                CauseOfDeath.Electrocution => DeathAnimation.Electrocuted,
                CauseOfDeath.Burning => DeathAnimation.Burnt,
                CauseOfDeath.Fan => DeathAnimation.HeadBurst,
                CauseOfDeath.Snipped => DeathAnimation.Sliced,
                _ => DeathAnimation.Normal,
            },
            DeathAnimation.None => null,
            _ => _deathAnimation.Value,
        };

    internal static Vector3 _BodyVelocity { get; set; } = default;

    private void Awake()
    {
        Logger = base.Logger;
        Instance = this;

        _causeOfDeath = Config.Bind(
            "General",
            "CauseOfDeath",
            CauseOfDeath.Unknown,
            "What cause of death to display for your corpse"
        );
        _deathAnimation = Config.Bind(
            "General",
            "DeathAnimation",
            DeathAnimation.CauseOfDeath,
            "What ragdoll to spawn (CauseOfDeath chooses automatically based on cause of death)"
        );

        Harmony ??= new Harmony(MyPluginInfo.PLUGIN_GUID);
        Logger.LogDebug("Patching...");
        Harmony.PatchAll();
        Logger.LogDebug("Finished patching!");

        keyBindings.KillKey.performed += KillBind_performed;

        Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
    }

    private static void KillBind_performed(InputAction.CallbackContext ctx)
    {
        if (
            !ctx.performed
            || GameNetworkManager.Instance.localPlayerController == null
            || GameNetworkManager.Instance.localPlayerController.isPlayerDead
            || GameNetworkManager.Instance.localPlayerController.isTypingChat
            || GameNetworkManager.Instance.localPlayerController.quickMenuManager.isMenuOpen
            || GameNetworkManager.Instance.localPlayerController.inTerminalMenu
        )
            return;

        var deathAnimation = Instance.deathAnimation;
        GameNetworkManager.Instance.localPlayerController.KillPlayer(
            _BodyVelocity,
            deathAnimation != null,
            Instance.causeOfDeath,
            Math.Clamp(
                (int)(deathAnimation ?? DeathAnimation.Normal),
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
                        AccessTools.PropertySetter(typeof(KillBind), nameof(_BodyVelocity))
                    )
                )
                .InstructionEnumeration();
    }
}
