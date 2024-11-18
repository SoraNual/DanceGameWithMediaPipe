using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using static SongCardManager;

public class SonglistSceneController : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button backButton;

    [Header("Songs")]
    [SerializeField] private List<Image> songCards;
    [SerializeField] private TMP_Text songText;
    [SerializeField] private TMP_Text artistText;

    [Header("Video")]
    [SerializeField] private TMP_Text loadingVidText;
    [SerializeField] private VideoPlayer vidPlayer;
    [SerializeField] private RenderTexture renderTexture;
    [SerializeField] private RawImage vidContainer;
    
    private string songCodename;
    private string previousSongCodeName;
    // Start is called before the first frame update
    void Start()
    {
        vidPlayer.SetTargetAudioSource(0,SongManager.Instance.GetVideoAudioSource());
        loadingVidText.text = "";
        artistText.text = "";
        if (SongManager.Instance != null && !SongManager.Instance.IsPlayingMainMenuMusic())
            SongManager.Instance.PlayMainMenuMusic();
        SetButtons();
    }

    private void SetButtons()
    {
        playButton.interactable = false;

        foreach (Image songCard in songCards)
        {
            songCard.GetComponent<Button>().onClick.AddListener(() => { SelectSong(songCard.transform.Find("square")?.GetComponent<RawImage>()); });
        }

        playButton.onClick.AddListener(OnPlayButtonPressed);
        quitButton.onClick.AddListener(OnQuitButtonPressed);
        backButton.onClick.AddListener(OnBackButtonPressed);
    }

    private void SelectSong(RawImage songSquare)
    {

        playButton.interactable = true;
        songCodename = songSquare.texture.name;
        Song selectedSong = SongManager.Instance.GetSongByCodename(songCodename);

        if (songCodename != previousSongCodeName)
        {
            StopPreviewVid();
            songText.text = $"{selectedSong.Song_name}";
            artistText.text = selectedSong.Artist;
            Debug.Log($"Selected: {songCodename}");

            string vidPath = "Videos/" + songCodename;
            VideoClip previewClip = Resources.Load<VideoClip>(vidPath);
            Debug.Log(vidPath);
            Debug.Log(previewClip);

            SongManager.Instance.StopMusic();
            previousSongCodeName = songCodename;
            PlayPreviewVid(previewClip);
        
        }
    }


    private void StopPreviewVid() 
    { 
        vidPlayer.Stop();
    }
    private void PlayPreviewVid(VideoClip previewClip)
    {
        
        Debug.Log("Try to play a preview");
        vidPlayer.clip = previewClip;
        StartCoroutine(PreparePreviewVid());
        
    }

    IEnumerator PreparePreviewVid()
    {
        loadingVidText.text = "°”≈—ß‚À≈¥...";
        vidPlayer.Prepare();
        while (!vidPlayer.isPrepared)
        {
            yield return null; // Wait for preparation
        }
        loadingVidText.text = "";
        vidPlayer.Play();
    }

    private void OnPlayButtonPressed()
    {
        SongManager.Instance.PlayPlayButtonSFX();
        PlayerPrefs.SetString("Current Song", songCodename);
        Debug.Log($"The current song should be {PlayerPrefs.GetString("Current Song")}");
        SceneManagement.Instance.ChangeSceneAsync("Gameplay");
    }

    private void OnQuitButtonPressed()
    {
        SceneManagement.Instance.QuitGame();
    }

    private void OnBackButtonPressed()
    {
        SceneManagement.Instance.ChangeSceneAsync("Webcam Setting");
    }
    private void OnDisable()
    {
        if (vidPlayer != null) StopPreviewVid();
        if (renderTexture != null)
        {
            renderTexture.Release();
            renderTexture = null;
        }

        // Unload unused assets synchronously
        Resources.UnloadUnusedAssets();

        // Force a garbage collection
        System.GC.Collect();
    }

    private void OnApplicationQuit()
    {
        //if(renderTexture!=null) renderTexture.Release();
    }
}
