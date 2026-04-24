using System;
using UnityEngine;
using TMPro;

public class HistoryRow : MonoBehaviour
{
    [Header("Row Data Fields")]
    [SerializeField] private TextMeshProUGUI noText;
    [SerializeField] private TextMeshProUGUI betIdText;
    [SerializeField] private TextMeshProUGUI dateTimeText;
    [SerializeField] private TextMeshProUGUI gameModeText;
    [SerializeField] private TextMeshProUGUI startingBalanceText;
    [SerializeField] private TextMeshProUGUI betText;
    [SerializeField] private TextMeshProUGUI winLossText;
    [SerializeField] private TextMeshProUGUI balanceText;

    [Header("Visual Settings")]
    [SerializeField] private Color positiveColor = Color.green;
    [SerializeField] private Color negativeColor = Color.red;
    [SerializeField] private Color neutralColor = Color.white;

    private int rowIndex;

    /// <summary>
    /// Set all row data from a BetHistoryItem
    /// </summary>
    public void SetRowData(int index, BetHistoryItem historyItem)
    {
        rowIndex = index;

        // Set row number
        if (noText != null)
        {
            noText.text = index.ToString();
        }

        // Set bet slip number (ID)
        if (betIdText != null)
        {
            betIdText.text = TruncateBetId(historyItem.betSlipNumber);
        }

        // Set formatted date and time
        if (dateTimeText != null)
        {
            dateTimeText.text = FormatDateTime(historyItem.date);
        }

        // Set game mode
        if (gameModeText != null)
        {
            gameModeText.text = historyItem.gameMode;
        }

        // Set starting balance
        if (startingBalanceText != null)
        {
            startingBalanceText.text = FormatCurrency(historyItem.startingBalance);
        }

        // Set bet amount
        if (betText != null)
        {
            betText.text = FormatCurrency(historyItem.bet);
        }

        // Set win/loss with color coding
        if (winLossText != null)
        {
            double winLoss = historyItem.winLoss;
            winLossText.text = FormatCurrency(winLoss);

            // Color code based on win/loss
            if (winLoss > 0)
            {
                winLossText.color = positiveColor;
            }
            else if (winLoss < 0)
            {
                winLossText.color = negativeColor;
            }
            else
            {
                winLossText.color = neutralColor;
            }
        }

        // Set final balance
        if (balanceText != null)
        {
            balanceText.text = FormatCurrency(historyItem.balance);
        }
    }

    /// <summary>
    /// Clear all row data
    /// </summary>
    public void ClearRowData()
    {
        if (noText != null) noText.text = "";
        if (betIdText != null) betIdText.text = "";
        if (dateTimeText != null) dateTimeText.text = "";
        if (gameModeText != null) gameModeText.text = "";
        if (startingBalanceText != null) startingBalanceText.text = "";
        if (betText != null) betText.text = "";
        if (winLossText != null)
        {
            winLossText.text = "";
            winLossText.color = neutralColor;
        }
        if (balanceText != null) balanceText.text = "";
    }

    /// <summary>
    /// Format currency with 2 decimal places
    /// </summary>
    private string FormatCurrency(double amount)
    {
        return amount.ToString("F2");
    }

    /// <summary>
    /// Truncate long bet IDs to show first 8 characters
    /// </summary>
    private string TruncateBetId(string betId)
    {
        if (string.IsNullOrEmpty(betId))
            return "-";

        // Show last part of bet ID after underscore
        int underscoreIndex = betId.LastIndexOf('_');
        if (underscoreIndex >= 0 && underscoreIndex < betId.Length - 1)
        {
            string shortId = betId.Substring(underscoreIndex + 1);
            return shortId.Length > 12 ? shortId.Substring(0, 12) + "..." : shortId;
        }

        return betId.Length > 12 ? betId.Substring(0, 12) + "..." : betId;
    }

    /// <summary>
    /// Format ISO date string to readable format
    /// </summary>
    private string FormatDateTime(string isoDate)
    {
        if (string.IsNullOrEmpty(isoDate))
            return "-";

        try
        {
            DateTime dateTime = DateTime.Parse(isoDate);
            return dateTime.ToString("dd/MM/yyyy HH:mm");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[HistoryRow] Date parse failed: {e.Message}");
            return isoDate;
        }
    }

    /// <summary>
    /// Toggle row visibility
    /// </summary>
    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }
}
