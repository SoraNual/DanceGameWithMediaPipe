using UnityEngine;
using TMPro;

public class FeedbackManager : MonoBehaviour
{
    [Header("Prefab Reference")]
    [SerializeField] private TextMeshProUGUI feedbackPrefab;
    [SerializeField] private Transform canvasTransform;

    [Header("Feedback Settings")]
    [SerializeField] private Vector2 spawnOffset = new Vector2(0f, 50f);
    [SerializeField] private Vector2 feedbackPosition = new Vector2(0f, 0f); // Center position for feedback

    // Cache the feedback colors
    private readonly Color perfectColor = Color.green;
    private readonly Color coolColor = Color.cyan;
    private readonly Color passableColor = Color.yellow;
    private readonly Color badColor = Color.magenta;
    private readonly Color missColor = Color.red;

    public void SpawnFeedback(string rating, int score)
    {
        TextMeshProUGUI feedbackInstance = Instantiate(feedbackPrefab, canvasTransform);

        // Position the feedback in the center of the screen with offset
        feedbackInstance.rectTransform.anchoredPosition = feedbackPosition + spawnOffset;

        // Set the color based on rating
        Color feedbackColor = rating switch
        {
            "à¾ÍÃìà¿¡µì" => perfectColor,
            "¡ÓÅÑ§´Õ" => coolColor,
            "¾Íä»ä´é" => passableColor,
            "áÂèË¹èÍÂ" => badColor,
            "¾ÅÒ´" => missColor,
            _ => Color.white
        };

        AnimateFeedback(feedbackInstance, rating, feedbackColor, score);
    }

    private void AnimateFeedback(TextMeshProUGUI feedbackText, string rating, Color color, int score)
    {
        // Reset state
        feedbackText.color = color;
        feedbackText.text = rating;
        feedbackText.transform.localScale = Vector3.one;

        // Store initial position
        Vector2 startPos = feedbackText.rectTransform.anchoredPosition;

        // Create score text
        if (score > 0)
        {
            TextMeshProUGUI scoreInstance = Instantiate(feedbackPrefab, canvasTransform);
            scoreInstance.rectTransform.anchoredPosition = startPos + new Vector2(0, -60f);
            scoreInstance.text = $"+{score}";
            scoreInstance.fontSize *= 0.8f; // Make score text slightly smaller

            // Animate score text
            LeanTween.value(scoreInstance.gameObject, 1f, 0f, 0.5f)
                .setDelay(0.3f)
                .setOnUpdate((float value) => {
                    Color newColor = color;
                    newColor.a = value;
                    scoreInstance.color = newColor;
                })
                .setOnComplete(() => {
                    Destroy(scoreInstance.gameObject);
                });
        }

        // Main feedback animation sequence
        LeanTween.scale(feedbackText.gameObject, Vector3.one * 1.2f, 0.1f)
            .setEase(LeanTweenType.easeOutBack)
            .setOnComplete(() => {
                // Float up
                LeanTween.moveY(feedbackText.rectTransform, startPos.y + 30f, 0.4f)
                    .setEase(LeanTweenType.easeOutCubic);

                // Scale down and fade out
                LeanTween.scale(feedbackText.gameObject, Vector3.one * 0.8f, 0.4f)
                    .setDelay(0.2f);

                LeanTween.value(feedbackText.gameObject, 1f, 0f, 0.4f)
                    .setDelay(0.2f)
                    .setOnUpdate((float value) => {
                        Color newColor = feedbackText.color;
                        newColor.a = value;
                        feedbackText.color = newColor;
                    })
                    .setOnComplete(() => {
                        Destroy(feedbackText.gameObject);
                    });
            });
    }
}