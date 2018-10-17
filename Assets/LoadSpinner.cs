using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoadSpinner : MonoBehaviour
{
    public RawImage spinnerElement;

    public static bool isLoading;

    private Interpolator fadeInterp = new Interpolator(0.0f, Interpolator.CurveType.SmoothStep);

    void Update()
    {
        var loading = Game.game == null || Game.game.levelManager == null || Game.game.levelManager.IsLoadingLevel();
        if (loading && fadeInterp.targetValue < 1.0f)
            fadeInterp.MoveTo(1.0f, 0.5f);
        else if (!loading && fadeInterp.targetValue > 0.0f)
            fadeInterp.MoveTo(0.0f, 0.5f);

        var fadeValue = fadeInterp.GetValue();

        if (fadeValue <= 0.0f)
        {
            spinnerElement.enabled = false;
            return;
        }

        spinnerElement.enabled = true;

        var fadeColor = new Color(1.0f, 1.0f, 1.0f, fadeValue);
        spinnerElement.color = fadeColor;


        spinnerElement.transform.Rotate(0, 0, -1.7f);
    }
}
