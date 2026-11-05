using TMPro;
using UnityEngine;

// Presentation only. The scene places this text in its cube pad's local XZ plane.
[ExecuteAlways]
[RequireComponent(typeof(TextMeshPro))]
public sealed class PurchasePadSurfaceText : MonoBehaviour
{
    // A unit cube pad occupies [-0.5, 0.5]. Reserve 10% on every edge,
    // including SDF quad padding, rather than relying on TMP overflow clipping.
    private const float MeshSize = 0.8f;
    private TextMeshPro label;

    private void OnEnable()
    {
        label = GetComponent<TextMeshPro>();
        label.OnPreRenderText += FitMesh;
        label.SetVerticesDirty();
    }

    private void OnDisable()
    {
        if (label != null) label.OnPreRenderText -= FitMesh;
    }

    private void FitMesh(TMP_TextInfo info)
    {
        Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        int count = 0;
        for (int m = 0; m < info.materialCount; m++)
        {
            TMP_MeshInfo mesh = info.meshInfo[m];
            for (int v = 0; v < mesh.vertexCount; v++)
            {
                Vector2 point = mesh.vertices[v];
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
                count++;
            }
        }
        if (count == 0) return;

        Vector2 center = (min + max) * 0.5f;
        Vector2 size = max - min;
        float scale = Mathf.Min(1f, MeshSize / Mathf.Max(size.x, 0.0001f),
            MeshSize / Mathf.Max(size.y, 0.0001f));
        for (int m = 0; m < info.materialCount; m++)
        {
            TMP_MeshInfo mesh = info.meshInfo[m];
            for (int v = 0; v < mesh.vertexCount; v++)
            {
                Vector3 point = mesh.vertices[v];
                point.x = (point.x - center.x) * scale;
                point.y = (point.y - center.y) * scale;
                mesh.vertices[v] = point;
            }
        }
    }

    [ContextMenu("Validate Pad Mesh Bounds (Play Mode)")]
    private void ValidatePadMeshBounds()
    {
        if (!Application.isPlaying) return;
        label.ForceMeshUpdate();
        float extent = 0f;
        for (int m = 0; m < label.textInfo.materialCount; m++)
        {
            TMP_MeshInfo mesh = label.textInfo.meshInfo[m];
            for (int v = 0; v < mesh.vertexCount; v++)
            {
                Vector3 point = transform.parent.InverseTransformPoint(
                    transform.TransformPoint(mesh.vertices[v]));
                extent = Mathf.Max(extent, Mathf.Abs(point.x), Mathf.Abs(point.z));
            }
        }
        Debug.Log($"{transform.parent.name}: mesh half extent {extent:F4}, " +
            $"limit 0.4000, inside pad: {extent <= 0.4001f}", this);
    }
}
