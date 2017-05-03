using System;
using UnityEngine;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine.UI;
using Tree = UnityEngine.Tree;

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
    public GameObject countSlider;

    public Rule[] RulesArray;
    public Parameter[] ParameterArray;
    public int Iterations = 1;
    public GameObject LodPrefab;
    public Texture2D Tex;
    public Texture2D LeafTexture;
    public Dictionary<int, Vector2[]> UvsDictionary = new Dictionary<int, Vector2[]>();

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

    private List<GameObject> treeSets = new List<GameObject>();
    private State currentState = new State() { Heading = Vector3.up, Up = Vector3.back,
        Left = Vector3.left,  Width = 0,  Position = Vector3.zero, ChangedDir = false};
    private State nextState;
    private Dictionary<string, string> rules = new Dictionary<string, string>();
    private Dictionary<string, string> parameters = new Dictionary<string, string>();
    private Stack<State> states = new Stack<State>();
    private System.Random r = new System.Random();
    private string sentence;
    private float progress;

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

    private void Awake()
    {
        nextState = currentState.Clone();
        ResetParams();
        for (int i = 4; i < 9; i += 2)
        {
            UvsDictionary.Add(i, MakeUVs(i));
        }

        Generate(10);
    }

    private void Start() {
        

    }

    private void Generate (int numberOfTrees) {
        treeSets.Clear();
        for (int i = 0; i < numberOfTrees; i++)
        {
            for (int j = 0; j < numberOfTrees; j++)
            {
                var vector3 = new Vector3(r.Next(i * 5, (i + 1) * 5), 0, r.Next(j * 5, (j + 1) * 5));
                var treeSet = Instantiate(LodPrefab, vector3, Quaternion.identity);
                treeSets.Add(treeSet);
            }
        }
        sentence = Axiom;
        for (var i = 0; i < Iterations; i++)
        {
            sentence = Replace(sentence);
            //Debug.Log(sentence);
        }
        
        foreach (var treeSet in treeSets)
        {
            currentState = new State()
            {
                Heading = Vector3.up,
                Up = Vector3.back,
                Left = Vector3.left,
                Width = 0,
                Position = treeSet.transform.position,
                ChangedDir = false
            };
            nextState = currentState.Clone();
            Draw(sentence, treeSet);
        }
    }

    private List<Vector3> MakeVertices(State state, int sides) {
        int angleOffset = 360/sides;
        var vertices = new List<Vector3> {state.Position};
        for (var angle = 0; angle < 360; angle += angleOffset) {
            vertices.Add(state.Position + Quaternion.AngleAxis(angle, state.Heading) * state.Up * state.Width);
        }
        return vertices;
    }

    private void CreateLeaf(State state, int lod, Spruce tree) {
        var offset = currentState.Width;
        var length = currentState.Width*30;
        Vector3[] vertices;
        Vector2[] uv;
        int[] triangles;
        if (lod < 6) {
            vertices = new Vector3[] {
                state.Position - offset*state.Heading - offset*state.Left,
                state.Position - offset*state.Heading + offset*state.Left,
                state.Position + offset*state.Heading,
                state.Position + state.Up*length*1.3f + state.Heading*65*offset
            };
            uv = new Vector2[] {
                new Vector2(0, 0),
                new Vector2(0, 1),
                new Vector2(1, 0),
                new Vector2(1, 1),
            };
            triangles = new[] {
                3, 1, 0,
                3, 2, 1,
                3, 0, 2
            };
        }
        else {
            vertices = new Vector3[] {
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
            uv = new Vector2[] {
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
            triangles = new[] {
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
        if (tree.LeafCurrentVertices + vertices.Length >= Spruce.MaxVertices)
        {
            tree.InstatiateLeaf();
        }
        tree.LeafVertices.AddRange(vertices);
        tree.LeafUvs.AddRange(uv);
        for (int index = 0; index < triangles.Length; index++)
        {
            triangles[index] += tree.LeafCurrentVertices;
        }
        tree.LeafTris.AddRange(triangles);
        tree.LeafCurrentVertices += vertices.Length;
        
    }

    private Vector2[] MakeUVs(int sides) {
        float offset = 1f/sides;
        Vector2[] uvs = new Vector2[(sides+1) * 2];
        int k = 0;
        for (int i = 0; i <= 1f; i++) {
            for (float j = 0f; j < 1f; j+= offset) {
                uvs[k] = new Vector2(i, j);
                k++;
            }
            uvs[k] = new Vector2(i, 1);
        }
        return uvs;
    }

    private void CreateMesh(bool hasBot, bool hasTop, int sides, Spruce tree)
    {
        var vertices = MakeVertices(currentState, sides).Concat(MakeVertices(nextState, sides)).ToArray();
        if (tree.CurrentVertices + vertices.Length >= Spruce.MaxVertices) {
            tree.Instantiate();
        }
        tree.Vertices.AddRange(vertices);
        tree.Uvs.AddRange(MakeUVs(sides));
        var tris = MakeTris(hasBot, hasTop, sides);
        for (int index = 0; index < tris.Count; index++) {
            tris[index] += tree.CurrentVertices;
        }
        tree.Tris.AddRange(tris);
        tree.CurrentVertices += vertices.Length;
    }

    private static List<int> MakeTris(bool hasBot, bool hasTop, int sides)
    {
        var tris = new List<int>();
        for (int i = 1; i <= sides; i++) {
            tris.AddRange(new int[] {
                sides + 1 + i, i, i%sides +1,
                sides + 1 + i, i%sides + 1, sides + 2 + i%sides
            });
            if (hasBot) {
                tris.AddRange(new int[] {
                    0, i%sides + 1, i
                });
            }
            if (hasTop) {
                tris.AddRange(new int[] {
                    sides + 1, sides + i + 1, sides + i%sides + 2
                });
            }
        }
        return tris;
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

    public void Draw(string s, GameObject treeSet)
    {
        for (var index = 0; index < s.Length; index++)
        {
            var c = s[index];
            switch (c)
            {
                case 'F':
                    MoveForward(s, index, treeSet);
                    break;
                case 'L':
                    MoveForward(s, index, treeSet);
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
      
        ResetParams();
        foreach (var slider in Sliders) {
            parameters[slider.name] = slider.GetComponent<Slider>().value.ToString();
        }
         int numberOfTrees = (int)countSlider.GetComponent<Slider>().value;
        var trees = FindObjectsOfType<Lod>();
        foreach (var tree in trees) {
            Destroy(tree.gameObject);
        }
        currentState = new State() { Heading = Vector3.up, Up = Vector3.back, Left = Vector3.left, Width = 0,
            Position = Vector3.zero, ChangedDir = false };
        nextState = currentState.Clone();
        states.Clear();
        Generate(numberOfTrees);
    }

    private float MinBranchWidth(int lod) {
        switch (lod) {
            case 1:
                return 0.00175f;
            case 2:
                return 0.0015f;
            case 3:
                return 0.0013f;
            case 4:
                return 0.0012f;
            case 5:
                return 0.001f;
            case 6:
                return 0.00075f;
            default:
                return 0.00075f;
            
        }
    }

    private void MoveForward(string s, int index, GameObject treeSet) {
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
        int i = 0;
        foreach (Transform tree in treeSet.transform) {
            i++;
            if (currentState.Width < MinBranchWidth(i))
            {
                continue;
            }
            int sides = Sides(i);
            if (currentState.Width < MinBranchWidth(i) * 10)
            {
                sides = 4;
            }
            var spruce = tree.GetComponent<Spruce>();
            CreateMesh(index == 0, index + 1 >= s.Length || s[index + 1] == ']', sides, spruce);
            if (index == s.LastIndexOf('F')) {
                spruce.Instantiate();
                spruce.InstatiateLeaf();
            }
            if (currentState.Width < 0.005f)
            {
                BuildLeaves(tree, i);
            }
        }
        if (currentState.ChangedDir)
        {
            if (savedState != null) currentState = savedState.Clone();
        }
        currentState = nextState.Clone();
        currentState.ChangedDir = false;
    }

    private int Sides(int lod)
    {
        switch (lod)
        {
            case 5:
                return 6;
            case 6:
                return 8;
            default:
                return 4;
        }
    }

    private int MaxLeaves(int lod) {
        return (lod < 3) ? lod : lod-1;
    }

    private float MinOffset(int lod) {
        switch (lod)
        {
            case 1:
                return 0.2f;
            case 2:
                return 0.15f;
            case 3:
                return 0.11f;
            case 4:
                return 0.09f;
            case 5:
                return 0.07f;
            case 6:
                return 0.065f;
            default:
                return 0.06f;
        }
    }

    private float MaxOffset(int lod)
    {
        switch (lod)
        {
            case 1:
                return 0.5f;
            case 2:
                return 0.45f;
            case 3:
                return 0.4f;
            case 4:
                return 0.35f;
            case 5:
                return 0.3f;
            case 6:
                return 0.25f;
            default:
                return 0.2f;
        }
    }

    private void BuildLeaves(Transform tree, int lod) {
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
                CreateLeaf(leafState, lod, tree.GetComponent<Spruce>());
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
