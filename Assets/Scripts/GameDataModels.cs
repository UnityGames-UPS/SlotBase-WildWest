using System;
using System.Collections.Generic;

 #region Server Communication Models

    [Serializable]
    public class InitData
    {
        public string id = "initData";
        public GameConfig gameConfig;
        public PlayerData playerData;
        public List<List<int>> initialMatrix;
    }

    [Serializable]
    public class SpinResult
    {
        public string id = "ResultData";
        public List<List<int>> resultMatrix;
        public double winAmount;
        public List<WinLine> winLines;
        public PlayerData playerData;
        public FreeSpinData freeSpinData;
        public ScatterData scatterData;
    }

    [Serializable]
    public class SpinRequest
    {
        public string type = "SPIN";
        public SpinPayload payload;
    }

    [Serializable]
    public class SpinPayload
    {
        public int betIndex;
        public bool isFreeSpin;
    }

    #endregion

    #region Game Configuration

    [Serializable]
    public class GameConfig
    {
        public int reelCount = 5;
        public int rowCount = 4;
        public int symbolCount = 11;
        public int paylineCount = 40;
        public List<List<int>> paylines;
        public List<double> availableBets;
        public List<SymbolInfo> symbols;
    }

    [Serializable]
    public class SymbolInfo
    {
        public int id;
        public string name;
        public List<double> multipliers;
        public bool isWild;
        public bool isScatter;
    }

    #endregion

    #region Player & Game State

    [Serializable]
    public class PlayerData
    {
        public double balance;
        public int currentBetIndex;
    }

    [Serializable]
    public class WinLine
    {
        public int lineId;
        public int symbolId;
        public List<int> positions;
        public double winAmount;
    }

    [Serializable]
    public class FreeSpinData
    {
        public bool isTriggered;
        public int spinsAwarded;
        public int remainingSpins;
    }

    [Serializable]
    public class ScatterData
    {
        public bool isTriggered;
        public int scatterCount;
        public double winAmount;
    }

    #endregion

    #region Platform Communication

    [Serializable]
    public class AuthData
    {
        public string token;
        public string socketURL;
        public string nameSpace;
    }

    #endregion

    #region Enums

    public enum GameState
    {
        Initializing,
        Idle,
        Spinning,
        Stopping,
        ShowingWin,
        FreeSpinMode
    }

    public enum SpinSpeed
    {
        Normal,     // 3-4 seconds
        Turbo,      // 2 seconds
        QuickSpin   // Instant after 1 cycle
    }

    #endregion

