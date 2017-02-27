//using System;
//using UnityEngine;
//using System.Collections;
//using System.Text;
//using System.Linq;
//using System.Collections.Generic;
//using System.Security.Cryptography.X509Certificates;
//using UnityEngine.Assertions.Comparers;

//public class MyLSystem2 : MonoBehaviour
//{
//    public string axiom = "F";

//    [System.Serializable]
//    public struct Rule
//    {
//        public string pred;
//        public string succ;
//    }

//    [System.Serializable]
//    public struct Parameter
//    {
//        public string name;
//        public string value;
//    }

//    public Rule[] rulesArray;
//    public Parameter[] parameterArray;
//    public int iterations = 1;
//    public GameObject tree;

//    public class Rotation
//    {
//        public string name;
//        public Quaternion rot;

//        public Rotation(string name, Quaternion rot)
//        {
//            this.name = name;
//            this.rot = rot;
//        }
//    }

//    class State
//    {
//        public List<Rotation> rotations;
//        public float width;
//        public float nextWidth;
//        public Vector3 position;

//        public State Clone()
//        {
//            return (State)this.MemberwiseClone();
//        }
//    }
//    State currentState = new State() { rotations = new List<Rotation>(), width = 0, nextWidth = 0, position = Vector3.zero };
//    Dictionary<string, string> rules = new Dictionary<string, string>();
//    Dictionary<string, string> parameters = new Dictionary<string, string>();
//    Stack<State> states = new Stack<State>();

//    void Awake()
//    {
//        foreach (var c in rulesArray)
//        {
//            rules.Add(c.pred, c.succ);
//        }
//        foreach (var c in parameterArray)
//        {
//            parameters.Add(c.name, c.value);
//        }
//    }


//    void Start()
//    {
//        var sentence = axiom;
//        for (int i = 0; i < iterations; i++)
//        {
//            sentence = Replace(sentence);
//            Debug.Log(sentence);
//        }
//        Draw(sentence);
//    }

//    Mesh CreateMesh(State currentState, State nextState)
//    {
//        Mesh m = new Mesh();
//        m.name = "ScriptedMesh";
//        m.vertices = new Vector3[] {
//            currentState.position + new Vector3(-currentState.width, 0, -currentState.width),
//            currentState.position + new Vector3(currentState.width, 0, -currentState.width),
//            currentState.position + new Vector3(currentState.width, 0, currentState.width),
//            currentState.position + new Vector3(-currentState.width, 0, currentState.width),
//            nextState.position + new Vector3(-nextState.width, 0, -nextState.width),
//            nextState.position + new Vector3(nextState.width, 0, -nextState.width),
//            nextState.position + new Vector3(nextState.width, 0, nextState.width),
//            nextState.position + new Vector3(-nextState.width, 0, nextState.width)
//        };
//        m.uv = new Vector2[] {
//            new Vector2(0, 0),
//            new Vector2(0, 1),
//            new Vector2(1, 1),
//            new Vector2(1, 0),
//            new Vector2(2, 1),
//            new Vector2(2, 0),
//            new Vector2(3, 1),
//            new Vector2(3, 0)
//        };
//        m.triangles = new int[] {
//            0, 1, 2, 0, 2, 3,
//            6, 5, 4, 7, 6, 4,
//            4, 5, 1, 4, 1, 0,
//            5, 6, 2, 5, 2, 1,
//            6, 7, 3, 6, 3, 2,
//            7, 4, 0, 7, 0, 3
//        };
//        currentState = nextState;
//        m.RecalculateNormals();

//        return m;
//    }

//    private float GetParameter(string s, int i)
//    {
//        if (s[i + 1] == '(')
//        {
//            int end = s.IndexOf(')', i + 1);
//            return float.Parse(s.Substring(i + 2, end - i - 2));
//        }
//        else
//        {
//            return 0;
//        }
//    }

//    private Quaternion QuatProduct(State state)
//    {
//        var acc = Quaternion.identity;
//        foreach (var r in state.rotations)
//        {
//            acc *= r.rot;
//        }
//        return acc;
//    }

//    public void Draw(string s)
//    {
//        for (int index = 0; index < s.Length; index++)
//        {
//            char c = s[index];
//            switch (c)
//            {
//                case 'F':
//                    GameObject plane = new GameObject("Branch");
//                    plane.transform.SetParent(tree.transform);
//                    MeshFilter meshFilter = (MeshFilter)plane.AddComponent(typeof(MeshFilter));
//                    float meshLength = GetParameter(s, index);
//                    State nextState = new State()
//                    {
//                        position = currentState.position +
//                        QuatProduct(currentState)
//                        * new Vector3(0, meshLength, 0),
//                        rotations = currentState.rotations,
//                        width = currentState.nextWidth,
//                        nextWidth = currentState.nextWidth
//                    };
//                    meshFilter.mesh = CreateMesh(currentState, nextState);
//                    MeshRenderer renderer = plane.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
//                    renderer.material.shader = Shader.Find("Diffuse");
//                    Texture2D tex = new Texture2D(1, 1);
//                    tex.SetPixel(0, 0, new Color(0.5f, 0f, 0.2f));
//                    tex.Apply();
//                    renderer.material.mainTexture = tex;
//                    renderer.material.color = new Color(0.5f, 0f, 0.2f);
//                    currentState = nextState;
//                    break;

//                case '[':
//                    states.Push(currentState.Clone());
//                    break;

//                case ']':
//                    currentState = states.Pop();
//                    break;

//                case '+':
//                    currentState.rotations.Add(new Rotation("yaw", Quaternion.AngleAxis(GetParameter(s, index), Vector3.back)));
//                    break;

//                case '-':
//                    currentState.rotations.Add(new Rotation("yaw", Quaternion.AngleAxis(-GetParameter(s, index), Vector3.back)));
//                    break;
//                case '^':
//                    currentState.rotations.Add(new Rotation("pitch", Quaternion.AngleAxis(GetParameter(s, index), Vector3.left)));
//                    break;
//                case '&':
//                    currentState.rotations.Add(new Rotation("pitch", Quaternion.AngleAxis(-GetParameter(s, index), Vector3.left)));
//                    break;
//                case '/':
//                    currentState.rotations.Add(new Rotation("roll", Quaternion.AngleAxis(GetParameter(s, index), Vector3.up)));
//                    break;
//                case '\\':
//                    currentState.rotations.Add(new Rotation("roll", Quaternion.AngleAxis(-GetParameter(s, index), Vector3.up)));
//                    break;
//                case '$':
//                    currentState.rotations.RemoveAll(x => x.name == "roll");
//                    break;
//                case '!':
//                    if (currentState.width == 0)
//                    {
//                        currentState.width = GetParameter(s, index) / 50;
//                    }
//                    currentState.nextWidth = GetParameter(s, index) / 50;
//                    break;
//            }
//        }
//    }

//    private string[] getParamsFromString(string s, int i, out int end)
//    {
//        end = s.IndexOf(')', i + 1);
//        string[] ruleParameters = s.Substring(i + 2, end - i - 2).Split(',');
//        return ruleParameters;
//    }

//    private string ReplaceParams(string s)
//    {
//        StringBuilder sb = new StringBuilder();
//        for (int i = 0; i < s.Length; i++)
//        {
//            if (s[i] == '(')
//            {
//                int formalEnd;
//                string[] formalParams = getParamsFromString(s, i - 1, out formalEnd);
//                string[] actualParams = new string[formalParams.Length];
//                for (int j = 0; j < formalParams.Length; j++)
//                {
//                    string[] multipliers = formalParams[j].Split('*');
//                    if (multipliers.Length > 1)
//                    {
//                        float f = multipliers.Aggregate<string, float>(1, (current, m) => current * float.Parse(parameters[m]));
//                        actualParams[j] = f.ToString();
//                    }
//                    else
//                    {
//                        actualParams[j] = parameters[formalParams[j]];
//                    }

//                }
//                sb.Append('(');
//                for (int j = 0; j < actualParams.Length; j++)
//                {
//                    sb.Append(actualParams[j]);
//                    if (j != actualParams.Length - 1)
//                    {
//                        sb.Append(',');
//                    }
//                }
//                sb.Append(')');
//                i = formalEnd;
//            }
//            else
//            {
//                sb.Append(s[i]);
//            }
//        }
//        return sb.ToString();
//    }

//    public string Replace(string s)
//    {
//        StringBuilder sb = new StringBuilder();
//        for (int i = 0; i < s.Length; i++)
//        {
//            var rule = rules.FirstOrDefault(x => x.Key.StartsWith(s[i].ToString()));
//            if (rule.Key != null)
//            {
//                if ((i + 1) < s.Length && s[i + 1] == '(')
//                {
//                    int actualEnd, formalEnd;
//                    string[] actualParams = getParamsFromString(s, i, out actualEnd);
//                    string[] formalParams = getParamsFromString(rule.Key, 0, out formalEnd);
//                    for (int j = 0; j < formalParams.Length; j++)
//                    {
//                        if (parameters.ContainsKey(formalParams[j]))
//                        {
//                            parameters[formalParams[j]] = actualParams[j];
//                        }
//                        else
//                        {
//                            parameters.Add(formalParams[j], actualParams[j]);
//                        }
//                    }
//                    string ruleValue = ReplaceParams(rules[rule.Key]);
//                    sb.Append(ruleValue);
//                    i = actualEnd;
//                }
//                else
//                {
//                    sb.Append(rules[rule.Key]);
//                }
//            }
//            else
//            {
//                sb.Append(s[i]);
//            }
//        }
//        return sb.ToString();
//    }
//}
