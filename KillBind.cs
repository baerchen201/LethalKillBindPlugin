using BepInEx;
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
    [InputAction(KeyboardControl.K, Name = "Kill bind")]
    public InputAction KillKey { get; private set; } = null!;
}

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("com.rune580.LethalCompanyInputUtils")]
public class KillBind : BaseUnityPlugin
{
    public static KillBind Instance { get; private set; } = null!;
    internal static new ManualLogSource Logger { get; private set; } = null!;
    internal static Harmony? Harmony { get; set; }

    internal static readonly KeyBindings keyBindings = new KeyBindings();

    private void Awake()
    {
        Logger = base.Logger;
        Instance = this;

        Patch();

        Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
    }

    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.Update))]
    public class UpdatePatch
    {
        public static void Postfix(ref PlayerControllerB __instance)
        {
            if (!keyBindings.KillKey.triggered)
                return;
            if (
                (
                    !__instance.IsOwner
                    || !__instance.isPlayerControlled
                    || __instance is { IsServer: true, isHostPlayerObject: false }
                ) && !__instance.isTestingPlayer
            )
                return;

            if (
                __instance is not { inTerminalMenu: false, isTypingChat: false }
                || !Application.isFocused
            )
                return;

            float num3 = __instance.movementSpeed / __instance.carryWeight;
            if (__instance.sinkingValue > 0.73f)
            {
                num3 = 0f;
            }
            else
            {
                if (__instance.isCrouching)
                {
                    num3 /= 1.5f;
                }
                else if (__instance.criticallyInjured && !__instance.isCrouching)
                {
                    num3 *= __instance.limpMultiplier;
                }

                if (__instance.isSpeedCheating)
                {
                    num3 *= 15f;
                }

                if (__instance.movementHinderedPrev > 0)
                {
                    num3 /= 2f * __instance.hinderedMultiplier;
                }

                if (__instance.drunkness > 0f)
                {
                    num3 *=
                        StartOfRound.Instance.drunknessSpeedEffect.Evaluate(__instance.drunkness)
                            / 5f
                        + 1f;
                }

                if (!__instance.isCrouching && __instance.crouchMeter > 1.2f)
                {
                    num3 *= 0.5f;
                }

                if (!__instance.isCrouching)
                {
                    float num4 = Vector3.Dot(__instance.playerGroundNormal, __instance.walkForce);
                    if (num4 > 0.05f)
                    {
                        __instance.slopeModifier = Mathf.MoveTowards(
                            __instance.slopeModifier,
                            num4,
                            (__instance.slopeModifierSpeed + 0.45f) * Time.deltaTime
                        );
                    }
                    else
                    {
                        __instance.slopeModifier = Mathf.MoveTowards(
                            __instance.slopeModifier,
                            num4,
                            __instance.slopeModifierSpeed / 2f * Time.deltaTime
                        );
                    }

                    num3 = Mathf.Max(
                        num3 * 0.8f,
                        num3 + __instance.slopeIntensity * __instance.slopeModifier
                    );
                }
            }

            Vector3 vector3 = new Vector3(0f, 0f, 0f);
            int num5 = Physics.OverlapSphereNonAlloc(
                __instance.transform.position,
                0.65f,
                __instance.nearByPlayers,
                StartOfRound.Instance.playersMask
            );
            for (int i = 0; i < num5; i++)
            {
                vector3 +=
                    Vector3.Normalize(
                        (
                            __instance.transform.position
                            - __instance.nearByPlayers[i].transform.position
                        ) * 100f
                    ) * 1.2f;
            }
            int num6 = Physics.OverlapSphereNonAlloc(
                __instance.transform.position,
                1.25f,
                __instance.nearByPlayers,
                524288
            );
            for (int j = 0; j < num6; j++)
            {
                EnemyAICollisionDetect component = __instance
                    .nearByPlayers[j]
                    .gameObject.GetComponent<EnemyAICollisionDetect>();
                if (
                    component != null
                    && component.mainScript != null
                    && !component.mainScript.isEnemyDead
                    && Vector3.Distance(
                        __instance.transform.position,
                        __instance.nearByPlayers[j].transform.position
                    ) < component.mainScript.enemyType.pushPlayerDistance
                )
                {
                    vector3 +=
                        Vector3.Normalize(
                            (
                                __instance.transform.position
                                - __instance.nearByPlayers[j].transform.position
                            ) * 100f
                        ) * component.mainScript.enemyType.pushPlayerForce;
                }
            }
            __instance.walkForce = Vector3.MoveTowards(
                maxDistanceDelta: (
                    (__instance.isFallingFromJump || __instance.isFallingNoJump)
                        ? 1.33f
                        : (
                            (__instance.drunkness > 0.3f)
                                ? Mathf.Clamp(Mathf.Abs(__instance.drunkness - 2.25f), 0.3f, 2.5f)
                                : (
                                    (!__instance.isCrouching && __instance.crouchMeter > 1f)
                                        ? 15f
                                        : (
                                            (!__instance.isSprinting)
                                                ? (10f / __instance.carryWeight)
                                                : (5f / (__instance.carryWeight * 1.5f))
                                        )
                                )
                        )
                ) * Time.deltaTime,
                current: __instance.walkForce,
                target: __instance.transform.right * __instance.moveInputVector.x
                    + __instance.transform.forward * __instance.moveInputVector.y
            );
            Vector3 vector4 = __instance.walkForce * num3 * __instance.sprintMultiplier + vector3;
            vector4 += __instance.externalForces;
            if (__instance.externalForceAutoFade.magnitude > 0.05f)
            {
                vector4 += __instance.externalForceAutoFade;
            }
            if (__instance.isPlayerSliding && __instance.thisController.isGrounded)
            {
                __instance.playerSlidingTimer += Time.deltaTime;
                if (__instance.slideFriction > __instance.maxSlideFriction)
                {
                    __instance.slideFriction -= 35f * Time.deltaTime;
                }
                vector4 = new Vector3(
                    vector4.x
                        + (1f - __instance.playerGroundNormal.y)
                            * __instance.playerGroundNormal.x
                            * (1f - __instance.slideFriction),
                    vector4.y,
                    vector4.z
                        + (1f - __instance.playerGroundNormal.y)
                            * __instance.playerGroundNormal.z
                            * (1f - __instance.slideFriction)
                );
            }
            else
            {
                __instance.playerSlidingTimer = 0f;
                __instance.slideFriction = 0f;
            }
            Logger.LogInfo($">> KillPlayer({vector4.ToString()})");
            __instance.KillPlayer(vector4);
        }
    }

    internal static void Patch()
    {
        Harmony ??= new Harmony(MyPluginInfo.PLUGIN_GUID);

        Logger.LogDebug("Patching...");

        Harmony.PatchAll();

        Logger.LogDebug("Finished patching!");
    }

    internal static void Unpatch()
    {
        Logger.LogDebug("Unpatching...");

        Harmony?.UnpatchSelf();

        Logger.LogDebug("Finished unpatching!");
    }
}
