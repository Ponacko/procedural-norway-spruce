using System;
using UnityEngine;
using System.Collections;
using System.Text;
using System.Linq;
using System.Collections.Generic;

public class MyLSystem : MonoBehaviour {
    public string axiom = "F";

    [System.Serializable]
    public struct Rule {
        public string pred;
        public string succ;
    }

    [System.Serializable]
    public struct Parameter {
        public string name;
        public string value;
    }

    public Rule[] rulesArray;
    public Parameter[] parameterArray;
    public float angle = 30;
    public float length = 2;
    public float width = 0.2f;
    public int iterations = 1;
    public GameObject tree;

    class State {
        public float yaw; //left or right
        public float pitch; //forward or backwards
        public float roll;
        public float nextRoll;
        public float width;
        public float nextWidth;
        public float x;
        public float y;
        public float z;

        public State Clone() {
            return (State) this.MemberwiseClone();
        }
    }

    State currentState = new State() {yaw = 0, pitch = 0, roll = 45, nextRoll = 45, width = 0, nextWidth = 0, x = 0, y = 0, z = 0};
    Dictionary<string, string> rules = new Dictionary<string, string>();
    Dictionary<string, string> parameters = new Dictionary<string, string>();
    Stack<State> states = new Stack<State>();

    void Awake() {
        foreach (var c in rulesArray) {
            rules.Add(c.pred, c.succ);
        }
        foreach (var c in parameterArray) {
            parameters.Add(c.name, c.value);
        }
    }


    void Start() {
        var sentence = axiom;
        for (int i = 0; i < iterations; i++) {
            sentence = Replace(sentence);
            Debug.Log(sentence);
        }
        Draw(sentence);
    }

    private float DegreeToRadian(float angle) {
        return (float) (Mathf.PI*angle/180.0);
    }

    private float widthPosX(State state, float angle, float width) {
        return Mathf.Sqrt(2)*Mathf.Cos(DegreeToRadian(90 - (state.roll + angle)))*width;
    }

    private float widthPosZ(State state, float angle, float width) {
        return Mathf.Sqrt(2)*Mathf.Sin(DegreeToRadian(90 - (state.roll + angle)))*width;
    }

    Mesh CreateMesh(float width, float height, State currentState, State nextState) {
        Mesh m = new Mesh();
        m.name = "ScriptedMesh";
        m.vertices = new Vector3[] {
            new Vector3(currentState.x + widthPosX(currentState, 180, currentState.width), currentState.y,
                currentState.z + widthPosZ(currentState, 180, currentState.width)),
            new Vector3(currentState.x + widthPosX(currentState, 90, currentState.width), currentState.y,
                currentState.z + widthPosZ(currentState, 90, currentState.width)),
            new Vector3(currentState.x + widthPosX(currentState, 0, currentState.width), currentState.y,
                currentState.z + widthPosZ(currentState, 0, currentState.width)),
            new Vector3(currentState.x + widthPosX(currentState, -90,currentState. width), currentState.y,
                currentState.z + widthPosZ(currentState, -90, currentState.width)),
            new Vector3(nextState.x + widthPosX(nextState, 180, nextState.width), nextState.y,
                nextState.z + widthPosZ(nextState, 180, nextState.width)),
            new Vector3(nextState.x + widthPosX(nextState, 90, nextState.width), nextState.y,
                nextState.z + widthPosZ(nextState, 90, nextState.width)),
            new Vector3(nextState.x + widthPosX(nextState, 0, nextState.width), nextState.y,
                nextState.z + widthPosZ(nextState, 0, nextState.width)),
            new Vector3(nextState.x + widthPosX(nextState, -90, nextState.width), nextState.y,
                nextState.z + widthPosZ(nextState, -90, nextState.width))
        };
        m.uv = new Vector2[] {
            new Vector2(0, 0),
            new Vector2(0, 1),
            new Vector2(1, 1),
            new Vector2(1, 0),
            new Vector2(2, 1),
            new Vector2(2, 0),
            new Vector2(3, 1),
            new Vector2(3, 0)
        };
        m.triangles = new int[] {
            0, 1, 2, 0, 2, 3,
            6, 5, 4, 7, 6, 4,
            4, 5, 1, 4, 1, 0,
            5, 6, 2, 5, 2, 1,
            6, 7, 3, 6, 3, 2,
            7, 4, 0, 7, 0, 3
        };
        currentState = nextState;
        m.RecalculateNormals();

        return m;
    }

    private float GetParameter(string s, int i) {
        if (s[i + 1] == '(') {
            int end = s.IndexOf(')', i + 1);
            return float.Parse(s.Substring(i + 2, end - i - 2));
        }
        else {
            return 0;
        }
    }

    public void Draw(string s) {
        for (int index = 0; index < s.Length; index++) {
            char c = s[index];
            switch (c) {
                case 'F':
                    GameObject plane = new GameObject("Branch");
                    plane.transform.SetParent(tree.transform);
                    MeshFilter meshFilter = (MeshFilter) plane.AddComponent(typeof(MeshFilter));
                    State nextState = new State() {
                        //x = currentState.x + length*Mathf.Cos(DegreeToRadian(90 - currentState.yaw)),
                        //y =
                        //    currentState.y +
                        //    length*Mathf.Sin(DegreeToRadian(90 - currentState.yaw))*
                        //    Mathf.Sin(DegreeToRadian(90 - currentState.pitch)),
                        //z = currentState.z + length*Mathf.Cos(DegreeToRadian(90 - currentState.pitch)),
                        yaw = currentState.yaw,
                        pitch = currentState.pitch,
                        roll = currentState.nextRoll,
                        nextRoll = currentState.nextRoll,
                        width = currentState.nextWidth,
                        nextWidth = currentState.nextWidth
                    };
                    float meshLength = GetParameter(s, index);
                    meshFilter.mesh = CreateMesh(width, meshLength, currentState, nextState);
                    MeshRenderer renderer = plane.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
                    renderer.material.shader = Shader.Find("Diffuse");
                    Texture2D tex = new Texture2D(1, 1);
                    tex.SetPixel(0, 0, Color.green);
                    tex.Apply();
                    renderer.material.mainTexture = tex;
                    renderer.material.color = Color.black;
                    currentState = nextState;
                    break;

                case '[':
                    states.Push(currentState.Clone());
                    break;

                case ']':
                    currentState = states.Pop();
                    break;

                case '+':
                    currentState.yaw += GetParameter(s, index);
                    break;

                case '-':
                    currentState.yaw -= GetParameter(s, index);
                    break;
                case '^':
                    currentState.pitch += GetParameter(s, index);
                    break;
                case '&':
                    currentState.pitch -= GetParameter(s, index);
                    break;
                case '/':
                    currentState.nextRoll += GetParameter(s, index);
                    break;
                case '\\':
                    currentState.nextRoll -= GetParameter(s, index);
                    break;
                case '$':
                    currentState.nextRoll = 45;
                    break;
                case '!':
                    if (currentState.width == 0) {
                        currentState.width = GetParameter(s, index) / 10;
                    }
                    currentState.nextWidth = GetParameter(s, index)/10;
                    break;
            }
        }
    }

    private string[] getParamsFromString(string s, int i, out int end) {
        //if (i + 1 > s.Length) {
        //    end = -1;
        //    return null;
        //}
        end = s.IndexOf(')', i + 1);
        string[] ruleParameters = s.Substring(i + 2, end - i - 2).Split(',');
        return ruleParameters;
    }

    private string ReplaceParams(string s) {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < s.Length; i++) {
            if (s[i] == '(') {
                int formalEnd;
                string[] formalParams = getParamsFromString(s, i - 1, out formalEnd);
                string[] actualParams = new string[formalParams.Length];
                for (int j = 0; j < formalParams.Length; j++) {
                    string[] multipliers = formalParams[j].Split('*');
                    if (multipliers.Length > 1)
                    {
                        float f = multipliers.Aggregate<string, float>(1, (current, m) => current * float.Parse(parameters[m]));
                        actualParams[j] = f.ToString();
                    }
                    else {
                        actualParams[j] = parameters[formalParams[j]];
                    }
                    
                }
                sb.Append('(');
                for (int j = 0; j < actualParams.Length; j++) {
                    sb.Append(actualParams[j]);
                    if (j != actualParams.Length - 1) {
                        sb.Append(',');
                    }
                }
                sb.Append(')');
                i = formalEnd;
            }
            else {
                sb.Append(s[i]);
            }
        }
        return sb.ToString();
    }

    public string Replace(string s) {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < s.Length; i++) {
            var rule = rules.FirstOrDefault(x => x.Key.StartsWith(s[i].ToString()));
            if (rule.Key != null) {
                if ((i + 1) < s.Length && s[i + 1] == '(') {
                    int actualEnd, formalEnd;
                    string[] actualParams = getParamsFromString(s, i, out actualEnd);
                    string[] formalParams = getParamsFromString(rule.Key, 0, out formalEnd);
                    for (int j = 0; j < formalParams.Length; j++) {
                        if (parameters.ContainsKey(formalParams[j])) {
                            parameters[formalParams[j]] = actualParams[j];
                        }
                        else {
                            parameters.Add(formalParams[j], actualParams[j]);
                        }
                    }
                    string ruleValue = ReplaceParams(rules[rule.Key]);
                    sb.Append(ruleValue);
                    i = actualEnd;
                }
                else {
                    sb.Append(rules[rule.Key]);
                }
            }
            else {
                sb.Append(s[i]);
            }
        }
        return sb.ToString();
    }
}
