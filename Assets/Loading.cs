using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Loading : MonoBehaviour {

    private AsyncOperation async = null; // When assigned, load is in progress.
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
