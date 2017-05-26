using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour {

    public void Redraw() {
        var generators = FindObjectsOfType<Generator>();
        foreach (var g in generators) {
            g.Redraw();
        }
    }

    public void ResetScene() {
        SceneManager.LoadScene(0);
    }
}
