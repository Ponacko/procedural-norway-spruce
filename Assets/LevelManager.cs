using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour {

    public void Redraw() {
        var generators = FindObjectsOfType<MyLSystem2>();
        foreach (var g in generators) {
            g.Redraw();
        }
    }

    public void ResetScene() {
        SceneManager.LoadScene(0);
    }
}
