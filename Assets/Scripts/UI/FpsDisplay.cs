using TMPro;
using UnityEngine;


public class FpsDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI fpsText;
    [SerializeField] private float updateInterval = 0.5f;

    private float timer;
    private int frames;

    private void Update()
    {
        frames++;
        timer += Time.unscaledDeltaTime;

        if (timer >= updateInterval)
        {
            float fps = frames / timer;
            fpsText.text = $"FPS: {Mathf.RoundToInt(fps)}";
            frames = 0;
            timer = 0f;
        }
    }
}