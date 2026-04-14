using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Best.SocketIO;
using Best.SocketIO.Events;
using Newtonsoft.Json;
using System.Runtime.InteropServices;

namespace SlotGame
{
    /// <summary>
    /// Handles all server communication via Socket.IO
    /// Demo mode with full debug logging for init, spin request, and results
    /// </summary>
    public class SocketIOManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] internal bool useDemoMode = true;
        [SerializeField] private string testToken = "test-token";
        [SerializeField] private string testSocketURL = "https://devrealtime.dingdinghouse.com/";
        [SerializeField] private string nameSpace = "playground";
        [SerializeField] private string gameID = "SL-NEW";

        [Header("References")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private JSFunctCalls JSManager;

        // Socket components
        private SocketManager socketManager;
        private Socket gameSocket;

        // Auth data
        private string authToken;
        private string socketURL;

        // Connection state
        internal bool isConnected;
        internal bool isInitialized;

        // Ping/Pong
        private Coroutine pingCoroutine;
        private float lastPongTime;
        private bool waitingForPong;
        private int missedPongs;
        private const int MAX_MISSED_PONGS = 5;
        private const float PING_INTERVAL = 2f;

        // Reconnection tracking
        private bool hasEverConnected = false;

        #region Initialization

        private void Start()
        {
            if (useDemoMode)
            {
                StartCoroutine(InitializeDemoMode());
            }
            else
            {
                InitializeSocketConnection();
            }
        }

        private void InitializeSocketConnection()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (JSManager != null)
            {
                JSManager.SendCustomMessage("authToken");
            }
            StartCoroutine(WaitForAuthAndConnect());
#else
            authToken = testToken;
            socketURL = testSocketURL;
            SetupSocketManager();
#endif
        }

        void ReceiveAuthToken(string jsonData)
        {
            Debug.Log($"[SocketIO] Received auth data: {jsonData}");
            var authData = JsonUtility.FromJson<AuthData>(jsonData);
            authToken = authData.token;
            socketURL = authData.socketURL;
            if (!string.IsNullOrEmpty(authData.nameSpace))
            {
                nameSpace = authData.nameSpace;
            }
        }

        #endregion

        #region Demo Mode

        private IEnumerator InitializeDemoMode()
        {
            Debug.Log("[SocketIO] 🎮 DEMO MODE - Simulating connection...");
            yield return new WaitForSeconds(0.5f);

            isConnected = true;
            isInitialized = true;

            // Generate demo init data
            var initData = GenerateDemoInitData();
            
            // LOG INIT DATA
            LogInitData(initData);
            
            gameManager.OnInitDataReceived(initData);

#if UNITY_WEBGL && !UNITY_EDITOR
            if (JSManager != null)
            {
                JSManager.SendCustomMessage("OnEnter");
            }
#endif
        }

        internal void SendDemoSpinRequest(int betIndex, bool isFreeSpin)
        {
            // LOG SPIN REQUEST
            Debug.Log($"[SocketIO] 📤 SPIN REQUEST - BetIndex: {betIndex}, IsFreeSpin: {isFreeSpin}");
            StartCoroutine(SimulateDemoSpinResult(betIndex, isFreeSpin));
        }

        private IEnumerator SimulateDemoSpinResult(int betIndex, bool isFreeSpin)
        {
            yield return new WaitForSeconds(UnityEngine.Random.Range(0.3f, 0.8f));

            var result = GenerateDemoSpinResult(betIndex, isFreeSpin);
            
            // LOG SPIN RESULT
            LogSpinResult(result);
            
            gameManager.OnSpinResultReceived(result);
        }

        private InitData GenerateDemoInitData()
        {
            var config = new GameConfig
            {
                reelCount = 5,
                rowCount = 4,
                symbolCount = 11,
                paylineCount = 40,
                availableBets = new List<double> { 0.10, 0.20, 0.50, 1.00, 2.00, 5.00, 10.00, 20.00, 50.00 },
                paylines = GenerateDemoPaylines(),
                symbols = GenerateDemoSymbols()
            };

            return new InitData
            {
                gameConfig = config,
                playerData = new PlayerData { balance = 1000.00, currentBetIndex = 0 },
                initialMatrix = GenerateRandomMatrix()
            };
        }

        private SpinResult GenerateDemoSpinResult(int betIndex, bool isFreeSpin)
        {
            var initData = gameManager.gameConfig;
            double betAmount = initData.availableBets[betIndex];
            
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

            // Deduct bet if not free spin
            if (!isFreeSpin)
                result.playerData.balance -= betAmount;

            // 30% chance of win
            if (UnityEngine.Random.value < 0.3f)
            {
                result.winAmount = betAmount * UnityEngine.Random.Range(2, 20);
                result.winLines.Add(new WinLine
                {
                    lineId = UnityEngine.Random.Range(0, 40),
                    symbolId = UnityEngine.Random.Range(2, 11),
                    positions = new List<int> { 0, 1, 2 },
                    winAmount = result.winAmount
                });
            }

            result.playerData.balance += result.winAmount;

            // 5% chance of free spins
            if (!isFreeSpin && UnityEngine.Random.value < 0.05f)
            {
                result.freeSpinData = new FreeSpinData
                {
                    isTriggered = true,
                    spinsAwarded = UnityEngine.Random.Range(5, 15),
                    remainingSpins = 0
                };
            }

            return result;
        }

        private List<List<int>> GenerateDemoPaylines()
        {
            var lines = new List<List<int>>
            {
                new List<int> { 0, 0, 0, 0, 0 },
                new List<int> { 1, 1, 1, 1, 1 },
                new List<int> { 2, 2, 2, 2, 2 },
                new List<int> { 3, 3, 3, 3, 3 }
            };

            for (int i = 0; i < 36; i++)
            {
                var line = new List<int>();
                for (int j = 0; j < 5; j++)
                    line.Add(UnityEngine.Random.Range(0, 4));
                lines.Add(line);
            }

            return lines;
        }

        private List<SymbolInfo> GenerateDemoSymbols()
        {
            return new List<SymbolInfo>
            {
                new SymbolInfo { id = 0, name = "Wild", multipliers = new List<double> { 100, 50, 25 }, isWild = true },
                new SymbolInfo { id = 1, name = "Scatter", multipliers = new List<double> { 50, 20, 10 }, isScatter = true },
                new SymbolInfo { id = 2, name = "Symbol_A", multipliers = new List<double> { 40, 15, 8 } },
                new SymbolInfo { id = 3, name = "Symbol_B", multipliers = new List<double> { 30, 12, 6 } },
                new SymbolInfo { id = 4, name = "Symbol_C", multipliers = new List<double> { 25, 10, 5 } },
                new SymbolInfo { id = 5, name = "Symbol_D", multipliers = new List<double> { 20, 8, 4 } },
                new SymbolInfo { id = 6, name = "Symbol_E", multipliers = new List<double> { 15, 6, 3 } },
                new SymbolInfo { id = 7, name = "Symbol_F", multipliers = new List<double> { 10, 5, 2 } },
                new SymbolInfo { id = 8, name = "Symbol_G", multipliers = new List<double> { 8, 4, 2 } },
                new SymbolInfo { id = 9, name = "Symbol_H", multipliers = new List<double> { 6, 3, 1 } },
                new SymbolInfo { id = 10, name = "Symbol_I", multipliers = new List<double> { 5, 2, 1 } }
            };
        }

        private List<List<int>> GenerateRandomMatrix()
        {
            var matrix = new List<List<int>>();
            for (int col = 0; col < 5; col++)
            {
                var column = new List<int>();
                for (int row = 0; row < 4; row++)
                    column.Add(UnityEngine.Random.Range(0, 11));
                matrix.Add(column);
            }
            return matrix;
        }

        #endregion

        #region Debug Logging

        private void LogInitData(InitData data)
        {
            Debug.Log("═══════════════════════════════════════════════");
            Debug.Log("🎮 INIT DATA");
            Debug.Log("═══════════════════════════════════════════════");
            Debug.Log($"💰 Balance: {data.playerData.balance:F2}");
            Debug.Log($"🎰 Bet Index: {data.playerData.currentBetIndex}");
            Debug.Log($"📊 Available Bets: {string.Join(", ", data.gameConfig.availableBets)}");
            
            Debug.Log("\n📋 Initial Matrix:");
            LogMatrix(data.initialMatrix);
            
            Debug.Log("═══════════════════════════════════════════════\n");
        }

        private void LogSpinResult(SpinResult result)
        {
            Debug.Log("═══════════════════════════════════════════════");
            Debug.Log("🎯 SPIN RESULT");
            Debug.Log("═══════════════════════════════════════════════");
            Debug.Log($"💰 Balance: {result.playerData.balance:F2}");
            Debug.Log($"🏆 Win Amount: {result.winAmount:F2}");
            Debug.Log($"📊 Win Lines: {result.winLines.Count}");
            
            if (result.freeSpinData != null && result.freeSpinData.isTriggered)
            {
                Debug.Log($"🎁 FREE SPINS TRIGGERED! Awarded: {result.freeSpinData.spinsAwarded}");
            }
            
            Debug.Log("\n📋 Result Matrix:");
            LogMatrix(result.resultMatrix);
            
            Debug.Log("═══════════════════════════════════════════════\n");
        }

        private void LogMatrix(List<List<int>> matrix)
        {
            if (matrix == null || matrix.Count != 5)
            {
                Debug.LogWarning("Invalid matrix!");
                return;
            }

            for (int row = 0; row < 4; row++)
            {
                string rowStr = "  [";
                for (int col = 0; col < 5; col++)
                {
                    rowStr += matrix[col][row].ToString("D2");
                    if (col < 4) rowStr += ", ";
                }
                rowStr += "]";
                Debug.Log(rowStr);
            }
        }

        #endregion

        #region Real Socket Connection

        private IEnumerator WaitForAuthAndConnect()
        {
            float timeout = 10f;
            float elapsed = 0f;

            while (string.IsNullOrEmpty(authToken) && elapsed < timeout)
            {
                Debug.Log("[SocketIO] Waiting for auth token...");
                elapsed += Time.deltaTime;
                yield return new WaitForSeconds(0.5f);
            }

            if (string.IsNullOrEmpty(authToken))
            {
                Debug.LogError("[SocketIO] Auth timeout!");
                yield break;
            }

            Debug.Log("[SocketIO] Auth received, setting up socket manager");
            SetupSocketManager();
        }

        private void SetupSocketManager()
        {
            var options = new SocketOptions
            {
                AutoConnect = false,
                Reconnection = false,
                Timeout = TimeSpan.FromSeconds(3),
                ConnectWith = Best.SocketIO.Transports.TransportTypes.WebSocket,
                Auth = (manager, socket) => new { token = authToken }
            };

#if UNITY_EDITOR
            socketManager = new SocketManager(new Uri(testSocketURL), options);
#else
            socketManager = new SocketManager(new Uri(socketURL), options);
#endif

            gameSocket = string.IsNullOrEmpty(nameSpace) 
                ? socketManager.Socket 
                : socketManager.GetSocket("/" + nameSpace);

            SubscribeToEvents();
            socketManager.Open();
        }

        private void SubscribeToEvents()
        {
            gameSocket.On<ConnectResponse>(SocketIOEventTypes.Connect, OnConnected);
            gameSocket.On(SocketIOEventTypes.Disconnect, OnDisconnected);
            gameSocket.On<Error>(SocketIOEventTypes.Error, OnError);
            gameSocket.On<string>("game:init", OnInitReceived);
            gameSocket.On<string>("result", OnResultReceived);
            gameSocket.On<string>("pong", OnPongReceived);
            gameSocket.On<string>("AnotherDevice", OnAnotherDevice);
        }

        #endregion

        #region Socket Events

        private void OnConnected(ConnectResponse resp)
        {
            Debug.Log("[SocketIO] ✅ Connected!");
            isConnected = true;
            hasEverConnected = true;
            waitingForPong = false;
            missedPongs = 0;
            lastPongTime = Time.time;
            
            StartPingRoutine();

#if UNITY_WEBGL && !UNITY_EDITOR
            if (JSManager != null)
            {
                JSManager.SendCustomMessage("OnEnter");
            }
#endif
        }

        private void OnDisconnected()
        {
            Debug.LogWarning("[SocketIO] ⚠️ Disconnected!");
            isConnected = false;
            StopPingRoutine();
            gameManager.OnDisconnected();
        }

        private void OnError(Error err)
        {
            Debug.LogError($"[SocketIO] Error: {err}");
#if UNITY_WEBGL && !UNITY_EDITOR
            if (JSManager != null)
            {
                JSManager.SendCustomMessage("error");
            }
#endif
        }

        private void OnInitReceived(string jsonData)
        {
            Debug.Log($"[SocketIO] Init data received");
            var initData = JsonConvert.DeserializeObject<InitData>(jsonData);
            isInitialized = true;
            
            LogInitData(initData);
            
            gameManager.OnInitDataReceived(initData);
        }

        private void OnResultReceived(string jsonData)
        {
            Debug.Log($"[SocketIO] Result received");
            var result = JsonConvert.DeserializeObject<SpinResult>(jsonData);
            
            LogSpinResult(result);
            
            gameManager.OnSpinResultReceived(result);
        }

        private void OnPongReceived(string data)
        {
            Debug.Log("[SocketIO] ✅ Pong received");
            waitingForPong = false;
            missedPongs = 0;
            lastPongTime = Time.time;
        }

        private void OnAnotherDevice(string data)
        {
            Debug.Log("[SocketIO] Another device login detected: " + data);
            gameManager.OnDisconnected();
        }

        #endregion

        #region Ping/Pong Heartbeat

        private void StartPingRoutine()
        {
            StopPingRoutine();
            pingCoroutine = StartCoroutine(PingHeartbeat());
        }

        private void StopPingRoutine()
        {
            if (pingCoroutine != null)
            {
                StopCoroutine(pingCoroutine);
                pingCoroutine = null;
            }
        }

        private IEnumerator PingHeartbeat()
        {
            while (isConnected)
            {
                if (waitingForPong)
                {
                    missedPongs++;
                    Debug.LogWarning($"[SocketIO] ⚠️ Pong missed #{missedPongs}/{MAX_MISSED_PONGS}");
                    
                    if (missedPongs >= MAX_MISSED_PONGS)
                    {
                        Debug.LogError("[SocketIO] ❌ Connection lost - too many missed pongs!");
                        OnDisconnected();
                        yield break;
                    }
                }

                waitingForPong = true;
                Debug.Log("[SocketIO] 📤 Sending ping...");
                gameSocket?.Emit("ping");
                
                yield return new WaitForSeconds(PING_INTERVAL);
            }
        }

        #endregion

        #region Send Requests

        internal void SendSpinRequest(int betIndex, bool isFreeSpin)
        {
            if (useDemoMode)
            {
                SendDemoSpinRequest(betIndex, isFreeSpin);
                return;
            }

            if (!isConnected || gameSocket == null)
            {
                Debug.LogWarning("[SocketIO] Cannot spin - not connected!");
                return;
            }

            var request = new SpinRequest
            {
                payload = new SpinPayload
                {
                    betIndex = betIndex,
                    isFreeSpin = isFreeSpin
                }
            };

            string json = JsonUtility.ToJson(request);
            Debug.Log($"[SocketIO] 📤 Sending spin request: {json}");
            gameSocket.Emit("request", json);
        }

        #endregion

        #region Cleanup

        internal void CloseSocket()
        {
            Debug.Log("[SocketIO] Closing socket...");
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

        private void OnDestroy()
        {
            CloseSocket();
        }

        #endregion
    }
}