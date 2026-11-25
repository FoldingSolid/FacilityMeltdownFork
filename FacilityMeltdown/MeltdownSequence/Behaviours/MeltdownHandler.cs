using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using FacilityMeltdown.API;
using FacilityMeltdown.Behaviours;
using FacilityMeltdown.Lang;
using FacilityMeltdown.Util;
using Dawn;
using GameNetcodeStuff;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Experimental.AI;
using UnityEngine.Rendering.HighDefinition;
using Random = UnityEngine.Random;

namespace FacilityMeltdown.MeltdownSequence.Behaviours;
public class MeltdownHandler : NetworkBehaviour
{
    public float TimeLeftUntilMeltdown => meltdownTimer;

    internal float Duration { get; private set; }

    public float Progress => Duration <= 0 ? 0 : Mathf.Clamp01(1 - (TimeLeftUntilMeltdown / Duration));

    static PlayerControllerB Player => GameNetworkManager.Instance.localPlayerController;
    internal static MeltdownHandler Instance { get; private set; }

    internal float meltdownTimer;
    bool meltdownStarted = false;
    float nextCountdownShakeTime;
    internal LungProp causingLungProp;

    GameObject explosion, shockwave;

    [SerializeField]
    AudioSource meltdownMusic, warningSource;

    Vector3 effectOrigin => MeltdownMoonMapper.Instance.EffectOrigin;

    // This is intentionally implemented in the same per-behaviour form as the
    // current Facility Meltdown release. NetcodePatcher 3 generates a global
    // registration that v81 does not use for plugin-added NetworkBehaviours.
    void SendStartMeltdownRpc(int duration)
    {
        NetworkManager networkManager = NetworkManager;
        if (networkManager == null || !networkManager.IsListening)
            return;

        if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
        {
            ClientRpcParams clientRpcParams = default;
            FastBufferWriter bufferWriter = __beginSendClientRpc(3631067672u, clientRpcParams, RpcDelivery.Reliable);
            bufferWriter.WriteValueSafe(in duration, default);
            __endSendClientRpc(ref bufferWriter, 3631067672u, clientRpcParams, RpcDelivery.Reliable);
        }

        if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
        {
            __rpc_exec_stage = __RpcExecStage.Send;
            StartMeltdown(duration);
        }
    }

    void StartMeltdown(int duration)
    {
        if (Instance != null) return;
        Instance = this;

        Stopwatch timer = new Stopwatch();

        MeltdownPlugin.logger.LogInfo($"Beginning Meltdown Sequence! Countdown: {duration} seconds. I'd run if I were you!");

        timer.Start();
        MeltdownMoonMapper.EnsureMeltdownMoonMapper();
        MeltdownInteriorMapper.EnsureMeltdownInteriorMapper();


        if (MeltdownInteriorMapper.Instance == null) MeltdownPlugin.logger.LogError("WHAT. Just ensured that the interior mapper exists and it doesnt?!?");
        if (MeltdownMoonMapper.Instance == null) MeltdownPlugin.logger.LogError("WHAT. Just ensured that the moon mapper exists and it doesnt?!?");

        timer.Stop();
        MeltdownPlugin.logger.LogDebug($"Ensuring data objects exist: {timer.ElapsedMilliseconds}ms");

        Duration = duration;
        meltdownTimer = Duration;

        if (MeltdownPlugin.config.ShowCountdownTimer && !TryGetComponent<MeltdownCountdownTimer>(out _))
        {
            gameObject.AddComponent<MeltdownCountdownTimer>();
        }

        timer.Restart();

        timer.Stop();
        MeltdownPlugin.logger.LogDebug($"Creating audio: {timer.ElapsedMilliseconds}ms");

        timer.Restart();
        #region EFFECTS
        StartCoroutine(
            MeltdownEffects.WithDelay(() =>
            {
                HUDManager.Instance.ReadDialogue(GetDialogue("meltdown.dialogue.start"));
            }, 5)
        );

        StartCoroutine(
            MeltdownEffects.WithDelay(
                MeltdownEffects.RepeatUntilEndOfMeltdown(
                    () => MeltdownEffects.WithRandomDelay(
                        () => MeltdownEffects.WarningAnnouncer(warningSource),
                        10,
                        15
                    ))
            , 10)
        );

        StartCoroutine(
            MeltdownEffects.RepeatUntilEndOfMeltdown(
                () => MeltdownEffects.WithRandomDelay(
                    MeltdownEffects.InsideParticleEffects,
                    10,
                    15
                ))
        );

        if (MeltdownPlugin.config.EmergencyLights)
        {
            try
            {
                List<(Light, Color)> originalLightColours = MeltdownEffects.SetupEmergencyLights();
                StartCoroutine(
                    MeltdownEffects.RepeatUntilEndOfMeltdown(
                        () =>
                        {
                            return MeltdownEffects.EmergencyLights(2, 5, originalLightColours);
                        }
                    )
                );
            }
            catch (Exception e)
            {
                MeltdownPlugin.logger.LogError($"Failed to set the emergency light colour: {e}");
            }
        }

        StartCoroutine(
            MeltdownEffects.RepeatUntilEndOfMeltdown(
                () =>
                {
                    return MeltdownEffects.WithRandomDelay(
                        () =>
                        {
                            if (shockwave != null) GameObject.Destroy(shockwave);

                            shockwave = GameObject.Instantiate(MeltdownPlugin.assets.shockwavePrefab);
                            shockwave.transform.position = effectOrigin;
                        }
                    , 25, 35);
                }
            )
        );

        StartCoroutine(
            MeltdownEffects.AtProgress(
                HUDManager.Instance.RadiationWarningHUD,
                .5f
            )
        );

        #endregion

        timer.Stop();
        MeltdownPlugin.logger.LogDebug($"Starting effects: {timer.ElapsedMilliseconds}ms");

        if (GameNetworkManager.Instance.localPlayerController.IsServer)
        {
            SpawnEnemies();
        }

        timer.Restart();

        if (effectOrigin == Vector3.zero)
        {
            MeltdownPlugin.logger.LogError("Effect Origin is Vector3.Zero! We couldn't find the effect origin");
            HUDManager.Instance.DisplayGlobalNotification("Failed to find effect origin... Things will look broken.");
        }

        if (MeltdownPlugin.config.ScreenShake)
        {
            HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);
            HUDManager.Instance.ShakeCamera(ScreenShakeType.Long);
        }
        timer.Stop();
        MeltdownPlugin.logger.LogDebug($"Getting effect origin: {timer.ElapsedMilliseconds}ms");

        timer.Restart();
        MeltdownAPI.OnMeltdownStart();
        timer.Stop();
        MeltdownPlugin.logger.LogDebug($"MeltdownAPI.OnMeltdownStart(): {timer.ElapsedMilliseconds}ms");
        meltdownStarted = true;
    }

    void SpawnEnemies()
    {
        Stopwatch timer = Stopwatch.StartNew();
        List<string> disallowed = MeltdownPlugin.config.GetDisallowedEnemies();
        List<SpawnableEnemyWithRarity> spawnProbibilities = RoundManager.Instance.currentLevel.Enemies.Where(enemy =>
        {
            if (enemy == null || enemy.enemyType == null)
            {
                MeltdownPlugin.logger.LogWarning("Ignoring an invalid enemy entry in the meltdown spawn pool.");
                return false;
            }

            return enemy.rarity > 0 && !EnemyCannotBeSpawned(enemy.enemyType) && !disallowed.Contains(enemy.enemyType.enemyName) && IsEnemyEligibleForMeltdown(enemy.enemyType);
        }).ToList();

        timer.Stop();
        MeltdownPlugin.logger.LogDebug($"Filtering enemies: {timer.ElapsedMilliseconds}ms");

        timer.Restart();
        List<EnemyVent> avaliableVents = RoundManager.Instance.allEnemyVents.Where(vent =>
        {
            if (vent == null)
            {
                MeltdownPlugin.logger.LogWarning("Ignoring a null enemy vent during the meltdown sequence.");
                return false;
            }

            return !vent.occupied;
        }).ToList();
        timer.Stop();
        MeltdownPlugin.logger.LogDebug($"Filtering vents: {timer.ElapsedMilliseconds}ms");
        timer.Restart();


        if (spawnProbibilities.Count == 0)
        {
            MeltdownPlugin.logger.LogWarning("No eligible enemies are available to spawn during the meltdown sequence.");
            return;
        }

        int spawnCount = Mathf.Min(MeltdownPlugin.config.MonsterSpawnAmount, avaliableVents.Count);
        System.Random spawnRandom = new System.Random(StartOfRound.Instance.randomMapSeed + 1923847);
        for (int i = 0; i < spawnCount; i++)
        {
            EnemyVent vent = avaliableVents[spawnRandom.Next(avaliableVents.Count)];
            avaliableVents.Remove(vent);
            SpawnableEnemyWithRarity enemy = spawnProbibilities[spawnRandom.Next(spawnProbibilities.Count)];

            RoundManager.Instance.currentEnemyPower += enemy.enemyType.PowerLevel;
            MeltdownPlugin.logger.LogInfo("Spawning a " + enemy.enemyType.enemyName + " during the meltdown sequence");
            vent.SpawnEnemy(enemy);
        }

        timer.Stop();
        MeltdownPlugin.logger.LogDebug($"Spawning enemies: {timer.ElapsedMilliseconds}ms");

        if (MeltdownPlugin.config.ScareBabyManeaters && causingLungProp != null)
        {
            foreach (CaveDwellerAI maneater in FindObjectsOfType<CaveDwellerAI>())
            {
                if (maneater.currentBehaviourStateIndex == 0)
                {
                    maneater.ScareBaby(causingLungProp.transform.position);
                }
            }
        }
    }

    bool IsEnemyEligibleForMeltdown(EnemyType enemyType)
    {
        DawnEnemyInfo dawnInfo = EnemyTypeExtensions.GetDawnInfo(enemyType);
        if (dawnInfo == null)
        {
            MeltdownPlugin.logger.LogWarning($"{enemyType.name} has no DawnEnemyInfo and will not spawn during the meltdown sequence.");
            return false;
        }

        return !dawnInfo.HasTag(MeltdownTags.NotSpawnable) && !dawnInfo.HasTag(Tags.Unimplemented);
    }

    public override void OnNetworkSpawn()
    {
        if (!MeltdownPlugin.loadedFully || !IsServer)
        {
            return;
        }

        MeltdownMoonMapper.EnsureMeltdownMoonMapper();
        MeltdownInteriorMapper.EnsureMeltdownInteriorMapper();

        if (effectOrigin == Vector3.zero)
        {
            MeltdownPlugin.logger.LogError("Effect origin is Vector3.zero; the meltdown will not start on this moon.");
            return;
        }

        int duration = MeltdownPlugin.config.MeltdownTime;
        MeltdownPlugin.logger.LogInfo($"Starting meltdown with host-configured duration: {duration} seconds.");
        SendStartMeltdownRpc(duration);
    }

    protected override void __initializeRpcs()
    {
        __registerRpc(3631067672u, __rpc_handler_3631067672, "StartMeltdownClientRpc");
        base.__initializeRpcs();
    }

    static void __rpc_handler_3631067672(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
    {
        if (target is not MeltdownHandler handler)
            return;

        NetworkManager networkManager = handler.NetworkManager;
        if (networkManager == null || !networkManager.IsListening)
            return;

        if (!reader.TryBeginRead(sizeof(int)))
        {
            MeltdownPlugin.logger.LogError("Meltdown start RPC was missing its duration.");
            return;
        }

        reader.ReadValueSafe(out int duration, default);

        handler.__rpc_exec_stage = __RpcExecStage.Execute;
        handler.SendStartMeltdownRpc(duration);
        handler.__rpc_exec_stage = __RpcExecStage.Send;
    }

    internal bool EnemyCannotBeSpawned(EnemyType type)
    {
        return type.spawningDisabled || type.numberSpawned >= type.MaxCount;
    }

    internal static DialogueSegment[] GetDialogue(string translation)
    {
        JArray translatedDialogue = LangParser.GetTranslationSet(translation);
        DialogueSegment[] dialogue = new DialogueSegment[translatedDialogue.Count];
        for (int i = 0; i < translatedDialogue.Count; i++)
        {
            dialogue[i] = new DialogueSegment
            {
                bodyText = ((string)translatedDialogue[i]).Replace("<meltdown_time>", ((Instance?.Duration ?? MeltdownPlugin.config.MeltdownTime) / 60f)
                    .ToString("0.#", CultureInfo.InvariantCulture)),
                speakerText = "meltdown.dialogue.speaker".Translate()
            };
        }
        return dialogue;
    }

    void OnDisable()
    {
        if (Instance != this) return;

        MeltdownPlugin.logger.LogInfo("Cleaning up MeltdownHandler.");

        Instance = null;
        if (explosion != null)
            Destroy(explosion);
        if (shockwave != null)
            Destroy(shockwave);

        if (!meltdownStarted)
        {
            MeltdownPlugin.logger.LogError("MeltdownHandler was disabled without starting a meltdown, a client most likely failed the MeltdownReadyCheck. If you are going to report this make sure to provide ALL client logs.");
        }
    }

    void Update()
    {
        if (!meltdownStarted) return;
        if (HasExplosionOccured()) return;
        StartOfRound shipManager = StartOfRound.Instance;

        meltdownMusic.volume = MeltdownPlugin.config.MusicVolume / 100f;

        if (!Player.isInsideFactory && !MeltdownPlugin.config.MusicPlaysOutside)
        {
            meltdownMusic.volume = 0;
        }

        meltdownTimer -= Time.deltaTime;
        UpdateCountdownScreenShake();

        if (meltdownTimer <= 3 && !shipManager.shipIsLeaving)
        {
            StartCoroutine(ShipTakeOff());
        }

        if (meltdownTimer <= 0)
        {
            meltdownMusic.Stop();

            GameObject explosionPrefab = MeltdownMoonMapper.Instance.explosionPrefab;
            if (explosionPrefab == null)
                explosionPrefab = MeltdownPlugin.assets.facilityExplosionPrefab;

            explosion = Instantiate(explosionPrefab);
            explosion.transform.position = effectOrigin;
            if (!explosion.TryGetComponent<FacilityExplosionHandler>(out var _))
            {
                explosion.AddComponent<FacilityExplosionHandler>();
            }
        }
    }

    // This is deliberately independent of the intermittent environmental shakes.
    // It establishes a reliable sense of increasing instability during the latter
    // half of every meltdown, while the duration snapshot keeps all clients aligned.
    void UpdateCountdownScreenShake()
    {
        if (!MeltdownPlugin.config.ScreenShake || Duration <= 0 || Progress < 0.5f || Time.time < nextCountdownShakeTime)
            return;

        float intensity = Mathf.InverseLerp(0.5f, 1f, Progress);
        ScreenShakeType shake = intensity switch
        {
            < 0.33f => ScreenShakeType.Small,
            < 0.7f => ScreenShakeType.Big,
            _ => ScreenShakeType.VeryStrong
        };

        HUDManager.Instance.ShakeCamera(shake);

        // Start with occasional tremors at the halfway point, then approach a
        // rapid, violent rumble as the reactor reaches critical state.
        nextCountdownShakeTime = Time.time + Mathf.Lerp(4f, 0.45f, intensity);
    }

    IEnumerator ShipTakeOff()
    {
        StartOfRound shipManager = StartOfRound.Instance;
        shipManager.shipLeftAutomatically = true;
        shipManager.shipIsLeaving = true;

        HUDManager.Instance.ReadDialogue(GetDialogue("meltdown.dialogue.shiptakeoff"));

        yield return new WaitForSeconds(3f); // Wait for explosion
        yield return new WaitForSeconds(3f);
        HUDManager.Instance.shipLeavingEarlyIcon.enabled = false;
        StartMatchLever startMatchLever = FindObjectOfType<StartMatchLever>();
        startMatchLever.triggerScript.animationString = "SA_PushLeverBack";
        startMatchLever.leverHasBeenPulled = false;
        startMatchLever.triggerScript.interactable = false;
        startMatchLever.leverAnimatorObject.SetBool("pullLever", false);
        shipManager.ShipLeave();
        yield return new WaitForSeconds(1.5f);
        shipManager.SetSpectateCameraToGameOverMode(true, null);
        if (GameNetworkManager.Instance.localPlayerController.isPlayerDead)
        {
            GameNetworkManager.Instance.localPlayerController.SetSpectatedPlayerEffects(true);
        }
        yield return new WaitForSeconds(1f);

        yield return new WaitForSeconds(9.5f);

        yield break;
    }

    public bool HasExplosionOccured()
    {
        return explosion != null;
    }
}
