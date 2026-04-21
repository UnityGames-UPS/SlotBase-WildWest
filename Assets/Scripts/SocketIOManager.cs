using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Best.SocketIO;
using Best.SocketIO.Events;
using Newtonsoft.Json;

public class SocketIOManager : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] internal bool useDemoMode = false;
    [SerializeField] private string testToken = "test-token";
    [SerializeField] private string testSocketURL = "https://devrealtime.dingdinghouse.com/";
    [SerializeField] private string nameSpace = "playground";
    [SerializeField] private string gameID = "SL-WW";

    [Header("Scatter Configuration")]
    [SerializeField] private int scatterSymbolId = 12;
    [SerializeField] private int scattersRequiredForFreeSpin = 3;

    [Header("References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private UIManager uiManager;
    [SerializeField] internal JSFunctCalls JSManager;
    [SerializeField] private GameObject RaycastBlocker;

    private SocketManager socketManager;
    private Socket gameSocket;

    private string authToken;
    private string socketURL;

    internal bool isConnected;
    internal bool isInitialized;

    private Coroutine pingCoroutine;
    private float lastPongTime;
    private bool waitingForPong;
    private int missedPongs;
    private const int MAX_MISSED_PONGS = 3;
    private const float PING_INTERVAL = 2f;
    private const float PONG_TIMEOUT = 5f;

    #region Initialization

    private void Awake()
    {
        isInitialized = false;
        isConnected = false;
    }

    private void Start()
    {
        if (useDemoMode)
        {
            StartCoroutine(InitializeDemoMode());
        }
        else
        {
            RequestAuthToken();
        }
    }

    private void RequestAuthToken()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (JSManager != null)
        {
            JSManager.SendCustomMessage("authToken");
        }
#else
        authToken = testToken;
        socketURL = testSocketURL;
        InitializeSocket();
#endif
    }

    void ReceiveAuthToken(string jsonData)
    {
        Debug.Log($"[SocketIO] Auth received");

        try
        {
            var authData = JsonUtility.FromJson<AuthTokenData>(jsonData);
            authToken = authData.cookie;
            socketURL = authData.socketURL;

            if (!string.IsNullOrEmpty(authData.nameSpace))
            {
                nameSpace = authData.nameSpace;
            }

            InitializeSocket();
        }
        catch (Exception e)
        {
            Debug.LogError($"[SocketIO] Auth parse failed: {e.Message}");
        }
    }

    private void InitializeSocket()
    {
        if (RaycastBlocker) RaycastBlocker.SetActive(true);

        SocketOptions options = new SocketOptions
        {
            AutoConnect = false,
            Reconnection = false,
            Timeout = TimeSpan.FromSeconds(3),
            ConnectWith = Best.SocketIO.Transports.TransportTypes.WebSocket
        };

        options.Auth = (SocketManager manager, Socket socket) => new { token = authToken };

#if UNITY_EDITOR
        socketManager = new SocketManager(new Uri(testSocketURL), options);
#else
        socketManager = new SocketManager(new Uri(socketURL), options);
#endif

        gameSocket = string.IsNullOrEmpty(nameSpace)
            ? socketManager.Socket
            : socketManager.GetSocket("/" + nameSpace);

        gameSocket.On<ConnectResponse>(SocketIOEventTypes.Connect, OnSocketConnected);
        gameSocket.On(SocketIOEventTypes.Disconnect, OnSocketDisconnected);
        gameSocket.On<Error>(SocketIOEventTypes.Error, OnSocketError);

        gameSocket.On<string>("game:init", OnInitReceived);
        gameSocket.On<string>("result", OnResultReceived);
        gameSocket.On<string>("pong", OnPongReceived);
        gameSocket.On<string>("AnotherDevice", OnAnotherDevice);

        socketManager.Open();
    }

    #endregion

    #region Socket Events

    private void OnSocketConnected(ConnectResponse resp)
    {
        Debug.Log("[SocketIO] Connected");

        isConnected = true;
        waitingForPong = false;
        missedPongs = 0;
        lastPongTime = Time.time;

        if (uiManager != null)
        {
            uiManager.CheckAndClosePopups();
        }

        StartPingRoutine();
    }

    private void OnSocketDisconnected()
    {
        Debug.Log("[SocketIO] Disconnected");

        isConnected = false;
        StopPingRoutine();

        if (uiManager != null)
        {
            uiManager.DisconnectionPopup();
        }

        if (gameManager != null)
        {
            gameManager.OnDisconnected();
        }
    }

    private void OnSocketError(Error err)
    {
        Debug.LogError($"[SocketIO] Error: {err.message}");

#if UNITY_WEBGL && !UNITY_EDITOR
        if (JSManager != null)
        {
            JSManager.SendCustomMessage("error");
        }
#endif
    }

    private void OnInitReceived(string jsonData)
    {
        Debug.Log($"[SocketIO] Init received: {jsonData}");

        try
        {
            var initData = JsonConvert.DeserializeObject<InitData>(jsonData);

            var gameConfig = InitDataConverter.ConvertToGameConfig(initData);
            var playerData = InitDataConverter.ConvertToPlayerData(initData.player);
            var initialMatrix = GenerateRandomMatrix();

            isInitialized = true;

            gameManager.OnInitDataReceived(gameConfig, playerData, initialMatrix);

            if (RaycastBlocker) RaycastBlocker.SetActive(false);

#if UNITY_WEBGL && !UNITY_EDITOR
            if (JSManager != null)
            {
                JSManager.SendCustomMessage("OnEnter");
            }
#endif
        }
        catch (Exception e)
        {
            Debug.LogError($"[SocketIO] Init parse failed: {e.Message}");
        }
    }

    private void OnResultReceived(string jsonData)
    {
        Debug.Log($"[SocketIO] Result received: {jsonData}");

        try
        {
            var serverResponse = JsonConvert.DeserializeObject<ServerSpinResponse>(jsonData);

            if (!serverResponse.success)
            {
                Debug.LogError("[SocketIO] Spin failed");
                return;
            }

            double currentBalance = gameManager.playerData.balance;
            double betAmount = gameManager.currentBetAmount;
            GameConfig gameConfig = gameManager.gameConfig;

            SpinResult result = InitDataConverter.ConvertServerResponseToSpinResult(
                serverResponse,
                currentBalance,
                betAmount,
                gameConfig
            );

            result.playerData.currentBetIndex = gameManager.currentBetIndex;

            gameManager.OnSpinResultReceived(result);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SocketIO] Result parse failed: {e.Message}");
        }
    }

    private void OnAnotherDevice(string data)
    {
        Debug.Log("[SocketIO] Another device login");

        if (uiManager != null)
        {
            uiManager.AnotherDevicePopup();
        }
    }

    #endregion

    #region Ping/Pong Health Check

    private void StartPingRoutine()
    {
        if (pingCoroutine != null)
            StopCoroutine(pingCoroutine);

        pingCoroutine = StartCoroutine(PingRoutine());
    }

    private void StopPingRoutine()
    {
        if (pingCoroutine != null)
        {
            StopCoroutine(pingCoroutine);
            pingCoroutine = null;
        }
    }

    private IEnumerator PingRoutine()
    {
        while (isConnected)
        {
            yield return new WaitForSeconds(PING_INTERVAL);

            if (waitingForPong)
            {
                float timeSinceLastPong = Time.time - lastPongTime;

                if (timeSinceLastPong > PONG_TIMEOUT)
                {
                    missedPongs++;

                    if (missedPongs >= MAX_MISSED_PONGS)
                    {
                        Debug.LogWarning("[SocketIO] Max pongs missed - disconnecting");
                        OnSocketDisconnected();
                        yield break;
                    }

                    if (missedPongs == 2 && uiManager != null)
                    {
                        uiManager.ReconnectionPopup();
                    }
                }
            }

            SendPing();
            waitingForPong = true;
        }
    }

    private void SendPing()
    {
        if (gameSocket != null && isConnected)
        {
            gameSocket.Emit("ping");
        }
    }

    private void OnPongReceived(string data)
    {
        waitingForPong = false;
        lastPongTime = Time.time;

        if (missedPongs > 0)
        {
            missedPongs = 0;

            if (uiManager != null)
            {
                uiManager.CheckAndClosePopups();
            }
        }
    }

    #endregion

    #region Spin Request

    internal void SendSpinRequest(int betIndex, bool isFreeSpin)
    {
        if (useDemoMode)
        {
            SendDemoSpinRequest(betIndex, isFreeSpin);
            return;
        }

        Debug.Log($"[SocketIO] Spin request: betIndex={betIndex}, isFreeSpin={isFreeSpin}");

        var request = new SpinRequest
        {
            type = "SPIN",
            payload = new SpinPayload
            {
                betIndex = betIndex,
                isFreeSpin = isFreeSpin
            }
        };

        string json = JsonUtility.ToJson(request);
        gameSocket.Emit("request", json);
    }

    #endregion

    #region Demo Mode

    private IEnumerator InitializeDemoMode()
    {
        if (RaycastBlocker) RaycastBlocker.SetActive(true);

        yield return new WaitForSeconds(0.5f);

        isConnected = true;
        isInitialized = true;

        var demoConfig = GenerateDemoGameConfig();
        var demoPlayer = new PlayerData { balance = 100.00, currentBetIndex = 0 };

        gameManager.OnInitDataReceived(demoConfig, demoPlayer, GenerateRandomMatrix());

        if (RaycastBlocker) RaycastBlocker.SetActive(false);

#if UNITY_WEBGL && !UNITY_EDITOR
        if (JSManager != null)
        {
            JSManager.SendCustomMessage("OnEnter");
        }
#endif
    }

    internal void SendDemoSpinRequest(int betIndex, bool isFreeSpin)
    {
        StartCoroutine(SimulateDemoSpinResult(betIndex, isFreeSpin));
    }

    private IEnumerator SimulateDemoSpinResult(int betIndex, bool isFreeSpin)
    {
        yield return new WaitForSeconds(UnityEngine.Random.Range(0.3f, 0.8f));

        var result = GenerateDemoSpinResult(betIndex, isFreeSpin);
        gameManager.OnSpinResultReceived(result);
    }

    private GameConfig GenerateDemoGameConfig()
    {
        var config = new GameConfig
        {
            reelCount = 5,
            rowCount = 4,
            symbolCount = 13,
            paylineCount = 40,
            availableBets = new List<double> { 0.01, 0.02, 0.05, 0.10, 0.20, 0.50, 1.00, 2.00, 5.00, 10.00 },
            paylines = GenerateDemoPaylines(),
            symbols = GenerateDemoSymbols(),
            wildSymbolId = 11,
            wild2xSymbolId = 13,
            wild3xSymbolId = 14,
            wild5xSymbolId = 15,
            wildMultipliers = new List<int> { 1, 2, 3, 5 },
            scatterSymbolId = 12
        };

        return config;
    }

    private SpinResult GenerateDemoSpinResult(int betIndex, bool isFreeSpin)
    {
        var gameConfig = gameManager.gameConfig;
        double betAmount = gameConfig.availableBets[betIndex];

        var result = new SpinResult
        {
            resultMatrix = GenerateRandomMatrix(),
            winAmount = 0,
            winLines = new List<WinLine>(),
            playerData = new PlayerData
            {
                balance = gameManager.playerData.balance,
                currentBetIndex = betIndex
            }
        };

        if (!isFreeSpin)
            result.playerData.balance -= betAmount;

        int scatterCount = CountScattersInMatrix(result.resultMatrix);

        if (UnityEngine.Random.value < 0.35f && scatterCount < scattersRequiredForFreeSpin)
        {
            result.winAmount = betAmount * UnityEngine.Random.Range(2, 25);

            int numWinLines = UnityEngine.Random.Range(1, 4);
            for (int i = 0; i < numWinLines; i++)
            {
                int lineLength = UnityEngine.Random.Range(3, 6);
                List<int> positions = new List<int>();
                for (int j = 0; j < lineLength; j++)
                {
                    positions.Add(j * 4 + UnityEngine.Random.Range(0, 4));
                }

                result.winLines.Add(new WinLine
                {
                    lineId = UnityEngine.Random.Range(0, 40),
                    symbolId = UnityEngine.Random.Range(0, 11),
                    positions = positions,
                    winAmount = result.winAmount / numWinLines
                });
            }
        }

        result.playerData.balance += result.winAmount;

        if (!isFreeSpin && scatterCount >= scattersRequiredForFreeSpin)
        {
            if (scatterCount > scattersRequiredForFreeSpin)
            {
                result.resultMatrix = GenerateMatrixWithExactScatters(scattersRequiredForFreeSpin);
            }

            result.freeSpinData = new FreeSpinData
            {
                isTriggered = true,
                spinsAwarded = UnityEngine.Random.Range(8, 16),
                remainingSpins = 0
            };

            result.scatterData = new ScatterData
            {
                isTriggered = true,
                scatterCount = scattersRequiredForFreeSpin,
                winAmount = betAmount * 5.0
            };

            result.winAmount += result.scatterData.winAmount;
            result.playerData.balance += result.scatterData.winAmount;
        }

        return result;
    }

    private List<List<int>> GenerateRandomMatrix()
    {
        var matrix = new List<List<int>>();
        for (int col = 0; col < 5; col++)
        {
            var column = new List<int>();
            for (int row = 0; row < 4; row++)
            {
                column.Add(UnityEngine.Random.Range(0, 11));
            }
            matrix.Add(column);
        }
        return matrix;
    }

    private List<List<int>> GenerateMatrixWithExactScatters(int scatterCount)
    {
        var matrix = GenerateRandomMatrix();

        for (int col = 0; col < 5; col++)
        {
            for (int row = 0; row < 4; row++)
            {
                if (matrix[col][row] == scatterSymbolId)
                {
                    matrix[col][row] = UnityEngine.Random.Range(0, 11);
                }
            }
        }

        for (int i = 0; i < scatterCount; i++)
        {
            int col = UnityEngine.Random.Range(0, 5);
            int row = UnityEngine.Random.Range(0, 4);
            matrix[col][row] = scatterSymbolId;
        }

        return matrix;
    }

    private int CountScattersInMatrix(List<List<int>> matrix)
    {
        int count = 0;
        foreach (var column in matrix)
        {
            foreach (var symbolId in column)
            {
                if (symbolId == scatterSymbolId)
                {
                    count++;
                }
            }
        }
        return count;
    }

    private List<List<int>> GenerateDemoPaylines()
    {
        var paylines = new List<List<int>>();
        for (int i = 0; i < 40; i++)
        {
            paylines.Add(new List<int> { 0, 1, 2, 3, 4 });
        }
        return paylines;
    }

    private List<SymbolInfo> GenerateDemoSymbols()
    {
        var symbols = new List<SymbolInfo>();
        for (int i = 0; i < 13; i++)
        {
            symbols.Add(new SymbolInfo
            {
                id = i,
                name = $"Symbol_{i}",
                multipliers = new List<double> { 1, 2, 5, 10, 20 },
                isWild = i == 11,
                isScatter = i == 12,
                wildMultiplier = 1
            });
        }
        return symbols;
    }

    #endregion

    #region Cleanup

    internal void CloseSocket()
    {
        if (RaycastBlocker) RaycastBlocker.SetActive(true);

        StopPingRoutine();

        if (socketManager != null)
        {
            socketManager.Close();
            socketManager = null;
        }

        isConnected = false;

#if UNITY_WEBGL && !UNITY_EDITOR
        if (JSManager != null)
        {
            JSManager.SendCustomMessage("OnExit");
        }
#endif
    }

    private void OnDisable()
    {
        StopPingRoutine();
    }

    private void OnDestroy()
    {
        CloseSocket();
    }

    #endregion
}

[Serializable]
public class AuthTokenData
{
    public string cookie;
    public string socketURL;
    public string nameSpace;
}