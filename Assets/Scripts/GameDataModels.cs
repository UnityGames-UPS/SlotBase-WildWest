using System;
using System.Collections.Generic;
using System.Linq;

#region Server Communication Models

[Serializable]
public class InitData
{
    public string id = "initData";
    public ServerGameData gameData;
    public ServerFeatures features;
    public ServerUIData uiData;
    public ServerPlayer player;
}

[Serializable]
public class ServerGameData
{
    public List<List<int>> lines;
    public List<double> bets;
    public int totalLines;
}

[Serializable]
public class ServerFeatures
{
    public FreeSpinFeature freeSpins;
    public BuyFeature buyFeature;
}

[Serializable]
public class FreeSpinFeature
{
    public bool enabled;
    public int initialSpins;
    public bool stickyWilds;
    public bool wildMultiplierPersist;
    public OverlayScatterFeature overlayScatter;
}

[Serializable]
public class OverlayScatterFeature
{
    public bool enabled;
    public List<int> values;
    public ExtraSpinsData extraSpins;
}

[Serializable]
public class ExtraSpinsData
{
    public int _2; // For 2 scatters
    public int _3; // For 3 scatters
    public int _4; // For 4 scatters
    public int _5; // For 5 scatters
}

[Serializable]
public class BuyFeature
{
    public bool enabled;
    public double costMultiplier;
}

[Serializable]
public class ServerUIData
{
    public PaylineData paylines;
}

[Serializable]
public class PaylineData
{
    public List<ServerSymbolInfo> symbols;
}

[Serializable]
public class ServerSymbolInfo
{
    public int id;
    public string name;
    public List<double> multiplier; // Note: "multiplier" not "multipliers"
}

[Serializable]
public class ServerPlayer
{
    public double balance;
}

// ============================================================================
// FIXED: Server Response Models - Must match actual server JSON structure
// ============================================================================

[Serializable]
public class ServerSpinResponse
{
    public string id = "ResultData";
    public bool success;
    public ServerPlayerBalance player;
    public ServerPayload payload;
    public ServerFeaturesResult features;
}

[Serializable]
public class ServerPlayerBalance
{
    public double? balance; // Nullable because server sends null
}

[Serializable]
public class ServerPayload
{
    public List<List<string>> reels;        // Server sends STRINGS not ints!
    public List<ServerWinLine> winningLines; // Server uses "winningLines"
    public double totalWin;                  // Server uses "totalWin"
    public int scatterCount;
    public bool scatterTriggered;
    public object freeSpinState; // Can be null
}

[Serializable]
public class ServerWinLine
{
    public int lineIndex;                    // Server uses "lineIndex"
    public List<List<int>> positions;        // Server format: [[row,col], [row,col]]
    public string symbolId;                  // Server sends STRING!
    public int matchCount;
    public double basePayout;
    public double payout;
    public int wildMultiplier;
    public List<WildDetail> wildDetails;
}

[Serializable]
public class WildDetail
{
    public int col;
    public int row;
    public int multiplier;
}

[Serializable]
public class ServerFeaturesResult
{
    public ServerFreeSpinResult freeSpins;
}

[Serializable]
public class ServerFreeSpinResult
{
    public bool triggered;
    public int spinsAwarded;
}

// ============================================================================
// Client-Side Spin Request
// ============================================================================

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

#region Game Configuration (Client Side Converted)

[Serializable]
public class GameConfig
{
    public int reelCount = 5;
    public int rowCount = 4;
    public int symbolCount = 13;
    public int paylineCount = 40;
    public List<List<int>> paylines;
    public List<double> availableBets;
    public List<SymbolInfo> symbols;
    
    // Wild configuration
    public int wildSymbolId = 11;
    public List<int> wildMultipliers = new List<int> { 1, 2, 3, 5 };
    
    // Scatter configuration
    public int scatterSymbolId = 12;
}

[Serializable]
public class SymbolInfo
{
    public int id;
    public string name;
    public List<double> multipliers;
    public bool isWild;
    public bool isScatter;
    public int wildMultiplier;
}

#endregion

#region Player & Game State (Client Side)

[Serializable]
public class PlayerData
{
    public double balance;
    public int currentBetIndex;
}

[Serializable]
public class SpinResult
{
    public List<List<int>> resultMatrix;  // Client uses int matrix
    public double winAmount;
    public List<WinLine> winLines;
    public PlayerData playerData;
    public FreeSpinData freeSpinData;
    public ScatterData scatterData;
}

[Serializable]
public class WinLine
{
    public int lineId;
    public int symbolId;
    public List<int> positions;  // Flat list: [0, 5, 10, 15, 20]
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
    Normal,
    Turbo,
    QuickSpin
}

#endregion

#region Helper Classes for Conversion

/// <summary>
/// Converts server data to client GameConfig
/// </summary>
public static class InitDataConverter
{
    public static GameConfig ConvertToGameConfig(InitData serverData)
    {
        var config = new GameConfig
        {
            reelCount = 5,
            rowCount = 4,
            symbolCount = serverData.uiData.paylines.symbols.Count,
            paylineCount = serverData.gameData.totalLines,
            paylines = serverData.gameData.lines,
            availableBets = serverData.gameData.bets,
            symbols = new List<SymbolInfo>()
        };
        
        foreach (var serverSymbol in serverData.uiData.paylines.symbols)
        {
            var symbolInfo = new SymbolInfo
            {
                id = serverSymbol.id,
                name = serverSymbol.name,
                multipliers = serverSymbol.multiplier ?? new List<double>(),
                isWild = serverSymbol.name.ToLower().Contains("wild"),
                isScatter = serverSymbol.name.ToLower().Contains("scatter"),
                wildMultiplier = 1
            };
            
            config.symbols.Add(symbolInfo);
            
            if (symbolInfo.isWild)
            {
                config.wildSymbolId = symbolInfo.id;
            }
            if (symbolInfo.isScatter)
            {
                config.scatterSymbolId = symbolInfo.id;
            }
        }
        
        return config;
    }
    
    public static PlayerData ConvertToPlayerData(ServerPlayer serverPlayer, int defaultBetIndex = 0)
    {
        return new PlayerData
        {
            balance = serverPlayer.balance,
            currentBetIndex = defaultBetIndex
        };
    }

    /// <summary>
    /// CRITICAL: Converts server response to client SpinResult
    /// Handles string-to-int conversion and field name mapping
    /// </summary>
    public static SpinResult ConvertServerResponseToSpinResult(ServerSpinResponse serverResponse, double currentBalance, double betAmount)
    {
        var result = new SpinResult
        {
            // Convert reels from List<List<string>> to List<List<int>>
            resultMatrix = ConvertReelsToMatrix(serverResponse.payload.reels),
            
            // Map totalWin to winAmount
            winAmount = serverResponse.payload.totalWin,
            
            // Convert winningLines to winLines
            winLines = ConvertWinningLines(serverResponse.payload.winningLines),
            
            // Update player data
            playerData = new PlayerData
            {
                balance = CalculateNewBalance(currentBalance, betAmount, serverResponse.payload.totalWin),
                currentBetIndex = 0 // Will be set by GameManager
            },
            
            // Convert free spin data
            freeSpinData = serverResponse.features?.freeSpins != null && serverResponse.features.freeSpins.triggered
                ? new FreeSpinData
                {
                    isTriggered = true,
                    spinsAwarded = serverResponse.features.freeSpins.spinsAwarded,
                    remainingSpins = 0
                }
                : null,
            
            // Convert scatter data
            scatterData = serverResponse.payload.scatterTriggered
                ? new ScatterData
                {
                    isTriggered = true,
                    scatterCount = serverResponse.payload.scatterCount,
                    winAmount = 0 // Calculate if needed
                }
                : null
        };
        
        return result;
    }

    /// <summary>
    /// Converts server reels (strings) to client matrix (ints)
    /// Server: [["4","8","3","7"], ...] → Client: [[4,8,3,7], ...]
    /// </summary>
    private static List<List<int>> ConvertReelsToMatrix(List<List<string>> reels)
    {
        var matrix = new List<List<int>>();
        
        foreach (var reel in reels)
        {
            var column = new List<int>();
            foreach (var symbolStr in reel)
            {
                if (int.TryParse(symbolStr, out int symbolId))
                {
                    column.Add(symbolId);
                }
                else
                {
                    UnityEngine.Debug.LogError($"Failed to parse symbol: {symbolStr}");
                    column.Add(0); // Default to 0
                }
            }
            matrix.Add(column);
        }
        
        return matrix;
    }

    /// <summary>
    /// Converts server winningLines to client winLines
    /// Server: positions as [[row,col], [row,col]]
    /// Client: positions as flat list [index0, index1, ...]
    /// </summary>
    private static List<WinLine> ConvertWinningLines(List<ServerWinLine> serverWinLines)
    {
        var winLines = new List<WinLine>();
        
        if (serverWinLines == null) return winLines;
        
        foreach (var serverLine in serverWinLines)
        {
            // Parse symbolId from string to int
            if (!int.TryParse(serverLine.symbolId, out int symbolId))
            {
                UnityEngine.Debug.LogError($"Failed to parse symbolId: {serverLine.symbolId}");
                continue;
            }
            
            // Convert positions from [[row,col]] to flat indices
            var flatPositions = new List<int>();
            foreach (var pos in serverLine.positions)
            {
                if (pos.Count >= 2)
                {
                    int row = pos[0];
                    int col = pos[1];
                    int flatIndex = col * 4 + row; // col * rowCount + row
                    flatPositions.Add(flatIndex);
                }
            }
            
            winLines.Add(new WinLine
            {
                lineId = serverLine.lineIndex,
                symbolId = symbolId,
                positions = flatPositions,
                winAmount = serverLine.payout
            });
        }
        
        return winLines;
    }

    /// <summary>
    /// Calculate new balance: current - bet + win
    /// </summary>
    private static double CalculateNewBalance(double currentBalance, double betAmount, double winAmount)
    {
        return currentBalance - betAmount + winAmount;
    }
}

#endregion