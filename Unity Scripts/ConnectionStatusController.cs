using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class ConnectionStatusController : MonoBehaviour
{
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text disclaimerText;
    [SerializeField] private string nextSceneName = "Start";
    [SerializeField] private CanvasGroup canvasGroup;
    private int connectionAttempt = 0;

    private void Start()
    {
        StartCoroutine(InitializeConnection());
    }

    private IEnumerator InitializeConnection()
    {
        statusText.text = "กำลังเตรียมเวที";
        Debug.Log("InitializeConnection started");

        bool connected = false;
        while (!connected)
        {
            Debug.Log($"Connection attempt #{connectionAttempt + 1}");
            var clientManager = ClientConnectionManager.Instance;

            // Start the connection process
            var connectionTask = clientManager.ConnectToServerWithLaunch();

            // Wait for Unity to process a few frames to allow WebSocket messages to be handled
            yield return new WaitForSeconds(0.5f);

            // Check connection status for up to 5 seconds
            float timeout = 5f;
            float elapsed = 0f;

            while (elapsed < timeout && !connected)
            {
                // Allow WebSocket messages to be processed
                yield return new WaitForEndOfFrame();

                // Check if we're connected
                if (clientManager.IsConnected)
                {
                    connected = true;
                    Debug.Log("Connection verified successful!");
                    statusText.text = "เวทีนี้ พร้อมสำหรับคุณแล้ว!";
                    yield return new WaitForSeconds(1);
                    LoadNextScene();
                    yield break;
                }

                elapsed += Time.deltaTime;
                //Debug.Log($"{Mathf.Round(elapsed)}/{timeout} (s)");
                statusText.text = "กำลังเตรียมเวที";
                for (int i = 0; i < elapsed; i++)
                {
                    statusText.text += ".";
                }
            }

            if (!connected)
            {
                connectionAttempt++;
                Debug.Log($"Connection failed. Total attempts: {connectionAttempt}");
                statusText.text = "เอ๊ะ เหมือนจะเจอเจ้าตัวป่วน...";
                yield return new WaitForSeconds(2);
            }
        }
    }
    private void LoadNextScene()
    {
        StartCoroutine(FadeThings(1f));
        
    }
    private IEnumerator FadeThings(float targetValue)
    {
        canvasGroup.blocksRaycasts = targetValue > 0; // Block interactions during fade in

        float startValue = canvasGroup.alpha;
        float elapsedTime = 0;


        while (elapsedTime < 0.5f)
        {
            elapsedTime += Time.deltaTime;
            float currentAlpha = Mathf.Lerp(startValue, targetValue, elapsedTime / 0.5f);
            canvasGroup.alpha = currentAlpha;
            statusText.alpha = 1 - currentAlpha;
            disclaimerText.alpha = 1 - currentAlpha;
            yield return null;
        }

        canvasGroup.alpha = targetValue; // Ensure we reach exact target
        SceneManager.LoadScene(nextSceneName);
    }
}