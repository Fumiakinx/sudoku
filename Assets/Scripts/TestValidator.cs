using UnityEngine;
public class TestValidator : MonoBehaviour {
    void Start() { Debug.Log("Test"); DestroyImmediate(gameObject); }
    void Update() { DestroyImmediate(gameObject); }
}
