using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    [Header("Time Settings")]
    public float dayLengthInSeconds = 120f;

    [Header("Sun Settings")]
    public Light sunLight;
    public Gradient sunColor;
    public AnimationCurve sunIntensity;

    [Header("Skybox Settings")]
    public Gradient skyColorGradient;

    private float timeOfDay;

    void Awake()
    {
        // --- Default intensity curve ---
        if (sunIntensity == null || sunIntensity.keys.Length == 0)
        {
            sunIntensity = new AnimationCurve();
            sunIntensity.AddKey(new Keyframe(0f, 0.1f));   // Night
            sunIntensity.AddKey(new Keyframe(0.25f, 1f));  // Morning
            sunIntensity.AddKey(new Keyframe(0.5f, 1.2f)); // Noon
            sunIntensity.AddKey(new Keyframe(0.75f, 1f));  // Evening
            sunIntensity.AddKey(new Keyframe(1f, 0.1f));   // Night again
            for (int i = 0; i < sunIntensity.keys.Length; i++)
                sunIntensity.SmoothTangents(i, 0f);
        }

        // --- Default color gradient ---
        if (sunColor == null)
        {
            sunColor = new Gradient();
            GradientColorKey[] colorKeys = {
                new GradientColorKey(new Color(0.2f,0.2f,0.5f), 0f),  // night
                new GradientColorKey(new Color(1f,0.6f,0.3f), 0.25f), // morning
                new GradientColorKey(Color.white, 0.5f),              // day
                new GradientColorKey(new Color(1f,0.5f,0.3f), 0.75f), // sunset
                new GradientColorKey(new Color(0.1f,0.1f,0.3f), 1f)   // night again
            };
            GradientAlphaKey[] alphaKeys = {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            };
            sunColor.SetKeys(colorKeys, alphaKeys);
        }

        // --- Skybox gradient ---
        if (skyColorGradient == null)
        {
            skyColorGradient = new Gradient();
            GradientColorKey[] skyKeys = {
                new GradientColorKey(new Color(0.05f,0.05f,0.2f), 0f), // night blue
                new GradientColorKey(new Color(0.7f,0.5f,0.3f), 0.25f), // dawn
                new GradientColorKey(new Color(0.5f,0.7f,1f), 0.5f),   // bright sky
                new GradientColorKey(new Color(0.8f,0.4f,0.2f), 0.75f), // dusk
                new GradientColorKey(new Color(0.02f,0.02f,0.1f), 1f)  // deep night
            };
            GradientAlphaKey[] alphaKeys = {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            };
            skyColorGradient.SetKeys(skyKeys, alphaKeys);
        }

        if (sunLight == null)
            sunLight = GetComponent<Light>();
    }

    void Update()
    {
        timeOfDay += Time.deltaTime / dayLengthInSeconds;
        if (timeOfDay >= 1f) timeOfDay = 0f;

        // Rotate sun around scene
        transform.rotation = Quaternion.Euler(timeOfDay * 360f - 90f, 170f, 0);

        // Update light color & intensity
        if (sunLight != null)
        {
            sunLight.color = sunColor.Evaluate(timeOfDay);
            sunLight.intensity = sunIntensity.Evaluate(timeOfDay);
        }

        // Update skybox tint
        if (RenderSettings.skybox.HasProperty("_Tint"))
        {
            RenderSettings.skybox.SetColor("_Tint", skyColorGradient.Evaluate(timeOfDay));
        }
    }
}
