using System;
using UnityEngine;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.UI;

public class MyLSystem2 : MonoBehaviour
{
    public string Axiom = "F";

    [System.Serializable]
    public struct Rule
    {
        public string Pred;
        public string Succ;
    }

    [System.Serializable]
    public struct Parameter
    {
        public string Name;
        public string Value;
    }
    
    public List<GameObject> Sliders = new List<GameObject>();

    public Rule[] RulesArray;
    public Parameter[] ParameterArray;
    public int Iterations = 1;
    public List<GameObject> Trees = new List<GameObject>();
    public Texture2D Tex;
    public Texture2D LeafText;

    private class State {
        public Vector3 Heading;
        public Vector3 Up;
        public Vector3 Left;
        public float Width;
        public Vector3 Position;
        public bool ChangedDir;

        public State Clone()
        {
            return (State)MemberwiseClone();
        }
    }

    private State currentState = new State() { Heading = Vector3.up, Up = Vector3.back,
        Left = Vector3.left,  Width = 0,  Position = Vector3.zero, ChangedDir = false};
    private State nextState;
    private Dictionary<string, string> rules = new Dictionary<string, string>();
    private Dictionary<string, string> parameters = new Dictionary<string, string>();
    private Stack<State> states = new Stack<State>();
    private System.Random r = new System.Random();

    private void Awake()
    {
        nextState = currentState.Clone();
        ResetParams();
    }

    private void ResetParams()
    {
        //StringBuilder sb = new StringBuilder();
        //foreach (var rule in rulesArray)
        //{
        //    sb.Append(string.Format("{0} -> {1}\r\n", rule.pred, rule.succ));
        //}
        //foreach (var param in parameterArray)
        //{
        //    sb.Append(string.Format("{0}: {1}\r\n", param.name, param.value));
        //}
        //Debug.Log(sb.ToString());
        foreach (var c in RulesArray)
        {
            if (!rules.ContainsKey(c.Pred)) {
                rules.Add(c.Pred, c.Succ);
            }
            else {
                rules[c.Pred] = c.Succ;
            }
        }
        foreach (var c in ParameterArray)
        {
            if (!parameters.ContainsKey(c.Name)) {
                parameters.Add(c.Name, c.Value);
            }
            else {
                parameters[c.Name] = c.Value;
            }
        }
    }

    private void Start()
    {
        var sentence = Axiom;
        for (var i = 0; i < Iterations; i++)
        {
            sentence = Replace(sentence);
            Debug.Log(sentence);
        }
        Draw(sentence);
    }

    private List<Vector3> MakeVertices(State state) {
        var vertices = new List<Vector3>();
        vertices.Add(state.Position);
        for (var angle = 0; angle < 360; angle += 45) {
            vertices.Add(state.Position + Quaternion.AngleAxis(angle, state.Heading) * state.Up * state.Width);
        }
        return vertices;
    }

    private Mesh CreateLeaf(State state, int lod) {
        var m = new Mesh();
        var offset = currentState.Width;
        var length = currentState.Width*30;
        m.name = "Leaf";
        if (lod != Trees.Count) {
            m.vertices = new Vector3[] {
                state.Position - offset*state.Heading - offset*state.Left,
                state.Position - offset*state.Heading + offset*state.Left,
                state.Position + offset*state.Heading,
                state.Position + state.Up*length*1.3f + state.Heading*65*offset
            };
            m.uv = new Vector2[] {
                new Vector2(0, 0),
                new Vector2(0, 1),
                new Vector2(1, 0),
                new Vector2(1, 1),
            };
            m.triangles = new[] {
                3, 1, 0,
                3, 2, 1,
                3, 0, 2
            };
        }
        else {
            m.vertices = new Vector3[] {
                state.Position - offset*state.Heading - offset*state.Left,
                state.Position - offset*state.Heading + offset*state.Left,
                state.Position + offset*state.Heading - offset*state.Left,
                state.Position + offset*state.Heading + offset*state.Left,
                state.Position + state.Up*length + state.Heading*50*offset - offset*state.Heading - offset*state.Left,
                state.Position + state.Up*length + state.Heading*50*offset - offset*state.Heading + offset*state.Left,
                state.Position + state.Up*length + state.Heading*50*offset + offset*state.Heading - offset*state.Left,
                state.Position + state.Up*length + state.Heading*50*offset + offset*state.Heading + offset*state.Left,
                state.Position + state.Up*length*1.3f + state.Heading*65*offset
            };
            m.uv = new Vector2[] {
                new Vector2(0, 0),
                new Vector2(0, 1),
                new Vector2(0.25f, 0),
                new Vector2(0.25f, 1),
                new Vector2(0.5f, 0),
                new Vector2(0.5f, 1),
                new Vector2(0.75f, 0),
                new Vector2(0.75f, 1),
                new Vector2(1, 0.5f)
            };
            m.triangles = new[] {
                4, 0, 1,
                4, 1, 5,
                6, 2, 0,
                6, 0, 4,
                7, 3, 6,
                6, 3, 2,
                5, 1, 3,
                5, 3, 7,
                8, 6, 4,
                8, 7, 6,
                8, 5, 7,
                8, 4, 5
            };
        }
        
        return m;
    }

    private List<Vector2> MakeUVs(int sides) {
        float offset = 1f/sides;
        List<Vector2> uvs = new List<Vector2>();
        for (int i = 0; i <= 1f; i++) {
            for (float j = 0f; j <= 1f; j+= offset) {
                uvs.Add(new Vector2(i, j));
            }
        }
        return uvs;
    }

    private Mesh CreateMesh(bool hasBot, bool hasTop)
    {
        var m = new Mesh();
        m.name = "ScriptedMesh";
        
        m.vertices = MakeVertices(currentState).Concat(MakeVertices(nextState)).ToArray();
        m.uv = new Vector2[] {
            new Vector2(0, 0),
            new Vector2(0, 0.125f),
            new Vector2(0, 0.25f),
            new Vector2(0, 0.375f),
            new Vector2(0, 0.5f),
            new Vector2(0, 0.625f),
            new Vector2(0, 0.75f),
            new Vector2(0, 0.875f),
            new Vector2(0, 1),
            new Vector2(1, 0),
            new Vector2(1, 0.125f),
            new Vector2(1, 0.25f),
            new Vector2(1, 0.375f),
            new Vector2(1, 0.5f),
            new Vector2(1, 0.625f),
            new Vector2(1, 0.75f),
            new Vector2(1, 0.875f),
            new Vector2(1, 1),
        };
        var dopici = MakeUVs(8).ToArray();
        foreach (var kokot in dopici) {
            Debug.Log(String.Format("{0},{1}",kokot.x, kokot.y));
        }
        var tris = new List<int>();
        tris.AddRange(new int[] {
            10, 1, 2, 10, 2, 11,
            11, 2, 3, 11, 3, 12,
            12, 3, 4, 12, 4, 13,
            13, 4, 5, 13, 5, 14,
            14, 5, 6, 14, 6, 15,
            15, 6, 7, 15, 7, 16,
            16, 7, 8, 16, 8, 17,
            17, 8, 1, 17, 1, 10});
        if (hasBot) {
            tris.AddRange(new int[] {
                0, 2, 1, 0, 3, 2,
                0, 4, 3, 0, 5, 4,
                0, 6, 5, 0, 7, 6,
                0, 8, 7, 0, 1, 8
            });
        }
        if (hasTop) {
            tris.AddRange(new int[] {
                9, 10, 11, 9, 11, 12,
                9, 12, 13, 9, 13, 14,
                9, 14, 15, 9, 15, 16,
                9, 16, 17, 9, 17, 10
            });
        }
        m.triangles = tris.ToArray();
        m.RecalculateNormals();

        return m;
    }

    private float Randomness(float a, float b) {
        return (float) (a + r.NextDouble() * (b - a));

    }

    private float GetParameter(string s, int i)
    {
        if (s[i + 1] == '(')
        {
            var end = s.IndexOf(')', i + 1);
            return float.Parse(s.Substring(i + 2, end - i - 2)) ;
        }
        else
        {
            return 0;
        }
    }

    private void RotateAxes(float angle, ref Vector3 first, ref Vector3 second, Vector3 around) {
        first = Quaternion.AngleAxis(angle, around) * first;
        second = Quaternion.AngleAxis(angle, around) * second;
    }

    public void Draw(string s)
    {
        for (var index = 0; index < s.Length; index++)
        {
            var c = s[index];
            switch (c)
            {
                case 'F':
                    MoveForward(s, index);
                    break;
                case 'L':
                    MoveForward(s, index);
                    break;
                case 'f':
                    currentState.Position += currentState.Heading*GetParameter(s, index);
                    nextState.Position = currentState.Position;
                    break;

                case '[':
                    states.Push(currentState.Clone());
                    states.Push(nextState.Clone());
                    break;

                case ']':
                    nextState = states.Pop();
                    currentState = states.Pop();
                    break;

                case '+':
                    currentState.ChangedDir = true;
                    RotateAxes(GetParameter(s, index) * Randomness(0.5f, 1.5f), ref nextState.Heading, 
                        ref nextState.Left, nextState.Up);
                    break;

                case '-':
                    currentState.ChangedDir = true;
                    RotateAxes(- GetParameter(s, index) * Randomness(0.5f, 1.5f), ref nextState.Heading, 
                        ref nextState.Left, nextState.Up);
                    break;
                case '^':
                    currentState.ChangedDir = true;
                    RotateAxes(GetParameter(s, index) * Randomness(0.9f, 1f), ref nextState.Heading, 
                        ref nextState.Up, nextState.Left);
                    break;
                case '&':
                    currentState.ChangedDir = true;
                    RotateAxes(- GetParameter(s, index) * Randomness(0.9f, 1f), ref nextState.Heading, 
                        ref nextState.Up, nextState.Left);
                    break;
                case '/':
                    RotateAxes(GetParameter(s, index) * Randomness(0.75f, 1.2f), ref nextState.Up, 
                        ref nextState.Left, nextState.Heading);
                    break;
                case '\\':
                    RotateAxes(- GetParameter(s, index) * Randomness(0.75f, 1.2f), ref nextState.Up, 
                        ref nextState.Left, nextState.Heading);
                    break;
                case '$':
                    RotateAxes(Vector2.Angle(new Vector2(nextState.Left.x, nextState.Left.y), Vector2.left) , 
                        ref nextState.Up, ref nextState.Left, nextState.Heading);

                    break;
                case '!':
                    if (Math.Abs(currentState.Width) < 0.00000001f)
                    {
                        currentState.Width = GetParameter(s, index) / 50;
                    }
                    nextState.Width = GetParameter(s, index) / 50;
                    break;
            }
        }
    }

    public void Redraw() {
        foreach (var tree in Trees) {
            foreach (Transform child in tree.transform)
            {
                Destroy(child.gameObject);
            }
        }
        
        ResetParams();
        foreach (var slider in Sliders) {
            parameters[slider.name] = slider.GetComponent<Slider>().value.ToString();
        }
        currentState = new State() { Heading = Vector3.up, Up = Vector3.back, Left = Vector3.left, Width = 0,
            Position = Vector3.zero, ChangedDir = false };
        nextState = currentState.Clone();
        states.Clear();
        var sentence = Axiom;
        for (var i = 0; i < Iterations; i++)
        {
            sentence = Replace(sentence);
            Debug.Log(sentence);
        }
        Draw(sentence);
    }

    private float MinBranchWidth(int lod) {
        switch (lod) {
            case 1:
                return 0.0015f;
            case 2:
                return 0.00125f;
            case 3:
                return 0.001f;
            case 4:
                return 0.00075f;
            case 5:
                return 0.0005f;
            case 6:
                return 0.00025f;
            default:
                return 0f;
        }
    }

    private void MoveForward(string s, int index) {
        var meshLength = GetParameter(s, index)*Randomness(0.8f, 1.2f);
        var head = Quaternion.AngleAxis(Randomness(-0.07f, 0.07f)/nextState.Width, nextState.Up)*nextState.Heading;
        head = Quaternion.AngleAxis(Randomness(-0.07f, 0.07f)/nextState.Width, nextState.Left)*head;
        var savedPosition = currentState.Position;
        nextState.Position = currentState.Position +
                             head*meshLength;

        State savedState = null;
        if (currentState.ChangedDir) {
            savedState = currentState.Clone();
            currentState = nextState.Clone();
            currentState.Position = savedPosition;
        }
        for (int i = 0; i < Trees.Count; i++) {
            var tree = Trees[i];
            if (currentState.Width < MinBranchWidth(i + 1))
            {
                continue;
            }
            var plane = new GameObject("Branch");
            plane.transform.SetParent(tree.transform);
            var meshFilter = (MeshFilter) plane.AddComponent(typeof(MeshFilter));
            meshFilter.mesh = CreateMesh(index == 0, index + 1 >= s.Length || s[index + 1] == ']');
            if (currentState.Width < 0.005f) {
                BuildLeaves(plane, i + 1);
            }
            var renderer = plane.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
            renderer.material.shader = Shader.Find("Diffuse");
            Tex.Apply();
            renderer.material.mainTexture = Tex;
        }
        if (currentState.ChangedDir)
            {
                if (savedState != null) currentState = savedState.Clone();
            }
            currentState = nextState.Clone();
            currentState.ChangedDir = false;
           
    }

    private int MaxLeaves(int lod) {
        return lod + 3;
    }

    private float MinOffset(int lod) {
        switch (lod)
        {
            case 1:
                return 0.11f;
            case 2:
                return 0.09f;
            case 3:
                return 0.07f;
            case 4:
                return 0.065f;
            case 5:
                return 0.06f;
            case 6:
                return 0.055f;
            default:
                return 0.05f;
        }
    }

    private float MaxOffset(int lod)
    {
        switch (lod)
        {
            case 1:
                return 0.4f;
            case 2:
                return 0.35f;
            case 3:
                return 0.3f;
            case 4:
                return 0.25f;
            case 5:
                return 0.2f;
            case 6:
                return 0.15f;
            default:
                return 0.075f;
        }
    }

    private void BuildLeaves(GameObject branch, int lod) {
        var maxleaves = MaxLeaves(lod);
        var minOffset = MinOffset(lod);
        var maxOffset = MaxOffset(lod);
        var baseOffset =  0.0000005f;
        var leafState = currentState.Clone();
        var offset = baseOffset/(currentState.Width*currentState.Width);
        if (offset < minOffset) {
            offset = minOffset;
        }
        if (offset > maxOffset) {
            offset = maxOffset;
        }
        var direction = nextState.Position - leafState.Position;
        var j = 0;
        while (direction.magnitude > offset && j< maxleaves) {
            j++;
            for (var i = 0; i < 8; i++) {
                var leaf = new GameObject("Leaf");
                leaf.transform.SetParent(branch.transform);
                var meshFilter = (MeshFilter)leaf.AddComponent(typeof(MeshFilter));
                meshFilter.mesh = CreateLeaf(leafState, lod);
                var renderer = leaf.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
                renderer.material.shader = Shader.Find("Diffuse");
                LeafText.Apply();
                renderer.material.mainTexture = LeafText;
                RotateAxes( 45 * Randomness(0.75f, 1.2f), ref leafState.Up, ref leafState.Left, direction);
            }
            leafState.Position += direction.normalized*offset;
            direction = nextState.Position - leafState.Position;
        }
    }

    private string[] getParamsFromString(string s, int i, out int end)
    {
        end = s.IndexOf(')', i + 1);
        var ruleParameters = s.Substring(i + 2, end - i - 2).Split(',');
        return ruleParameters;
    }

    private string ReplaceParams(string s)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < s.Length; i++)
        {
            if (s[i] == '(')
            {
                int formalEnd;
                var formalParams = getParamsFromString(s, i - 1, out formalEnd);
                var actualParams = new string[formalParams.Length];
                for (var j = 0; j < formalParams.Length; j++)
                {
                    var multipliers = formalParams[j].Split('*');
                    if (multipliers.Length > 1)
                    {
                        var f = multipliers.Aggregate<string, float>(1, (current, m) => current * float.Parse(parameters[m]));
                        actualParams[j] = f.ToString();
                    }
                    else
                    {
                        actualParams[j] = parameters[formalParams[j]];
                    }

                }
                sb.Append('(');
                for (var j = 0; j < actualParams.Length; j++)
                {
                    sb.Append(actualParams[j]);
                    if (j != actualParams.Length - 1)
                    {
                        sb.Append(',');
                    }
                }
                sb.Append(')');
                i = formalEnd;
            }
            else
            {
                sb.Append(s[i]);
            }
        }
        return sb.ToString();
    }

    public string Replace(string s)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < s.Length; i++)
        {
            var rule = rules.FirstOrDefault(x => x.Key.StartsWith(s[i].ToString()));
            if (rule.Key != null)
            {
                if ((i + 1) < s.Length && s[i + 1] == '(')
                {
                    int actualEnd, formalEnd;
                    var actualParams = getParamsFromString(s, i, out actualEnd);
                    var formalParams = getParamsFromString(rule.Key, 0, out formalEnd);
                    for (var j = 0; j < formalParams.Length; j++)
                    {
                        if (parameters.ContainsKey(formalParams[j]))
                        {
                            parameters[formalParams[j]] = actualParams[j];
                        }
                        else
                        {
                            parameters.Add(formalParams[j], actualParams[j]);
                        }
                    }
                    var ruleValue = ReplaceParams(rules[rule.Key]);
                    sb.Append(ruleValue);
                    i = actualEnd;
                }
                else
                {
                    sb.Append(rules[rule.Key]);
                }
            }
            else
            {
                sb.Append(s[i]);
            }
        }
        return sb.ToString();
    }
}
