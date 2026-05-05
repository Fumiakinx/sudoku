using UnityEngine;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private SudokuBoard sudokuBoard;
    [SerializeField] private SudokuData sudokuData;
    [SerializeField] private GraphicalTimer graphicalTimer;

    public int MistCount { get; private set; }
    public int MistLimit { get; private set; }
    public bool IsUnlimitedMode { get; private set; }
    public float GameTime { get; private set; }
    public bool IsPaused { get; private set; }
    public bool IsGameOver { get; private set; }

    public event Action OnMistakeUpdated;
    public event Action OnGameWon;
    public event Action OnGameLost;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void StartNewGame(SudokuLogic.Difficulty difficulty, bool unlimited)
    {
        MistCount = 0;
        IsUnlimitedMode = unlimited;
        MistLimit = unlimited ? int.MaxValue : sudokuData.defaultMistLimit;
        GameTime = 0;
        IsPaused = false;
        IsGameOver = false;

        sudokuBoard.GenerateNewGame(difficulty);
    }

    private void Update()
    {
        if (!IsPaused && !IsGameOver)
        {
            GameTime += Time.deltaTime;
            if (graphicalTimer != null) graphicalTimer.UpdateTime(GameTime);
        }
    }

    public void OnMistake()
    {
        MistCount++;
        OnMistakeUpdated?.Invoke();

        if (!IsUnlimitedMode && MistCount >= MistLimit)
        {
            GameOver(false);
        }
    }

    public void OnWin()
    {
        GameOver(true);
    }

    private void GameOver(bool won)
    {
        IsGameOver = true;
        if (won)
        {
            OnGameWon?.Invoke();
            // 将来的な報酬システム：ミス3回以内でクリアしたらフラグを立てる等
        }
        else
        {
            OnGameLost?.Invoke();
        }
    }

    public void TogglePause()
    {
        IsPaused = !IsPaused;
    }

    public void AbortGame()
    {
        IsGameOver = true;
    }
}
