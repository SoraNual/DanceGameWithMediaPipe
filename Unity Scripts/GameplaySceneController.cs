using NativeWebSocket;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class GameplaySceneController : MonoBehaviour
{
    [SerializeField] private RawImage _rawImage;
    private WebCamTexture _webCamTexture;

    private RelativeAnglesData anglesData;
    [SerializeField] private VideoPlayer videoPlayer;

    private long currentComparingFrame = 0;
    private int feedbackCount = 0;
    private int totalScore = 0;

    [SerializeField] private TextMeshProUGUI feedbackText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI countdownText;

    private int sentFrame = 0;
    private Song currentSong;
    private bool flipWebcam = false;
    private Dictionary<string, int> feedbacks = new();

    [SerializeField] private Button backButton;
    [SerializeField] private Button quitButton;

    [SerializeField] private FeedbackManager feedbackManager;

    async void Start()
    {
        SetUpPlayerWebcam();
        SetUpScoreRelatedStuffs();
        SetUpButtons();
        
        // Check if the client is already connected
        if (!ClientConnectionManager.Instance.IsConnected)
        {
            bool connected = await ClientConnectionManager.Instance.ConnectToServerWithLaunch();
            if (!connected)
            {
                Debug.LogError("Failed to connect to server!");
                return;
            }
        }

        SendSelectedSong();
        SetUpVideoPlayer();
    }

    IEnumerator CountdownBeforeSongStart(int seconds)
    {
        videoPlayer.Prepare();
        countdownText.text = "เตรียมวิดีโออยู่จ้า...";
        while (!videoPlayer.isPrepared)
        {
            yield return null;
        }
        

        int count = seconds;
        while (count > 0)
        {
            countdownText.text = count.ToString();
            yield return new WaitForSeconds(1);
            count--;
        }
        countdownText.text = "";
        videoPlayer.Play();
        StartCoroutine(SendFramesPeriodically(1f));
    }

    IEnumerator SendFramesPeriodically(float interval)
    {
        while (videoPlayer.isPlaying)
        {
            if (_webCamTexture.isPlaying && videoPlayer.frame >= currentSong.Start_frame
                && videoPlayer.frame <= currentSong.End_frame)
            {
                SendFrame();
            }
            yield return new WaitForSeconds(interval);
        }
    }

    void OnLoopPointReached(VideoPlayer source)
    {
        PlayerPrefs.SetInt("score", totalScore);
        PlayerPrefs.SetInt("feedback count", feedbackCount);
        Debug.Log("Video finished playing!");
        SceneManagement.Instance.ChangeSceneAsync("Result");
    }

    private void SetUpPlayerWebcam()
    {
        WebCamDevice[] devices = WebCamTexture.devices;
        _webCamTexture = new WebCamTexture(devices[0].name);
        this._rawImage.texture = _webCamTexture;
        if (PlayerPrefs.GetString("flip webcam") == "true")
        {
            flipWebcam = true;
            _rawImage.rectTransform.localScale = new Vector3(-1, 1, 1);
        }
        _webCamTexture.requestedFPS = 30;
        _webCamTexture.Play();
    }

    private void SetUpVideoPlayer()
    {
        currentSong = SongManager.Instance.GetSongByCodename(PlayerPrefs.GetString("Current Song"));
        Debug.Log(currentSong.Song_name);
        string vidPath = "Videos/" + PlayerPrefs.GetString("Current Song");
        VideoClip referenceClip = Resources.Load<VideoClip>(vidPath);
        videoPlayer.clip = referenceClip;

        if (SongManager.Instance != null) SongManager.Instance.StopMusic();
        videoPlayer.SetTargetAudioSource(0, SongManager.Instance.GetVideoAudioSource());

        StartCoroutine(CountdownBeforeSongStart(5));

        countdownText.text = "";
        Debug.Log($"Video's framerate is...{videoPlayer.frameRate} fps");
        Debug.Log($"Video's total frame count is {videoPlayer.frameCount}");

        Debug.Log(currentSong.Start_frame + "<< Start , End >>" + currentSong.End_frame);
        videoPlayer.loopPointReached += OnLoopPointReached;
    }
    private void SetUpScoreRelatedStuffs()
    {
        scoreText.text = totalScore.ToString();
        Score.Instance.ResetScore();
        feedbacks.Add("เพอร์เฟกต์", 100);
        feedbacks.Add("กำลังดี", 60);
        feedbacks.Add("พอไปได้", 40);
        feedbacks.Add("แย่หน่อย", 20);
        feedbacks.Add("พลาด", 0);
    }
    private void SetUpButtons()
    {
        backButton.onClick.AddListener(OnBackButtonClicked);
        quitButton.onClick.AddListener(OnQuitButtonClicked);
    }
    private void OnBackButtonClicked()
    {
        SceneManagement.Instance.ChangeSceneAsync("Songlist");
    }
    private void OnQuitButtonClicked()
    {
        SceneManagement.Instance.QuitGame();
    }

    void Update()
    {
        currentComparingFrame = videoPlayer.frame;
    }

    async void SendFrame()
    {
        if (!ClientConnectionManager.Instance.IsConnected) return;

        int targetWidth = 640;
        int targetHeight = 360;
        Debug.Log($"Try sending Frame to Python server at Frame#{videoPlayer.frame}.");
        try
        {
            var rt = RenderTexture.GetTemporary(targetWidth, targetHeight, 24);
            Graphics.Blit(_webCamTexture, rt);
            RenderTexture.active = rt;

            var texture = new Texture2D(targetWidth, targetHeight, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
            texture.Apply();

            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);

            var bytes = texture.EncodeToJPG(70);
            Destroy(texture);

            string base64Image = Convert.ToBase64String(bytes);

            string jsonMessage = JsonConvert.SerializeObject(new
            {
                type = "frame_data",
                frame_number = currentComparingFrame,
                image_data = base64Image,
                flip = flipWebcam
            });

            await ClientConnectionManager.Instance.SendMessage(jsonMessage);
            Debug.Log($"Sent frame number{currentComparingFrame}");

            sentFrame++;
            Debug.Log("Total Frame Sent = " + sentFrame);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error sending frame: {e.Message}");
        }
    }

    async void SendSelectedSong()
    {
        if (!ClientConnectionManager.Instance.IsConnected) return;

        try
        {
            string jsonMessage = JsonConvert.SerializeObject(new
            {
                type = "song_selection",
                song_name = PlayerPrefs.GetString("Current Song"),
                app_path = Application.streamingAssetsPath
            });
            Debug.Log(jsonMessage);

            await ClientConnectionManager.Instance.SendMessage(jsonMessage);
            Debug.Log("sent selected song named " + PlayerPrefs.GetString("Current Song"));
        }
        catch (Exception e)
        {
            Debug.LogError($"Error sending selected song: {e.Message}");
        }
    }

    public void UpdateScoreAndFeedbackUsingDTW(double DTWdistance)
    {

        string currentFeedback;
        if (DTWdistance <= 18.0) currentFeedback = "เพอร์เฟกต์";
        else if (DTWdistance <= 21.5) currentFeedback = "กำลังดี";
        else if (DTWdistance <= 24.0) currentFeedback = "พอไปได้";
        else if (DTWdistance <= 30.0) currentFeedback = "แย่หน่อย";
        else currentFeedback = "พลาด";

        int feedbackScore = feedbacks[currentFeedback];
        totalScore += feedbackScore;

        // Spawn animated feedback
        feedbackManager.SpawnFeedback(currentFeedback, feedbackScore);

        // Update persistent score display
        scoreText.text = totalScore.ToString();

        // Update other game stats
        Score.Instance.AddFeedback(currentFeedback, feedbackScore);
        feedbackCount++;

        Debug.Log($"DTWdistance is {DTWdistance} Which get {currentFeedback}");
    }

    public void UpdateScoreAndFeedbackUsingCorrectness(double correctness)
    {

        string currentFeedback;
        if (correctness >= 0.8) currentFeedback = "เพอร์เฟกต์";
        else if (correctness >= 0.75) currentFeedback = "กำลังดี";
        else if (correctness >= 0.73) currentFeedback = "พอไปได้";
        else if (correctness >= 0.7) currentFeedback = "แย่หน่อย";
        else currentFeedback = "พลาด";

        int feedbackScore = feedbacks[currentFeedback];
        totalScore += feedbackScore;

        // Spawn animated feedback
        feedbackManager.SpawnFeedback(currentFeedback, feedbackScore);

        // Update persistent score display
        scoreText.text = totalScore.ToString();

        // Update other game stats
        Score.Instance.AddFeedback(currentFeedback, feedbackScore);
        feedbackCount++;

        Debug.Log($"Correctness is {correctness} Which get {currentFeedback}");
    }

    private void OnDisable()
    {
        if (videoPlayer != null) videoPlayer.Stop();
        Debug.Log("Webcam has stopped in gameplay scene");
        _webCamTexture.Stop();
        Resources.UnloadUnusedAssets();
        System.GC.Collect();
    }
}