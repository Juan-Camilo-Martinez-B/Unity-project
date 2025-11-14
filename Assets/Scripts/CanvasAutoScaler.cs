using UnityEngine;
using UnityEngine.UI;

// Este script ajusta automáticamente el CanvasScaler para que la UI se vea bien en cualquier resolución.
[RequireComponent(typeof(CanvasScaler))]
public class CanvasAutoScaler : MonoBehaviour
{
    public Vector2 referenceResolution = new Vector2(1920, 1080);
    public CanvasScaler.ScreenMatchMode matchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
    [Range(0, 1)]
    public float matchWidthOrHeight = 0.5f; // 0 = ancho, 1 = alto, 0.5 = ambos

    void Awake()
    {
        CanvasScaler scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = referenceResolution;
        scaler.screenMatchMode = matchMode;
        scaler.matchWidthOrHeight = matchWidthOrHeight;
    }
}
