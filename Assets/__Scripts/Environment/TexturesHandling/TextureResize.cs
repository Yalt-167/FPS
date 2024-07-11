using UnityEngine;
using System.Collections;
using UnityEditor;

[ExecuteInEditMode]
public class TextureResize : MonoBehaviour
{

    public float scaleFactor = 5.0f;
    Material mat;

    private void Start()
    {
        UpdateTextureTiling();
    }

    
    private void Update()
    {
        if (transform.hasChanged)
        {
            UpdateTextureTiling();
            transform.hasChanged = false;
        }
    }

    public void UpdateTextureTiling()
    {
        GetComponent<Renderer>().material.mainTextureScale = new Vector2(transform.localScale.x / scaleFactor, transform.localScale.z / scaleFactor);
    }
}

[CustomEditor(typeof(TextureResize))]
public class TextureResizeEditorButton : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if (GUILayout.Button("UpdateTexture"))
        {
            TextureResize targetScript = (TextureResize)target;
            targetScript.UpdateTextureTiling();
        }
    }
}