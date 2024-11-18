using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingSceneController : MonoBehaviour
{
    [SerializeField] private Toggle flipWebCamToggle;
    [SerializeField] private WebCamTexture webCamTexture;
    [SerializeField] private RawImage rawImage;

    [Header("Buttons")]
    [SerializeField] private Button continueButton;
    [SerializeField] private Button quitButton;

    // Start is called before the first frame update
    void Start()
    {
        WebCamDevice[] devices = WebCamTexture.devices;
        // for debugging purposes, prints available devices to the console
        for (int i = 0; i < devices.Length; i++)
        {
            print("Webcam available: " + devices[i].name);
        }
        webCamTexture = new WebCamTexture(devices[0].name);
        rawImage = FindObjectOfType<RawImage>();
        //rawImage.GetComponent<RectTransform>().sizeDelta = new Vector2(webCamTexTure.width, webCamTexTure.height);
        rawImage.texture = webCamTexture;
        
        webCamTexture.Play();



        flipWebCamToggle.onValueChanged.AddListener((value) =>
        {
            FlipWebCamToggleListener(value);
        });        
        
        if(PlayerPrefs.GetString("flip webcam") == "true") { flipWebCamToggle.isOn = true; }
        else { flipWebCamToggle.isOn = false; }

        continueButton.onClick.AddListener(OnContinueButtonClicked);
        quitButton.onClick.AddListener(OnQuitButtonClicked);
    }
    private void OnContinueButtonClicked()
    {
        SceneManagement.Instance.ChangeSceneAsync("Songlist");
    }

    private void OnQuitButtonClicked()
    {
        SceneManagement.Instance.QuitGame();
    }
    public void FlipWebCamToggleListener(bool value)
    {
        if (value) 
        { 
            PlayerPrefs.SetString("flip webcam","true");
            rawImage.rectTransform.localScale = new Vector3(-1, 1, 1);
        }
        else {
            PlayerPrefs.SetString("flip webcam", "false");
            rawImage.rectTransform.localScale = new Vector3(1, 1, 1);
        }
    }

    // Update is called once per frame
    void OnDisable()
    {
        flipWebCamToggle.onValueChanged.RemoveListener(FlipWebCamToggleListener);
        Debug.Log("Webcam has stopped in setting scene");
        webCamTexture.Stop();
    }

    void OnApplicationQuit()
    {
        Debug.Log("Webcam has stopped in setting scene");
        webCamTexture.Stop();
    }

}
