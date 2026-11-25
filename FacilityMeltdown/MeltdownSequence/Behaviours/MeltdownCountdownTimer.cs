using System;
using TMPro;
using UnityEngine;

namespace FacilityMeltdown.MeltdownSequence.Behaviours;

/// <summary>
/// Displays the active meltdown's remaining time using a clone of the game's control-tip HUD style.
/// This component is created locally, so every player can choose whether to see the timer.
/// </summary>
internal sealed class MeltdownCountdownTimer : MonoBehaviour
{
    private GameObject timerObject;
    private TextMeshProUGUI timerText;

    private void Update()
    {
        MeltdownHandler handler = MeltdownHandler.Instance;
        if (handler == null || handler.HasExplosionOccured())
        {
            HideTimer();
            return;
        }

        if (timerText == null && !TryCreateTimer())
        {
            return;
        }

        int remainingSeconds = Mathf.Max(0, Mathf.CeilToInt(handler.TimeLeftUntilMeltdown));
        timerText.text = $"MELTDOWN: {remainingSeconds / 60:00}:{remainingSeconds % 60:00}";
    }

    private bool TryCreateTimer()
    {
        HUDManager hud = HUDManager.Instance;
        if (hud == null || hud.controlTipLines == null || hud.controlTipLines.Length == 0 || hud.controlTipLines[0] == null)
        {
            return false;
        }

        TextMeshProUGUI template = hud.controlTipLines[0];
        timerObject = Instantiate(template.gameObject, template.transform.parent, false);
        timerObject.name = "FacilityMeltdownCountdown";

        if (hud.controlTipLines.Length > 1 && hud.controlTipLines[1] != null)
        {
            float lineSpacing = template.transform.position.y - hud.controlTipLines[1].transform.position.y;
            timerObject.transform.Translate(0f, lineSpacing, 0f);
        }

        timerText = timerObject.GetComponentInChildren<TextMeshProUGUI>(true);
        if (timerText == null)
        {
            MeltdownPlugin.logger.LogWarning("Unable to create the meltdown countdown HUD text.");
            Destroy(timerObject);
            timerObject = null;
            return false;
        }

        timerText.color = Color.red;
        return true;
    }

    private void OnDisable()
    {
        HideTimer();
    }

    private void OnDestroy()
    {
        HideTimer();
    }

    private void HideTimer()
    {
        if (timerObject != null)
        {
            Destroy(timerObject);
            timerObject = null;
            timerText = null;
        }
    }
}
