using UnityEngine;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;

public class LSystemGenerator : MonoBehaviour
{

    [Serializable]
    class State
    {
        public float size;
        public float angle;
        public float x;
        public float y;
        public float dir;

        public State Clone()
        {
            return (State)this.MemberwiseClone();
        }
    }

    [Serializable]
    class Node
    {
        public int x, y;
        public bool isStreet;

        public Node(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }

    public string input = "LSYG";
    public float sizeValue = 15f;
    public float sizeGrowth = -1.5f;
    public float angleValue = 90f;
    public float angleGrowth = 0f;
    public Dictionary<char, string> rules = new Dictionary<char, string>();

    public int width, height = 80;

    public GameObject custom;

    private Node[,] nodes;
    private State state;
    private Stack<State> states = new Stack<State>();

    void Awake()
    {
        nodes = new Node[width, height];

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                nodes[x, y] = new Node(x, y);

        rules.Add('F', "F[+F][-F]F");
        /*rules.Add('S', "[F[FF-YS]F)G]+");
        rules.Add('Y', "--[F-)<F-FG]-");
        rules.Add('G', "FGF[Y+>F]+Y");*/
    }

    void Start()
    {
        input = Replace(input);
        Generate();
        Draw();
    }

    public void Draw()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (nodes[x, y].isStreet)
                {
                    GameObject go = (GameObject)Instantiate(custom, new Vector3(x, y, 0), Quaternion.identity);
                    go.transform.parent = this.transform;
                    go.name = "Tile (" + x + "|" + y + ")";
                }
            }
        }
    }

    public void Generate()
    {
        state = new State()
        {
            x =  1,
            y = 1,
            dir = 0,
            size = sizeValue,
            angle = angleValue
        };

        foreach (char c in input)
        {
            switch (c)
            {
                case 'F':
                    float newX = state.x + state.size * Mathf.Cos(state.dir * Mathf.PI / 180);
                    float newY = state.y + state.size * Mathf.Sin(state.dir * Mathf.PI / 180);

                    Debug.Log(state.x + " -" + state.y);
                    //Debug.Log(nodes.GetLength(0) + " " +  nodes.GetLength(1));
                   
                    nodes[Mathf.RoundToInt(state.x), Mathf.RoundToInt(state.y)].isStreet = true;
                    nodes[Mathf.RoundToInt(newX), Mathf.RoundToInt(newY)].isStreet = true;
                    //TODO: draw line

                    state.x = newX;
                    state.y = newY;
                    break;
                case '+':
                    state.dir += state.angle;
                    break;
                case '-':
                    state.dir -= state.angle;
                    break;
                case '>':
                    state.size *= (1 - sizeGrowth);
                    break;
                case '<':
                    state.size *= (1 + sizeGrowth);
                    break;
                case ')':
                    state.angle *= (1 + angleGrowth);
                    break;
                case '(':
                    state.angle *= (1 - angleGrowth);
                    break;
                case '[':
                    states.Push(state.Clone());
                    break;
                case ']':
                    state = states.Pop();
                    break;
                case '!':
                    state.angle *= -1;
                    break;
                case '|':
                    state.dir += 180;
                    break;
            }
        }
    }

    public string Replace(string s)
    {
        StringBuilder sb = new StringBuilder();

        foreach (char c in s)
        {
            if (rules.ContainsKey(c))
            {
                sb.Append(rules[c]);
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

}