using UnityEngine;

public class RandomGhostCleaner : MonoBehaviour {
    // Empty class


public void ApplyTheme() { Debug.Log("Ghost test"); }


public bool IsModern(SudokuData.SudokuTheme theme) => theme.themeName != null && theme.themeName.ToLower().Contains("modern");
}
