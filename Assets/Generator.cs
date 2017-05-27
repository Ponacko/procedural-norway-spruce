using System;
using UnityEngine;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.UI;

public class Generator : MonoBehaviour
{
    private class State
    {
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

    [Serializable]
    public struct Rule
    {
        public string Pred;
        public string Succ;
    }

    [Serializable]
    public struct Parameter
    {
        public string Name;
        public string Value;
    }

    public string Axiom;
    public List<GameObject> Sliders = new List<GameObject>();
    public GameObject countSlider;
    public Rule[] RulesArray;
    public Parameter[] ParameterArray;
    public int Iterations = 1;
    public GameObject LodPrefab;
    public Texture2D Tex;
    public Texture2D LeafTexture;
    public Dictionary<int, Vector2[]> UvsDictionary = new Dictionary<int, Vector2[]>();

    private List<GameObject> treeSets = new List<GameObject>();
    private State currentState = new State() { Heading = Vector3.up, Up = Vector3.back,
        Left = Vector3.left,  Width = 0,  Position = Vector3.zero, ChangedDir = false};
    private State nextState;
    private Dictionary<string, string> rules = new Dictionary<string, string>();
    private Dictionary<string, string> parameters = new Dictionary<string, string>();
    private Stack<State> states = new Stack<State>();
    private System.Random r = new System.Random();
    private string sentence;


    /// <summary>
    /// Resets all rules and parameters to their default values.
    /// </summary>
    private void ResetRulesAndParams()
    {
        foreach (var c in RulesArray)
        {
            if (!rules.ContainsKey(c.Pred))
            {
                rules.Add(c.Pred, c.Succ);
            }
            else
            {
                rules[c.Pred] = c.Succ;
            }
        }
        foreach (var c in ParameterArray)
        {
            if (!parameters.ContainsKey(c.Name))
            {
                parameters.Add(c.Name, c.Value);
            }
            else
            {
                parameters[c.Name] = c.Value;
            }
        }
    }
    
    /// <summary>
    /// Get all parameters from a given string starting at a given index and finishing at the index of ')' symbol.
    /// </summary>
    /// <param name="s">The sentence which is searched.</param>
    /// <param name="i">Index where the search starts.</param>
    /// <param name="end">Output index which determines where the parameters end.</param>
    /// <returns>An array of parameters.</returns>
    private string[] GetParamsFromString(string s, int i, out int end)
    {
        end = s.IndexOf(')', i + 1);
        var ruleParameters = s.Substring(i + 2, end - i - 2).Split(',');
        return ruleParameters;
    }

    /// <summary>
    /// Replaces all parameters in a sentence with their values
    /// </summary>
    /// <param name="s">The sentence to be processed.</param>
    /// <returns>The sentence with the parameters replaced.</returns>
    private string ReplaceParams(string s)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < s.Length; i++)
        {
            if (s[i] == '(')
            {
                int formalEnd;
                var formalParams = GetParamsFromString(s, i - 1, out formalEnd);
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

    /// <summary>
    /// Replace all symbols in the sentence according to the rules and formal parameters with their values.
    /// </summary>
    /// <param name="s">The sentence to be processed.</param>
    /// <returns>The sentence with the symbols replaced.</returns>
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
                    var actualParams = GetParamsFromString(s, i, out actualEnd);
                    var formalParams = GetParamsFromString(rule.Key, 0, out formalEnd);
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


    /// <summary>
    /// Returns the appropriate value of minimum branch width to be generated (branches thinner than this value are skipped in the generation)
    /// for the chosen level of detail.
    /// </summary>
    /// <param name="lod">Level of detail of the tree being generated.</param>
    /// <returns>Width of the thinnest branch that is generated.</returns>
    private float MinBranchWidth(int lod)
    {
        switch (lod)
        {
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

    /// <summary>
    /// Returns the appropriate number of sides for a branch of given level of detail.
    /// </summary>
    /// <param name="lod">Level of detail of the tree being generated.</param>
    /// <returns>The number of sides.</returns>
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

    /// <summary>
    /// Returns the maximum number of leaves per branch to be generated for a given level of detail.
    /// </summary>
    /// <param name="lod">Level of detail of the tree being generated.</param>
    /// <returns>The maximum number of leaves per branch.</returns>
    private int MaxLeaves(int lod)
    {
        return (lod < 3) ? lod : lod - 1;
    }

    /// <summary>
    /// Returns the minimum offset between the generated leaves for a given level of detail.
    /// </summary>
    /// <param name="lod">Level of detail of the tree being generated.</param>
    /// <returns>The minimum offset between the generated leaves.</returns>
    private float MinOffset(int lod)
    {
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

    /// <summary>
    /// Returns the maximum offset between the generated leaves for a given level of detail.
    /// </summary>
    /// <param name="lod">Level of detail of the tree being generated.</param>
    /// <returns>The maximum offset between the generated leaves.</returns>
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

    /// <summary>
    /// Generates a pseudo-random number in the given range.
    /// </summary>
    /// <param name="a">Lower bound of the range.</param>
    /// <param name="b">Upper bound of the range.</param>
    /// <returns>The generated random number.</returns>
    private float Randomness(float a, float b)
    {
        return (float)(a + r.NextDouble() * (b - a));

    }

    /// <summary>
    /// Extracts parameter from the sentence for the symbol at given index.
    /// </summary>
    /// <param name="s">The sentence in which parameter is found.</param>
    /// <param name="i">Index of the symbol in the sentence for which a parameter is found.</param>
    /// <returns>The parameter value if available, else 0.</returns>
    private float GetParameter(string s, int i)
    {
        if (s[i + 1] == '(')
        {
            var end = s.IndexOf(')', i + 1);
            return float.Parse(s.Substring(i + 2, end - i - 2));
        }
        return 0;
    }

    /// <summary>
    /// Rotates two vectors against the third.
    /// </summary>
    /// <param name="angle">Angle of the rotation.</param>
    /// <param name="first">First vector to be rotated.</param>
    /// <param name="second">Second vector to be rotated.</param>
    /// <param name="around">Vector around which the rotation is performed.</param>
    private void RotateVectors(float angle, ref Vector3 first, ref Vector3 second, Vector3 around)
    {
        first = Quaternion.AngleAxis(angle, around) * first;
        second = Quaternion.AngleAxis(angle, around) * second;
    }

    /// <summary>
    /// Creates a list of vertices at the position of the given state and at the state's rotation. The amount of created vertices depends on the number of sides.
    /// </summary>
    /// <param name="state">The state that determines the position of the vertices.</param>
    /// <param name="sides">Determines how many sides should the generated polygon have.</param>
    /// <returns>Created vertices as a list.</returns>
    private List<Vector3> MakeVertices(State state, int sides)
    {
        int angleOffset = 360 / sides;
        var vertices = new List<Vector3> { state.Position };
        for (var angle = 0; angle < 360; angle += angleOffset)
        {
            vertices.Add(state.Position + Quaternion.AngleAxis(angle, state.Heading) * state.Up * state.Width);
        }
        return vertices;
    }

    /// <summary>
    /// Generates uvs for a chosen number of sides.
    /// </summary>
    /// <param name="sides">Number of sides of the segment being generated.</param>
    /// <returns>An array of calcuated uvs.</returns>
    private Vector2[] MakeUVs(int sides)
    {
        float offset = 1f / sides;
        Vector2[] uvs = new Vector2[(sides + 1) * 2];
        int k = 0;
        for (int i = 0; i <= 1f; i++)
        {
            for (float j = 0f; j < 1f; j += offset)
            {
                uvs[k] = new Vector2(i, j);
                k++;
            }
            uvs[k] = new Vector2(i, 1);
        }
        return uvs;
    }

    /// <summary>
    /// Generates triangles for a polygon with chosen number of sides.
    /// </summary>
    /// <param name="hasBot">Determines if the polygon needs a bottom face.</param>
    /// <param name="hasTop">Determines if the polygon needs a top face.</param>
    /// <param name="sides">The number of sides of the polygon.</param>
    /// <returns></returns>
    private static List<int> MakeTris(bool hasBot, bool hasTop, int sides)
    {
        var tris = new List<int>();
        for (int i = 1; i <= sides; i++)
        {
            tris.AddRange(new int[] {
                sides + 1 + i, i, i%sides +1,
                sides + 1 + i, i%sides + 1, sides + 2 + i%sides
            });
            if (hasBot)
            {
                tris.AddRange(new int[] {
                    0, i%sides + 1, i
                });
            }
            if (hasTop)
            {
                tris.AddRange(new int[] {
                    sides + 1, sides + i + 1, sides + i%sides + 2
                });
            }
        }
        return tris;
    }

    /// <summary>
    /// Generates vertices, uvs and triangles of a leaf and adds them to the tree object, which instantiates them later. 
    /// The position and rotation of the leaf is specified by the state parameter, while the shape is specified by the lod parameter 
    /// (higher LOD = more complex shape).
    /// </summary>
    /// <param name="state">State where the leaf should be created.</param>
    /// <param name="lod">Level of detail fo the leaf.</param>
    /// <param name="tree">The tree to which the leaf belongs.</param>
    private void CreateLeaf(State state, int lod, Spruce tree) {
        var offset = currentState.Width;
        var length = currentState.Width*30;
        Vector3[] vertices;
        Vector2[] uv;
        int[] triangles;
        // If lod is lower then 6, the simpler shape is chosen. If it is >= 6 a more complex one is chosen.
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
        // If the tree object has reached the maximum number of vertices for leaves, a mesh of leaves is instantiated.
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
    
    /// <summary>
    /// Creates vertices, uvs and triangles for a branch and adds it to the tree object, which handles the intantiating of the mesh.
    /// </summary>
    /// <param name="hasBot">Determines if the branch needs a bottom face.</param>
    /// <param name="hasTop">Determines if the branch needs a top face.</param>
    /// <param name="sides">Determines how many sides should the resulting polygon have.</param>
    /// <param name="tree">The tree to which this branch belongs.</param>
    private void CreateBranch(bool hasBot, bool hasTop, int sides, Spruce tree)
    {
        var vertices = MakeVertices(currentState, sides).Concat(MakeVertices(nextState, sides)).ToArray();

        // If the tree object has reached the maximum number of vertices for leaves, a mesh of leaves is instantiated.
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

    /// <summary>
    /// Creates leaves on the branch being generated.
    /// </summary>
    /// <param name="tree">Tree to which the branch belongs.</param>
    /// <param name="lod">Level of detail of the leaves.</param>
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
                RotateVectors( 45 * Randomness(0.75f, 1.2f), ref leafState.Up, ref leafState.Left, direction);
            }
            leafState.Position += direction.normalized*offset;
            direction = nextState.Position - leafState.Position;
        }
    }

    /// <summary>
    /// Creates a branch by moving forward from the current state to the next state. Calculates the branch's vertices, uvs and triangles and if the branch's width
    /// is in specified range, generates leaves on this branch.
    /// </summary>
    /// <param name="s">The sentence being processed.</param>
    /// <param name="index">Index of the sentence being processed.</param>
    /// <param name="treeSet">The tree set to which the branch belongs.</param>
    private void MoveForward(string s, int index, GameObject treeSet)
    {
        var meshLength = GetParameter(s, index) * Randomness(0.8f, 1.2f);
        var head = Quaternion.AngleAxis(Randomness(-0.07f, 0.07f) / nextState.Width, nextState.Up) * nextState.Heading;
        head = Quaternion.AngleAxis(Randomness(-0.07f, 0.07f) / nextState.Width, nextState.Left) * head;
        var savedPosition = currentState.Position;
        nextState.Position = currentState.Position +
                             head * meshLength;
        State savedState = null;
        if (currentState.ChangedDir)
        {
            savedState = currentState.Clone();
            currentState = nextState.Clone();
            currentState.Position = savedPosition;
        }
        int i = 0;
        foreach (Transform tree in treeSet.transform)
        {
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
            CreateBranch(index == 0, index + 1 >= s.Length || s[index + 1] == ']', sides, spruce);
            if (index == s.LastIndexOf('F'))
            {
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

    /// <summary>
    /// Extracts values from the sliders in the GUI, assigns them to the parameters, destroys all trees and generates them again.
    /// </summary>
    public void Redraw()
    {
        ResetRulesAndParams();
        foreach (var slider in Sliders)
        {
            parameters[slider.name] = slider.GetComponent<Slider>().value.ToString();
        }
        int numberOfTrees = (int)countSlider.GetComponent<Slider>().value;
        // Destroys all tree game objects in the scene.
        var trees = FindObjectsOfType<Lod>();
        foreach (var tree in trees)
        {
            Destroy(tree.gameObject);
        }
        // Resets the states and the stack of states
        currentState = new State()
        {
            Heading = Vector3.up,
            Up = Vector3.back,
            Left = Vector3.left,
            Width = 0,
            Position = Vector3.zero,
            ChangedDir = false
        };
        nextState = currentState.Clone();
        states.Clear();
        Generate(numberOfTrees);
    }

    /// <summary>
    /// Processes the sentence and does appropriate drawing operations for each symbol. The drawing is performed for a specific treeSet.
    /// </summary>
    /// <param name="s">The sentence to be processed.</param>
    /// <param name="treeSet">The tree set being generated.</param>
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
                    currentState.Position += currentState.Heading * GetParameter(s, index);
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
                    RotateVectors(GetParameter(s, index) * Randomness(0.5f, 1.5f), ref nextState.Heading,
                        ref nextState.Left, nextState.Up);
                    break;

                case '-':
                    currentState.ChangedDir = true;
                    RotateVectors(-GetParameter(s, index) * Randomness(0.5f, 1.5f), ref nextState.Heading,
                        ref nextState.Left, nextState.Up);
                    break;
                case '^':
                    currentState.ChangedDir = true;
                    RotateVectors(GetParameter(s, index) * Randomness(0.9f, 1f), ref nextState.Heading,
                        ref nextState.Up, nextState.Left);
                    break;
                case '&':
                    currentState.ChangedDir = true;
                    RotateVectors(-GetParameter(s, index) * Randomness(0.9f, 1f), ref nextState.Heading,
                        ref nextState.Up, nextState.Left);
                    break;
                case '/':
                    RotateVectors(GetParameter(s, index) * Randomness(0.75f, 1.2f), ref nextState.Up,
                        ref nextState.Left, nextState.Heading);
                    break;
                case '\\':
                    RotateVectors(-GetParameter(s, index) * Randomness(0.75f, 1.2f), ref nextState.Up,
                        ref nextState.Left, nextState.Heading);
                    break;
                case '$':
                    RotateVectors(Vector2.Angle(new Vector2(nextState.Left.x, nextState.Left.y), Vector2.left),
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

    /// <summary>
    /// Generates a grid of trees with both height and width equal to the chosen amount.
    /// </summary>
    /// <param name="numberOfTrees">Number of trees in a row/column</param>
    private void Generate(int numberOfTrees)
    {
        treeSets.Clear();
        // Instantiates numberOfTrees^2 Lod game objects and adds them to the tree set
        for (int i = 0; i < numberOfTrees; i++)
        {
            for (int j = 0; j < numberOfTrees; j++)
            {
                var vector3 = new Vector3(r.Next(i * 5, (i + 1) * 5), 0, r.Next(j * 5, (j + 1) * 5));
                var treeSet = Instantiate(LodPrefab, vector3, Quaternion.identity);
                treeSets.Add(treeSet);
            }
        }
        // Executes the rewriting process starting from the axiom, replacing every symbol according to the rules
        sentence = Axiom;
        for (var i = 0; i < Iterations; i++)
        {
            sentence = Replace(sentence);
        }

        // Draws all the trees
        foreach (var treeSet in treeSets)
        {
            // Resets current and the next state
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

    /// <summary>
    /// Called before the scene is loaded.
    /// </summary>
    private void Awake()
    {
        nextState = currentState.Clone();
        ResetRulesAndParams();
        // Pre-calculates uvs as an optimization
        for (int i = 4; i < 9; i += 2)
        {
            UvsDictionary.Add(i, MakeUVs(i));
        }
        // Generates 10^2 trees
        Generate(10);
    }
}
