using UnityEngine;

public class TextMeshOutlineSyncer : MonoBehaviour
{
    private TextMesh mainTextMesh;
    private TextMesh outlineTextMesh;

    public void Initialize(TextMesh main, TextMesh outline)
    {
        mainTextMesh = main;
        outlineTextMesh = outline;
    }

    private void LateUpdate()
    {
        if (mainTextMesh != null && outlineTextMesh != null)
        {
            outlineTextMesh.text = mainTextMesh.text;
        }
    }
}
