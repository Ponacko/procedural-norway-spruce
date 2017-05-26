using UnityEngine;
using System.Collections.Generic;

public class Lod : MonoBehaviour {
    public List<GameObject> trees = new List<GameObject>();
    private Camera cam;
    public List<float> thresh = new List<float>();
    public int state = 0;

	// Use this for initialization
	void Start () {
	    cam = FindObjectOfType<Camera>();
        foreach (var t in trees)
        {
            if (t != null) {
                t.SetActive(false);
            }
        }

    }

    private void SetLodActive() {
        for (int i = 0; i < trees.Count; i++) {
            if (trees[i] != null) {
                trees[i].SetActive(i == state);
            }
        }
    }
	
	// Update is called once per frame
	void Update () {
	    float distance = Vector3.Distance(cam.transform.position, transform.position);
	    if (state < thresh.Count && distance > thresh[state]) {
	        state++;
            SetLodActive();
	    }
	    if (state > 0 && distance < thresh[state - 1]) {
	        state--;
            SetLodActive();
        }
	}
}
