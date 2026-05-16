using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SudokuFeedbackOverlay : MonoBehaviour {
    public static SudokuFeedbackOverlay Instance;

    [Header("UI References")]
    public GameObject mistakeContainer;
    public GameObject correctContainer;
    public float displayDuration = 1.5f;

    private void Awake() {
        Instance = this;
        if (mistakeContainer) mistakeContainer.SetActive(false);
        if (correctContainer) correctContainer.SetActive(false);
    }

    public void ShowMistake() {
        StopAllCoroutines();
        StartCoroutine(ShowRoutine(mistakeContainer));
    }

    public void ShowCorrect() {
        StopAllCoroutines();
        StartCoroutine(ShowRoutine(correctContainer));
    }

    private IEnumerator ShowRoutine(GameObject container) {
        if (container == null) yield break;
        
        container.SetActive(true);
        yield return new WaitForSeconds(displayDuration);
        container.SetActive(false);
    }
}
