using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleSceneController : MonoBehaviour
{
    [SerializeField] private CanvasGroup loopFadingText;
    [SerializeField] private CanvasGroup mainCanvasGroup;
    [SerializeField] private Button advanceSceneButton;

    public float fadeDuration = 1f;

    private IEnumerator FadeLoop()
    {
        while (true)
        {
            // Fade in
            float elapsedTime = 0f;
            while (elapsedTime < fadeDuration)
            {
                loopFadingText.alpha = elapsedTime / fadeDuration;
                elapsedTime += Time.deltaTime;
                yield return null;

            }

            // Fade out
            elapsedTime = 0f;
            while (elapsedTime < fadeDuration)
            {
                loopFadingText.alpha = 1f - elapsedTime / fadeDuration;
                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }
    }
    private IEnumerator FadeThings(float targetValue)
    {
        mainCanvasGroup.blocksRaycasts = targetValue > 0; // Block interactions during fade in

        float startValue = mainCanvasGroup.alpha;
        float elapsedTime = 0;


        while (elapsedTime < 0.5f)
        {
            elapsedTime += Time.deltaTime;
            float currentAlpha = Mathf.Lerp(startValue, targetValue, elapsedTime / 0.5f);
            mainCanvasGroup.alpha = currentAlpha;
            yield return null;
        }

        mainCanvasGroup.alpha = targetValue; // Ensure we reach exact target
    }
    void Start()
    {
        StartCoroutine(FadeThings(1f));
        StartCoroutine(FadeLoop());
        advanceSceneButton.onClick.AddListener(OnButtonPressed);
    }

    private void OnButtonPressed()
    {
        SceneManagement.Instance.ChangeSceneAsync("Webcam Setting");
    }
}
