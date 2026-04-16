using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Best.SocketIO;
using Best.SocketIO.Events;
using Newtonsoft.Json;
using System.Runtime.InteropServices;


    public class SocketIOManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] internal bool useDemoMode = true;
        [SerializeField] private string testToken = "test-token";
        [SerializeField] private string testSocketURL = "https://devrealtime.dingdinghouse.com/";
        [SerializeField] private string nameSpace = "playground";
        [SerializeField] private string gameID = "SL-NEW";

        [Header("Scatter Configuration")]
        [SerializeField] private int scatterSymbolId = 4; // Scatter symbol ID
        [SerializeField] private int scattersRequiredForFreeSpin = 3; // Must have exactly 3 scatters

        [Header("References")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private JSFunctCalls JSManager;

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
        private const int MAX_MISSED_PONGS = 5;
        private const float PING_INTERVAL = 2f;

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
            Debug.Log($"[SocketIO] Auth data received");
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
            Debug.Log("[SocketIO] DEMO MODE - Simulating connection");
            yield return new WaitForSeconds(0.5f);

            isConnected = true;
            isInitialized = true;

            var initData = GenerateDemoInitData();
            Debug.Log("[SocketIO] Init data generated");
            
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
            Debug.Log($"[SocketIO] SPIN REQUEST - BetIndex: {betIndex}, IsFreeSpin: {isFreeSpin}");
            StartCoroutine(SimulateDemoSpinResult(betIndex, isFreeSpin));
        }

        private IEnumerator SimulateDemoSpinResult(int betIndex, bool isFreeSpin)
        {
            yield return new WaitForSeconds(UnityEngine.Random.Range(0.3f, 0.8f));

            var result = GenerateDemoSpinResult(betIndex, isFreeSpin);
            Debug.Log($"[SocketIO] SPIN RESULT - Win: {result.winAmount:F2}");
            
            gameManager.OnSpinResultReceived(result);
        }

        private InitData GenerateDemoInitData()
        {
            var config = new GameConfig
            {
                reelCount = 5,
                rowCount = 4,
                symbolCount = 16,
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

            if (!isFreeSpin)
                result.playerData.balance -= betAmount;

            // Count scatters in result matrix
            int scatterCount = CountScattersInMatrix(result.resultMatrix);

            // Generate win if random chance (but not if we have scatters for free spin)
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
                        symbolId = UnityEngine.Random.Range(5, 16),
                        positions = positions,
                        winAmount = result.winAmount / numWinLines
                    });
                }
            }

            result.playerData.balance += result.winAmount;

            // Free spins ONLY trigger with exactly 3 scatters
            if (!isFreeSpin && scatterCount >= scattersRequiredForFreeSpin)
            {
                // Force exactly 3 scatters if more were randomly generated
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

                // Add scatter win
                result.scatterData = new ScatterData
                {
                    isTriggered = true,
                    scatterCount = scattersRequiredForFreeSpin,
                    winAmount = betAmount * 5.0 // Example scatter win multiplier
                };

                result.winAmount += result.scatterData.winAmount;
                result.playerData.balance += result.scatterData.winAmount;

                Debug.Log($"[SocketIO] 🎰 FREE SPINS TRIGGERED! {scattersRequiredForFreeSpin} Scatters - Awarded {result.freeSpinData.spinsAwarded} free spins");
            }

            return result;
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

        private List<List<int>> GenerateMatrixWithExactScatters(int scatterCount)
        {
            var matrix = GenerateRandomMatrix();
            
            // Remove all scatters first
            for (int col = 0; col < matrix.Count; col++)
            {
                for (int row = 0; row < matrix[col].Count; row++)
                {
                    if (matrix[col][row] == scatterSymbolId)
                    {
                        matrix[col][row] = UnityEngine.Random.Range(5, 16); // Replace with regular symbol
                    }
                }
            }

            // Add exactly the required number of scatters
            List<Vector2Int> availablePositions = new List<Vector2Int>();
            for (int col = 0; col < 5; col++)
            {
                for (int row = 0; row < 4; row++)
                {
                    availablePositions.Add(new Vector2Int(col, row));
                }
            }

            // Shuffle positions
            for (int i = availablePositions.Count - 1; i > 0; i--)
            {
                int randomIndex = UnityEngine.Random.Range(0, i + 1);
                var temp = availablePositions[i];
                availablePositions[i] = availablePositions[randomIndex];
                availablePositions[randomIndex] = temp;
            }

            // Place scatters
            for (int i = 0; i < scatterCount && i < availablePositions.Count; i++)
            {
                var pos = availablePositions[i];
                matrix[pos.x][pos.y] = scatterSymbolId;
            }

            return matrix;
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
                new SymbolInfo { id = 1, name = "Wild_2x", multipliers = new List<double> { 200, 100, 50 }, isWild = true },
                new SymbolInfo { id = 2, name = "Wild_3x", multipliers = new List<double> { 300, 150, 75 }, isWild = true },
                new SymbolInfo { id = 3, name = "Wild_5x", multipliers = new List<double> { 500, 250, 125 }, isWild = true },
                new SymbolInfo { id = 4, name = "Scatter", multipliers = new List<double> { 50, 20, 10 }, isScatter = true },
                new SymbolInfo { id = 5, name = "Character_1", multipliers = new List<double> { 40, 15, 8 } },
                new SymbolInfo { id = 6, name = "Character_2", multipliers = new List<double> { 35, 14, 7 } },
                new SymbolInfo { id = 7, name = "Character_3", multipliers = new List<double> { 30, 12, 6 } },
                new SymbolInfo { id = 8, name = "Character_4", multipliers = new List<double> { 25, 10, 5 } },
                new SymbolInfo { id = 9, name = "Character_5", multipliers = new List<double> { 20, 8, 4 } },
                new SymbolInfo { id = 10, name = "Character_6", multipliers = new List<double> { 15, 6, 3 } },
                new SymbolInfo { id = 11, name = "Ace", multipliers = new List<double> { 10, 5, 2 } },
                new SymbolInfo { id = 12, name = "King", multipliers = new List<double> { 8, 4, 2 } },
                new SymbolInfo { id = 13, name = "Queen", multipliers = new List<double> { 6, 3, 1 } },
                new SymbolInfo { id = 14, name = "Jack", multipliers = new List<double> { 5, 2, 1 } },
                new SymbolInfo { id = 15, name = "Ten", multipliers = new List<double> { 4, 2, 1 } }
            };
        }

        private List<List<int>> GenerateRandomMatrix()
        {
            var matrix = new List<List<int>>();
            for (int col = 0; col < 5; col++)
            {
                var column = new List<int>();
                for (int row = 0; row < 4; row++)
                {
                    // Generate mostly regular symbols (5-15), occasional scatter (4)
                    int symbolId;
                    if (UnityEngine.Random.value < 0.05f) // 5% chance for scatter
                    {
                        symbolId = scatterSymbolId;
                    }
                    else
                    {
                        symbolId = UnityEngine.Random.Range(5, 16);
                    }
                    column.Add(symbolId);
                }
                matrix.Add(column);
            }
            return matrix;
        }

        #endregion

        #region Real Socket Connection

        private IEnumerator WaitForAuthAndConnect()
        {
            float timeout = 10f;
            float elapsed = 0f;

            while (string.IsNullOrEmpty(authToken) && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return new WaitForSeconds(0.5f);
            }

            if (string.IsNullOrEmpty(authToken))
            {
                Debug.LogError("[SocketIO] Auth timeout");
                yield break;
            }

            Debug.Log("[SocketIO] Auth received, setting up socket");
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
            Debug.Log("[SocketIO] Connected successfully");
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
            Debug.LogWarning("[SocketIO] Disconnected");
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
            Debug.Log("[SocketIO] Init data received");
            var initData = JsonConvert.DeserializeObject<InitData>(jsonData);
            isInitialized = true;
            gameManager.OnInitDataReceived(initData);
        }

        private void OnResultReceived(string jsonData)
        {
            var result = JsonConvert.DeserializeObject<SpinResult>(jsonData);
            Debug.Log($"[SocketIO] Result received - Win: {result.winAmount:F2}");
            gameManager.OnSpinResultReceived(result);
        }

        private void OnPongReceived(string data)
        {
            waitingForPong = false;
            missedPongs = 0;
            lastPongTime = Time.time;
        }

        private void OnAnotherDevice(string data)
        {
            Debug.Log("[SocketIO] Another device login detected");
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
                    
                    if (missedPongs >= MAX_MISSED_PONGS)
                    {
                        Debug.LogError("[SocketIO] Connection lost - too many missed pongs");
                        OnDisconnected();
                        yield break;
                    }
                }

                waitingForPong = true;
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
                Debug.LogWarning("[SocketIO] Cannot spin - not connected");
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
            Debug.Log($"[SocketIO] Sending spin request - BetIndex: {betIndex}");
            gameSocket.Emit("request", json);
        }

        #endregion

        #region Cleanup

        internal void CloseSocket()
        {
            Debug.Log("[SocketIO] Closing socket");
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