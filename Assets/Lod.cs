using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Lod : MonoBehaviour {
    public List<GameObject> trees = new List<GameObject>();
    public Camera cam;
    public List<float> thresh = new List<float>();
    public int state = 0;

	// Use this for initialization
	void Start () {
        foreach (var t in trees)
        {
            t.SetActive(false);
        }

    }

    private void SetLodActive() {
        for (int i = 0; i < trees.Count; i++) {
            if (i == state) {
                trees[i].SetActive(true);
            }
            else {
                trees[i].SetActive(false);
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
