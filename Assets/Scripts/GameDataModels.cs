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
    public int wildSymbolId = 11;      // Base wild (1x)
    public int wild2xSymbolId = 13;     // Wild 2x multiplier
    public int wild3xSymbolId = 14;     // Wild 3x multiplier
    public int wild5xSymbolId = 15;     // Wild 5x multiplier
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
    /// Handles string-to-int conversion, matrix transposition, and wild multiplier mapping
    /// Server sends [row][col] (4 rows x 5 cols), Client needs [col][row] (5 cols x 4 rows)
    /// </summary>
    public static SpinResult ConvertServerResponseToSpinResult(ServerSpinResponse serverResponse, double currentBalance, double betAmount, GameConfig gameConfig)
    {
        var result = new SpinResult
        {
            // Convert and transpose reels from server format to client format
            resultMatrix = ConvertReelsToMatrix(serverResponse.payload.reels, serverResponse.payload.winningLines, gameConfig),

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
    /// Converts server reels to client matrix with wild multiplier handling
    /// Server format: [row][col] (4 rows x 5 cols) - reels[0] = ["3","8","4","8"] is row 0 across all 5 columns
    /// Client format: [col][row] (5 cols x 4 rows) - matrix[0] = [3,8,4,8] is column 0 with 4 rows
    /// 
    /// Wild handling: Wild ID=11, but client needs:
    /// - Wild with 1x multiplier → symbolId 11 (Wild)
    /// - Wild with 2x multiplier → symbolId 13 (Wild2x) 
    /// - Wild with 3x multiplier → symbolId 14 (Wild3x)
    /// - Wild with 5x multiplier → symbolId 15 (Wild5x)
    /// </summary>
    private static List<List<int>> ConvertReelsToMatrix(List<List<string>> serverReels, List<ServerWinLine> winningLines, GameConfig gameConfig)
    {
        // Server sends 4 rows x 5 columns: reels[row][col]
        // Client needs 5 columns x 4 rows: matrix[col][row]

        if (serverReels == null || serverReels.Count != 4)
        {
            UnityEngine.Debug.LogError($"Invalid server reels: expected 4 rows, got {serverReels?.Count}");
            return GenerateDefaultMatrix();
        }

        // Build wild multiplier lookup: [col][row] -> multiplier
        var wildMultipliers = new Dictionary<string, int>();
        if (winningLines != null)
        {
            foreach (var line in winningLines)
            {
                if (line.wildDetails != null)
                {
                    foreach (var wild in line.wildDetails)
                    {
                        string key = $"{wild.col}_{wild.row}";
                        wildMultipliers[key] = wild.multiplier;
                    }
                }
            }
        }

        var matrix = new List<List<int>>();

        // Transpose: iterate by columns
        for (int col = 0; col < 5; col++)
        {
            var column = new List<int>();

            // Each column has 4 rows
            for (int row = 0; row < 4; row++)
            {
                if (col >= serverReels[row].Count)
                {
                    UnityEngine.Debug.LogError($"Invalid server data at row {row}, col {col}");
                    column.Add(0);
                    continue;
                }

                string symbolStr = serverReels[row][col];

                if (!int.TryParse(symbolStr, out int symbolId))
                {
                    UnityEngine.Debug.LogError($"Failed to parse symbol: {symbolStr}");
                    column.Add(0);
                    continue;
                }

                // Check if this is a wild with multiplier
                if (symbolId == gameConfig.wildSymbolId)
                {
                    string key = $"{col}_{row}";
                    if (wildMultipliers.TryGetValue(key, out int multiplier))
                    {
                        // Map wild multiplier to correct symbol ID
                        symbolId = GetWildSymbolIdForMultiplier(multiplier, gameConfig);
                    }
                }

                column.Add(symbolId);
            }

            matrix.Add(column);
        }

        return matrix;
    }

    /// <summary>
    /// Maps wild multiplier to correct symbol ID
    /// 1x → 11 (Wild), 2x → 13 (Wild2x), 3x → 14 (Wild3x), 5x → 15 (Wild5x)
    /// </summary>
    private static int GetWildSymbolIdForMultiplier(int multiplier, GameConfig gameConfig)
    {
        return multiplier switch
        {
            1 => 11,  // Wild (normal)
            2 => 13,  // Wild 2x
            3 => 14,  // Wild 3x
            5 => 15,  // Wild 5x
            _ => 11   // Default to normal wild
        };
    }

    /// <summary>
    /// Generate default matrix if conversion fails
    /// </summary>
    private static List<List<int>> GenerateDefaultMatrix()
    {
        var matrix = new List<List<int>>();
        for (int col = 0; col < 5; col++)
        {
            var column = new List<int>();
            for (int row = 0; row < 4; row++)
            {
                column.Add(0);
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