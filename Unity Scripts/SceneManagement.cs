using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class SceneManagement : MonoBehaviour
{
    public static SceneManagement Instance { get; private set; }

    public Slider _loadingBar;
    public CanvasGroup _loadingCanvasGroup;
    public TextMeshProUGUI _loadingText;
    [SerializeField] private float _fadeDuration = 0.3f;
    // Start is called before the first frame update
    void Awake()
    {
        // Singleton pattern implementation
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Ensure we start with invisible loading screen
        _loadingCanvasGroup.alpha = 0f;
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void QuitGame()
    {
        Debug.Log("EXIT");
        Application.Quit();
    }

    public void ResetScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        Debug.Log("reset scene");
    }

    public void ChangeSceneAsync(string sceneName)
    {
        _loadingBar.value = 0f;
        Debug.Log($"Changing scene to {sceneName}");
        //_loadingCanvas.SetActive(true);
        StartCoroutine(LoadLevelWithTransitions(sceneName));
        //_loadingCanvas.SetActive(false);
    }
    private IEnumerator LoadLevelWithTransitions(string sceneName)
    {
        // Fade in loading screen
        yield return StartCoroutine(FadeLoadingScreen(1f));

        // Do the loading
        yield return StartCoroutine(ArtificialLoadLevelAsync(sceneName));

        // Fade out loading screen
        yield return StartCoroutine(FadeLoadingScreen(0f));
    }
    private IEnumerator FadeLoadingScreen(float targetValue)
    {
        _loadingCanvasGroup.blocksRaycasts = targetValue > 0; // Block interactions during fade in

        float startValue = _loadingCanvasGroup.alpha;
        float elapsedTime = 0;
        

        while (elapsedTime < _fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float currentAlpha = Mathf.Lerp(startValue, targetValue, elapsedTime / _fadeDuration);
            _loadingCanvasGroup.alpha = currentAlpha;
            yield return null;
        }

        _loadingCanvasGroup.alpha = targetValue; // Ensure we reach exact target
    }
    IEnumerator ArtificialLoadLevelAsync(string sceneName)
    {
        AsyncOperation loadLevel = SceneManager.LoadSceneAsync(sceneName);
        loadLevel.allowSceneActivation = false; // Prevent immediate scene switch

        
        float artificialProgress = 0f;

        // First phase: Artificial loading progress up to 70%
        while (artificialProgress < 0.9f)
        {
            artificialProgress += Random.Range(0.01f, 0.1f); // Random increment for more natural feel
            _loadingBar.value = artificialProgress;
            Debug.Log($"Artificial Loading progress: {artificialProgress * 100:F1}%");
            yield return new WaitForSeconds(0.05f);
        }

        // Final phase: Smooth fill to 100%
        while (artificialProgress < 1f)
        {
            artificialProgress = Mathf.MoveTowards(artificialProgress, 1f, 0.05f);
            _loadingBar.value = artificialProgress;
            Debug.Log($"Final Loading progress: {artificialProgress * 100:F1}%");
            yield return null;

            // Once we reach 100%, allow the scene to activate
            if (artificialProgress >= 0.99f)
            {
                loadLevel.allowSceneActivation = true;
            }
        }

        // Wait for the scene to actually finish loading
        while (!loadLevel.isDone)
        {
            yield return null;
        }

    }

    IEnumerator ActuallyLoadLevelAsync(string sceneName)
    {
        AsyncOperation loadLevel = SceneManager.LoadSceneAsync(sceneName);
        loadLevel.allowSceneActivation = true;
        while (!loadLevel.isDone)
        {
            Debug.Log(loadLevel.progress);
            yield return new WaitForSeconds(1);
        }
    }
}