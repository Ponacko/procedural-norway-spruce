using System.Collections.Generic;
using UnityEngine;

public class Spruce : MonoBehaviour {

    public const int MaxVertices = 65535;
    public int CurrentVertices;
    public List<Vector3> Vertices = new List<Vector3>();
    public List<Vector2> Uvs = new List<Vector2>();
    public List<int> Tris = new List<int>();
    public int LeafCurrentVertices;
    public List<Vector3> LeafVertices = new List<Vector3>();
    public List<Vector2> LeafUvs = new List<Vector2>();
    public List<int> LeafTris = new List<int>();
    public GameObject MeshPartPrefab;
    public Texture2D Tex;
    public Texture2D LeafTex;

    /// <summary>
    /// Creates a new mesh using the vertices, uvs and triangles in the attributes of this class. 
    /// Instantiates a new game object and assigns this mesh and texture to it. Clears lists of vertices, uvs and triangles.
    /// </summary>
    public void Instantiate() {
        var part = Instantiate(MeshPartPrefab, Vector3.zero, transform.rotation, transform);
        part.name = "Branches";
        var mesh = new Mesh() {
            vertices = Vertices.ToArray(),
            uv = Uvs.ToArray(),
            triangles = Tris.ToArray()
        };
        mesh.RecalculateNormals();
        part.GetComponent<MeshFilter>().mesh = mesh;
        var renderer = part.GetComponent<MeshRenderer>();
        renderer.material.shader = Shader.Find("Diffuse");
        Tex.Apply();
        renderer.material.mainTexture = Tex;
        Vertices = new List<Vector3>();
        Uvs = new List<Vector2>();
        Tris = new List<int>();
        CurrentVertices = 0;
    }

    /// <summary>
    /// Creates a new mesh using the vertices, uvs and triangles for the leaves in the attributes of this class. 
    /// Instantiates a new game object and assigns this mesh and texture to it. Clears lists of vertices, uvs and triangles for the leaves.
    /// </summary>
    public void InstatiateLeaf() {
        var leaf = Instantiate(MeshPartPrefab, Vector3.zero, transform.rotation, transform);
        leaf.name = "Branches";
        var mesh = new Mesh()
        {
            vertices = LeafVertices.ToArray(),
            uv = LeafUvs.ToArray(),
            triangles = LeafTris.ToArray()
        };
        mesh.RecalculateNormals();
        leaf.GetComponent<MeshFilter>().mesh = mesh;
        var renderer = leaf.GetComponent<MeshRenderer>();
        renderer.material.shader = Shader.Find("Diffuse");
        LeafTex.Apply();
        renderer.material.mainTexture = LeafTex;
        LeafVertices = new List<Vector3>();
        LeafUvs = new List<Vector2>();
        LeafTris = new List<int>();
        LeafCurrentVertices = 0;
    }
}
