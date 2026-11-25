using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FacilityMeltdown.Util;
using GameNetcodeStuff;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace FacilityMeltdown.MeltdownSequence.Behaviours
{
    internal class FacilityExplosionHandler : MonoBehaviour
    {
        PlayerControllerB player;
        float size, time;

        LocalVolumetricFog internalFog;

        void Awake()
        {
            player = GameNetworkManager.Instance.localPlayerController;

            // Some moon-specific explosion prefabs place the fog on a child object.
            // It is cosmetic, so continue safely if the prefab does not include it.
            internalFog = GetComponentInChildren<LocalVolumetricFog>(true);
            if (internalFog == null)
            {
                MeltdownPlugin.logger.LogWarning("Explosion prefab has no volumetric fog; continuing without the expanding fog effect.");
            }

            if (MeltdownPlugin.config.ScreenShake)
            {
                HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
                HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
                HUDManager.Instance.ShakeCamera(ScreenShakeType.Long);
                HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);
            }

            if (!player.isPlayerDead && player.isInsideFactory)
            {
                if (player.isInElevator)
                {
                    MeltdownPlugin.logger.LogWarning("Player is inside ship and facility at the same time!! Did you teleport out? Aborting kill");
                    return;
                }
                KillPlayer();
            }
        }

        void Update()
        {
            time += Time.deltaTime * 10;
            size = TimeToSize(time);

            transform.localScale = Vector3.one * size;
            if (internalFog != null)
            {
                internalFog.parameters.size = Vector3.one * size * 1.25f;
            }

            if (!ShouldIgnorePlayer() && PlayerIsInsideFireball())
            {
                player.KillPlayer(Vector3.zero, false, CauseOfDeath.Blast);
            }
        }

        void KillPlayer()
        {
            player.KillPlayer(Vector3.zero, false, CauseOfDeath.Blast);
        }

        bool PlayerIsInsideFireball()
        {
            return Vector3.Distance(transform.position, player.transform.position) < size;
        }

        bool ShouldIgnorePlayer()
        {
            return player.isPlayerDead || player.isInElevator && StartOfRound.Instance.shipIsLeaving;
        }

        float TimeToSize(float time)
        {
            return Mathf.Log(time) + 3 + 2 * time;
        }
    }
}
