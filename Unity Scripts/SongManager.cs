using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

public class SongManager : MonoBehaviour
{
    public static SongManager Instance { get; private set; }

    // Dictionary to hold song data
    public Dictionary<string, Song> songData = new Dictionary<string, Song>();

    [SerializeField] private TextAsset jsonSongData;
    [SerializeField] private AudioClip mainMenuMusic;
    [SerializeField] private AudioClip startButtonSFX;
    [SerializeField] private AudioClip playButtonSFX;
    
    [SerializeField] private AudioSource musicAudioSource;
    [SerializeField] private AudioSource videoAudioSource;
    [SerializeField] private AudioSource sfxAudioSource;

    private void Awake()
    {
        // Ensure that only one instance of the SongManager exists
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);  // This keeps the object alive between scenes
            LoadSongData(jsonSongData.text);
            musicAudioSource.clip = mainMenuMusic;
            PlayMusic();

        }
        else
        {
            Destroy(gameObject);  // Destroy any duplicate instances
        }
    }

    public void LoadSongData(string json)
    {
        // Deserialize JSON into songData dictionary
        songData = JsonConvert.DeserializeObject<Dictionary<string, Song>>(json);
    }

    public Song GetSongByCodename(string codename)
    {
        return songData.ContainsKey(codename) ? songData[codename] : null;
    }

    public void PauseMusic()
    {
        musicAudioSource.Pause();
    }

    public void StopMusic() { if (musicAudioSource.isPlaying) musicAudioSource.Stop();} 

    public void PlayMusic() { if(!musicAudioSource.isPlaying) musicAudioSource.Play();}
    public void PlayMainMenuMusic()
    {
        if (!musicAudioSource.isPlaying)
            musicAudioSource.clip = mainMenuMusic;
        PlayMusic();
    }

    public void PlayStartButtonSFX()
    {
        sfxAudioSource.clip = startButtonSFX;
        sfxAudioSource.Play();
    }

    public void PlayPlayButtonSFX()
    {
        sfxAudioSource.clip = playButtonSFX;
        sfxAudioSource.Play();
    }

    public AudioSource GetMusicAudioSource()
    {
        return musicAudioSource;
    }

    public AudioSource GetVideoAudioSource()
    {
        return videoAudioSource;
    }

    public Boolean IsPlayingMainMenuMusic() { return musicAudioSource.isPlaying && musicAudioSource.clip == mainMenuMusic; }
}