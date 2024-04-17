using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class MeshFromSkinnedMesh : MonoBehaviour
{

    [SerializeField] private VisualEffect VFXgraph;
    [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;
    [SerializeField] private float refreshRate;
    private WaitForSeconds refreshRate_;
    IEnumerator Start()
    {
        refreshRate_ = new(refreshRate);

        Mesh mesh;
        Mesh validMesh = new(); // seems dumb but Gabriel Aguiar said it fixed an issue so that stays ig
        while (gameObject.activeSelf)
        {
            mesh = new();
            skinnedMeshRenderer.BakeMesh(mesh);
            var vertices = mesh.vertices;
            validMesh.vertices = vertices;
            VFXgraph.SetMesh("mesh", validMesh);

            yield return refreshRate_;
        }
    }
}
