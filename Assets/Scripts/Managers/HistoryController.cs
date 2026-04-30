using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;

public class HistoryController : MonoBehaviour
{
    [Header("Panel References")]
    [SerializeField] private GameObject historyPanel;
    [SerializeField] private SocketIOManager socketManager;
    [SerializeField] private PopupManager popupManager;

    [Header("History Rows")]
    [SerializeField] private HistoryRow[] historyRows; // Assign 10 row components in inspector

    [Header("Summary Texts")]
    [SerializeField] private TextMeshProUGUI totalBetsText;
    [SerializeField] private TextMeshProUGUI totalBetValueText;
    [SerializeField] private TextMeshProUGUI totalWinLossText;
    [SerializeField] private TextMeshProUGUI pageInfoText;

    [Header("Navigation Buttons")]
    [SerializeField] private Button previousPageButton;
    [SerializeField] private Button nextPageButton;
    [SerializeField] private Button backToGameButton;

    [Header("Visual Settings")]
    [SerializeField] private Color positiveColor = Color.green;
    [SerializeField] private Color negativeColor = Color.red;

    // Pagination
    private int currentPage = 1;
    private int totalPages = 1;
    private int itemsPerPage = 10;
    private int totalItems = 0;

    // Current data
    private List<BetHistoryItem> currentHistoryData;

    // Statistics
    private int totalBetsCount = 0;
    private double totalBetValue = 0;
    private double totalWinLoss = 0;

    private bool isLoading = false;
    private RectTransform historyPanelRect;

    #region Initialization

    private void Awake()
    {
        if (historyPanel != null)
        {
            historyPanel.SetActive(false);
            historyPanelRect = historyPanel.GetComponent<RectTransform>();
        }

        SetupButtons();
    }

    private void SetupButtons()
    {
        if (previousPageButton != null)
        {
            previousPageButton.onClick.AddListener(OnPreviousPageClicked);
        }

        if (nextPageButton != null)
        {
            nextPageButton.onClick.AddListener(OnNextPageClicked);
        }

        if (backToGameButton != null)
        {
            backToGameButton.onClick.AddListener(OnBackToGameClicked);
        }
    }

    #endregion

    #region Panel Control

    /// <summary>
    /// Open history panel and load first page
    /// </summary>
    internal void OpenHistoryPanel()
    {
        if (isLoading) return;

        currentPage = 1;
        RequestHistoryData(currentPage);
    }

    /// <summary>
    /// Close history panel
    /// </summary>
    internal void CloseHistoryPanel()
    {
        if (historyPanel == null || !historyPanel.activeSelf) return;

        if (historyPanelRect != null)
        {
            historyPanelRect.DOAnchorPosX(Screen.width, 0.35f)
                .SetEase(Ease.InCubic)
                .OnComplete(() =>
                {
                    historyPanel.SetActive(false);
                    if (historyPanelRect != null)
                    {
                        historyPanelRect.anchoredPosition = new Vector2(0f, historyPanelRect.anchoredPosition.y);
                    }
                    ClearAllRows();
                });
        }
        else
        {
            historyPanel.SetActive(false);
            ClearAllRows();
        }
    }

    #endregion

    #region Data Request

    /// <summary>
    /// Request history data from server for specific page
    /// </summary>
    private void RequestHistoryData(int page)
    {
        if (socketManager == null || isLoading)
        {
            Debug.LogWarning("[HistoryController] Cannot request data - socketManager null or already loading");
            return;
        }

        isLoading = true;
        UpdateNavigationButtons();

        // Show loading popup only if history panel is not active
        if (popupManager != null && (historyPanel == null || !historyPanel.activeSelf))
        {
            popupManager.ShowLoadingPopup();
        }

        // Send request to socket manager
        socketManager.SendBetHistoryRequest(page);
    }

    /// <summary>
    /// Called by SocketIOManager when history data is received
    /// </summary>
    internal void OnHistoryDataReceived(BetHistoryResponse response)
    {
        isLoading = false;

        if (!response.success)
        {
            if (popupManager != null) popupManager.CloseLoadingPopup();
            Debug.LogError("[HistoryController] History request failed");
            return;
        }

        // Store current data
        currentHistoryData = response.payload.betHistory;

        // Update pagination info
        currentPage = response.payload.pagination.page;
        totalPages = response.payload.pagination.totalPages;
        totalItems = response.payload.pagination.total;

        // Use statistics directly from server pagination
        totalBetsCount = response.payload.pagination.total;
        totalBetValue = response.payload.pagination.totalBetAmount;
        totalWinLoss = response.payload.pagination.totalWinLoss;

        // Update UI
        UpdateHistoryRows();
        UpdateSummaryDisplay();
        UpdatePageInfo();
        UpdateNavigationButtons();

        // Close loading popup and THEN show panel with animation
        if (popupManager != null)
        {
            popupManager.CloseLoadingPopup(ShowPanelWithAnimation);
        }
        else
        {
            ShowPanelWithAnimation();
        }
    }

    private void ShowPanelWithAnimation()
    {
        // Show panel with animation if it's not active
        if (historyPanel != null && !historyPanel.activeSelf)
        {
            historyPanel.SetActive(true);
            if (historyPanelRect != null)
            {
                historyPanelRect.anchoredPosition = new Vector2(Screen.width, historyPanelRect.anchoredPosition.y);
                historyPanelRect.DOAnchorPosX(0f, 0.35f).SetEase(Ease.OutCubic);
            }
        }
    }

    #endregion

    #region Data Processing


    #endregion

    #region UI Update

    /// <summary>
    /// Update all history rows with current data
    /// </summary>
    private void UpdateHistoryRows()
    {
        if (historyRows == null || historyRows.Length == 0)
        {
            Debug.LogWarning("[HistoryController] No history rows assigned");
            return;
        }

        // Clear all rows first
        ClearAllRows();

        if (currentHistoryData == null || currentHistoryData.Count == 0)
        {
            return;
        }

        // Calculate starting index for row numbering (1-based)
        int startingNumber = ((currentPage - 1) * itemsPerPage) + 1;

        // Fill rows with data
        for (int i = 0; i < historyRows.Length && i < currentHistoryData.Count; i++)
        {
            if (historyRows[i] != null)
            {
                int rowNumber = startingNumber + i;
                historyRows[i].SetRowData(rowNumber, currentHistoryData[i]);
                historyRows[i].SetVisible(true);
            }
        }
    }

    /// <summary>
    /// Update summary statistics display
    /// </summary>
    private void UpdateSummaryDisplay()
    {
        // Total bets count
        if (totalBetsText != null)
        {
            totalBetsText.text = $"{totalBetsCount}";
        }

        // Total bet value
        if (totalBetValueText != null)
        {
            totalBetValueText.text = $"{totalBetValue:F2}";
        }

        // Total win/loss with color coding
        if (totalWinLossText != null)
        {
            string sign = totalWinLoss > 0 ? "+" : "";
            totalWinLossText.text = $"{sign}{totalWinLoss:F2}";

            if (totalWinLoss > 0)
            {
                totalWinLossText.color = positiveColor;
            }
            else if (totalWinLoss < 0)
            {
                totalWinLossText.color = negativeColor;
            }
            else
            {
                totalWinLossText.color = Color.white;
            }
        }
    }

    /// <summary>
    /// Update page information display (e.g., "1/15")
    /// </summary>
    private void UpdatePageInfo()
    {
        if (pageInfoText != null)
        {
            pageInfoText.text = $"{currentPage}/{totalPages}";
        }
    }

    /// <summary>
    /// Update navigation button interactability
    /// </summary>
    private void UpdateNavigationButtons()
    {
        if (previousPageButton != null)
        {
            previousPageButton.interactable = currentPage > 1;
        }

        if (nextPageButton != null)
        {
            nextPageButton.interactable =  currentPage < totalPages;
        }
    }

    /// <summary>
    /// Clear all row data
    /// </summary>
    private void ClearAllRows()
    {
        if (historyRows == null) return;

        foreach (var row in historyRows)
        {
            if (row != null)
            {
                row.ClearRowData();
                row.SetVisible(false);
            }
        }
    }

    #endregion

    #region Button Callbacks

    private void OnPreviousPageClicked()
    {
        if (isLoading || currentPage <= 1) return;

        AudioManager.Instance?.PlayPageSwipe();
        RequestHistoryData(currentPage - 1);
    }

    private void OnNextPageClicked()
    {
        if (isLoading || currentPage >= totalPages) return;

        AudioManager.Instance?.PlayPageSwipe();
        RequestHistoryData(currentPage + 1);
    }

    private void OnBackToGameClicked()
    {
        AudioManager.Instance?.PlayPopupClose();
        CloseHistoryPanel();
    }

    #endregion
}