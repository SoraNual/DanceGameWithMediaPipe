using UnityEngine;
using NativeWebSocket;
using System.Diagnostics;
using System.Threading.Tasks;
using Debug = UnityEngine.Debug;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;
using Newtonsoft.Json.Linq;

public class ClientConnectionManager : MonoBehaviour
{
    public static ClientConnectionManager Instance { get; private set; }
    private WebSocket _websocket;
    private string serverUrl = "ws://localhost:8139";
    private bool isConnected = false;
    private readonly string pythonAppName = "pyServerMediapipeLegacy_noVisibility";

    public bool IsConnected => isConnected; // Add public property to check connection state

    private void Awake()
    {
        Application.quitting += OnApplicationQuittingHandler;
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public async Task<bool> ConnectToServerWithLaunch()
    {
        Debug.Log("Starting connection process...");

        if (!IsServerRunning())
        {
            StartServerExecutable();
            await Task.Delay(3000);
        }

        bool result = await ConnectToServer();
        Debug.Log($"ConnectToServerWithLaunch returning: {result}"); // Debug log
        return result;
    }

    private bool IsServerRunning()
    {
        return Process.GetProcessesByName(pythonAppName).Length > 0;
    }

    private void StartServerExecutable()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = Application.streamingAssetsPath + $"/{pythonAppName}.exe",
            UseShellExecute = true
        });
    }

    private async Task<bool> ConnectToServer()
    {
        try
        {
            Debug.Log("ConnectToServer started");
            _websocket = new WebSocket(serverUrl);

            _websocket.OnOpen += () =>
            {
                Debug.Log("Connection open!");
                isConnected = true;
            };

            _websocket.OnError += (e) =>
            {
                Debug.Log("WebSocket Error: " + e);
                isConnected = false;
            };

            _websocket.OnClose += (e) =>
            {
                Debug.Log("Connection closed!");
                isConnected = false;
            };

            _websocket.OnMessage += (bytes) =>
            {
                var message = System.Text.Encoding.UTF8.GetString(bytes);
                try
                {
                    // Try to deserialize the message as a JObject
                    var response = JObject.Parse(message);

                    // Check if the response contains the "dtw_distance" key
                    if (response["dtw_distance"] != null)
                    {
                        double dtwDistance = response["dtw_distance"].Value<double>();
                        // Find the active PoseCapturesDTW instance and update the score
                        var gameplaySceneController = FindObjectOfType<GameplaySceneController>();
                        if (gameplaySceneController != null)
                        {
                            gameplaySceneController.UpdateScoreAndFeedbackUsingDTW(dtwDistance);
                        }
                    }
                    // Check if the response contains the "error" key
                    else if (response["error"] != null)
                    {
                        string errorMessage = response["error"].Value<string>();
                        //Debug.LogError($"Server error: {errorMessage}");
                    }
                    else if (response["correctness"] != null)
                    {
                        double correctness = response["correctness"].Value<double>();
                        Debug.Log(correctness);
                        // Find the active PoseCapturesDTW instance and update the score
                        var gameplaySceneController = FindObjectOfType<GameplaySceneController>();
                        if (gameplaySceneController != null)
                        {
                            gameplaySceneController.UpdateScoreAndFeedbackUsingCorrectness(correctness);
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Unexpected response from server: {message}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error processing message: {e.Message}");
                    Debug.Log(e.ToString());
                }
            };

            await _websocket.Connect();

            // Instead of using TaskCompletionSource, let's try a simple delay and check
            int maxAttempts = 10;
            for (int i = 0; i < maxAttempts; i++)
            {
                Debug.Log($"Checking connection status attempt {i + 1}/{maxAttempts}");
                if (isConnected)
                {
                    Debug.Log("Connection confirmed successful");
                    return true;
                }
                await Task.Delay(500);
            }

            Debug.Log("Connection check timed out");
            return false;
        }
        catch (Exception e)
        {
            Debug.LogError($"Connection error: {e}");
            return false;
        }
    }

    public async Task SendMessage(string message)
    {
        if (_websocket != null && isConnected)
        {
            await _websocket.SendText(message);
        }
        else
        {
            Debug.LogError("Cannot send message: WebSocket is not connected");
        }
    }

    private void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        if (_websocket != null)
        {
            _websocket.DispatchMessageQueue();
        }
#endif
    }
    private void OnApplicationQuittingHandler()
    {

        foreach (var process in Process.GetProcessesByName(pythonAppName))
        {
            process.Kill();
        }
    }
    private async void OnApplicationQuit()
    {
        if (_websocket != null && isConnected)
        {
            await _websocket.Close();
        }
        
    }
}