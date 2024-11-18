using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class ResultSceneController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI songNameText;
    [SerializeField] private TextMeshProUGUI artistNameText;
    [SerializeField] private TextMeshProUGUI feedbackDebugText;
    [SerializeField] private RawImage songSquare;

    [SerializeField] private TextMeshProUGUI perfectText;
    [SerializeField] private TextMeshProUGUI coolText;
    [SerializeField] private TextMeshProUGUI passableText;
    [SerializeField] private TextMeshProUGUI badText;
    [SerializeField] private TextMeshProUGUI missText;

    [SerializeField] private Button continueButton;

    private Song currentSong;
    // Start is called before the first frame update
    void Start()
    {
        SongManager.Instance.PlayMainMenuMusic();
        string currentSongCodename = PlayerPrefs.GetString("Current Song");
        string squarePath = "Squares/" + currentSongCodename;
        Texture2D squareTexture = Resources.Load<Texture2D>(squarePath);

        songSquare.texture = squareTexture;
        currentSong = SongManager.Instance.GetSongByCodename(currentSongCodename);
        
        float idealFeedbackCount = (currentSong.End_frame - currentSong.Start_frame) / 45;
        int playerFeedbackCount = PlayerPrefs.GetInt("feedback count");
        Debug.Log("Ideal feedback count: "+idealFeedbackCount);
        string feedbackDebug = $"{playerFeedbackCount}";
        feedbackDebugText.text = feedbackDebug;

        songNameText.text = currentSong.Song_name;
        artistNameText.text = currentSong.Artist;

        continueButton.onClick.AddListener(OnContinueButtonPressed);
        SetScore();
    }

    private void OnContinueButtonPressed()
    {
        SceneManagement.Instance.ChangeSceneAsync("Songlist");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void SetScore()
    {
        var score = Score.Instance;
        scoreText.text = $"{score.GetTotalScore():000}";
        perfectText.text = $"{score.GetPerfectTotal():000}";
        coolText.text = $"{score.GetCoolTotal():000}";
        passableText.text = $"{score.GetPassableTotal():000}";
        badText.text = $"{score.GetBadTotal():000}";
        missText.text = $"{score.GetMissTotal():000}";
    }
}
