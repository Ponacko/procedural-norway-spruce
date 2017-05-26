using System.Collections;
using UnityEngine;

public class Loading : MonoBehaviour {

    private AsyncOperation async;
    private IEnumerator LoadALevel(int level)
    {
        async = Application.LoadLevelAsync(level);
        return Load();
    }

    IEnumerator Load()
    {
        yield return async;
    }

    void Start() {
        LoadALevel(1);
    }
}
