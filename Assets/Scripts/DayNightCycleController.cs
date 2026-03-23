using UnityEngine;

[ExecuteInEditMode]
[DisallowMultipleComponent]
public class DayNightCycleController : MonoBehaviour
{
    [Header("Scene References")]
    public Light sunLight;
    public Light moonLight;
    public Camera targetCamera;

    [Header("Time")]
    [Range(0f, 24f)] public float currentHour = 9f;
    public bool playTime;
    [Range(0.1f, 240f)] public float dayDurationInSeconds = 90f;

    [Header("Sun Path")]
    [Range(0f, 12f)] public float sunriseHour = 6f;
    [Range(12f, 24f)] public float sunsetHour = 19.5f;
    [Range(-180f, 180f)] public float sunriseAzimuth = 85f;
    [Range(-180f, 180f)] public float sunsetAzimuth = 275f;
    [Range(5f, 89f)] public float maxSunElevation = 62f;
    [Range(0f, 1f)] public float shadowStrengthAtNoon = 0.95f;

    [Header("Visuals")]
    public Color nightAmbientColor = new Color(0.05f, 0.07f, 0.14f, 1f);
    public Color dawnAmbientColor = new Color(0.65f, 0.55f, 0.45f, 1f);
    public Color dayAmbientColor = new Color(0.92f, 0.95f, 1f, 1f);
    public Color duskAmbientColor = new Color(0.72f, 0.5f, 0.42f, 1f);
    [Range(0f, 2f)] public float nightAmbientIntensity = 0.15f;
    [Range(0f, 2f)] public float dayAmbientIntensity = 1f;
    [Range(0f, 2f)] public float skyExposureAtNight = 0.3f;
    [Range(0f, 3f)] public float skyExposureAtDay = 1.1f;

    private Material runtimeSkybox;

    public float NormalizedTime
    {
        get { return Mathf.Repeat(currentHour, 24f) / 24f; }
    }

    public bool IsDaylight
    {
        get { return currentHour >= sunriseHour && currentHour <= sunsetHour; }
    }

    private void Reset()
    {
        sunLight = GetComponent<Light>();

        if (sunLight == null)
        {
            sunLight = FindDirectionalLight();
        }

        targetCamera = Camera.main;
    }

    private void OnEnable()
    {
        EnsureSkyboxInstance();
        EnsureMoonLight();
        ApplyTimeOfDay();
    }

    private void Update()
    {
        if (playTime && Application.isPlaying && dayDurationInSeconds > 0.01f)
        {
            currentHour += (24f / dayDurationInSeconds) * Time.deltaTime;
            currentHour = Mathf.Repeat(currentHour, 24f);
        }

        ApplyTimeOfDay();
    }

    public void SetTimeOfDay(float hour)
    {
        currentHour = Mathf.Repeat(hour, 24f);
        ApplyTimeOfDay();
    }

    public void StepTime(float deltaHours)
    {
        SetTimeOfDay(currentHour + deltaHours);
    }

    public string GetFormattedHour()
    {
        int totalMinutes = Mathf.RoundToInt(Mathf.Repeat(currentHour, 24f) * 60f);
        totalMinutes %= 24 * 60;
        int hours = totalMinutes / 60;
        int minutes = totalMinutes % 60;
        return hours.ToString("00") + ":" + minutes.ToString("00");
    }

    public void TogglePlayback()
    {
        playTime = !playTime;
    }

    public void ApplyTimeOfDay()
    {
        if (sunLight == null)
        {
            sunLight = FindDirectionalLight();
        }

        if (sunLight == null)
        {
            return;
        }

        EnsureMoonLight();
        EnsureSkyboxInstance();

        float daylightFactor = EvaluateDaylightFactor(currentHour);
        float sunTravel = EvaluateSunTravel(currentHour);
        float sunElevation = Mathf.Lerp(-12f, maxSunElevation, Mathf.Sin(sunTravel * Mathf.PI));
        float sunAzimuth = Mathf.LerpAngle(sunriseAzimuth, sunsetAzimuth, sunTravel);
        sunLight.transform.rotation = Quaternion.Euler(sunElevation, sunAzimuth, 0f);
        sunLight.intensity = Mathf.Lerp(0.03f, 1.05f, daylightFactor);
        sunLight.color = Color.Lerp(new Color(1f, 0.56f, 0.32f, 1f), Color.white, Mathf.Clamp01(daylightFactor * 1.15f));
        sunLight.shadowStrength = Mathf.Lerp(0f, shadowStrengthAtNoon, daylightFactor);

        if (moonLight != null)
        {
            moonLight.transform.rotation = Quaternion.Euler(-sunElevation, sunAzimuth + 180f, 0f);
            float moonFactor = 1f - daylightFactor;
            moonLight.enabled = moonFactor > 0.02f;
            moonLight.intensity = Mathf.Lerp(0f, 0.2f, moonFactor);
        }

        RenderSettings.sun = sunLight;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = EvaluateAmbientColor(daylightFactor);
        RenderSettings.ambientIntensity = Mathf.Lerp(nightAmbientIntensity, dayAmbientIntensity, daylightFactor);
        RenderSettings.reflectionIntensity = Mathf.Lerp(0.05f, 0.55f, daylightFactor);

        if (runtimeSkybox != null && runtimeSkybox.HasProperty("_Exposure"))
        {
            runtimeSkybox.SetFloat("_Exposure", Mathf.Lerp(skyExposureAtNight, skyExposureAtDay, daylightFactor));
        }

        DynamicGI.UpdateEnvironment();
    }

    private void EnsureMoonLight()
    {
        if (moonLight != null)
        {
            return;
        }

        Transform existingMoon = transform.parent != null ? transform.parent.Find("Moon Light") : null;
        if (existingMoon != null)
        {
            moonLight = existingMoon.GetComponent<Light>();
        }

        if (moonLight != null)
        {
            return;
        }

        GameObject moonObject = new GameObject("Moon Light");
        if (transform.parent != null)
        {
            moonObject.transform.SetParent(transform.parent, false);
        }

        moonLight = moonObject.AddComponent<Light>();
        moonLight.type = LightType.Directional;
        moonLight.color = new Color(0.6f, 0.72f, 1f, 1f);
        moonLight.intensity = 0.12f;
        moonLight.shadows = LightShadows.None;
    }

    private void EnsureSkyboxInstance()
    {
        if (runtimeSkybox != null)
        {
            return;
        }

        if (RenderSettings.skybox != null)
        {
            runtimeSkybox = new Material(RenderSettings.skybox);
            runtimeSkybox.name = RenderSettings.skybox.name + " Runtime";
            RenderSettings.skybox = runtimeSkybox;
        }
    }

    private float EvaluateDaylightFactor(float hour)
    {
        if (hour < sunriseHour - 1f || hour > sunsetHour + 1f)
        {
            return 0f;
        }

        if (hour >= sunriseHour && hour <= sunsetHour)
        {
            float progress = Mathf.InverseLerp(sunriseHour, sunsetHour, hour);
            return Mathf.Clamp01(Mathf.Sin(progress * Mathf.PI));
        }

        if (hour < sunriseHour)
        {
            return Mathf.InverseLerp(sunriseHour - 1f, sunriseHour, hour) * 0.2f;
        }

        return Mathf.InverseLerp(sunsetHour + 1f, sunsetHour, hour) * 0.2f;
    }

    private float EvaluateSunTravel(float hour)
    {
        if (hour >= sunriseHour && hour <= sunsetHour)
        {
            return Mathf.InverseLerp(sunriseHour, sunsetHour, hour);
        }

        if (hour < sunriseHour)
        {
            return 0f;
        }

        return 1f;
    }

    private Color EvaluateAmbientColor(float daylightFactor)
    {
        if (daylightFactor <= 0.01f)
        {
            return nightAmbientColor;
        }

        if (daylightFactor < 0.25f)
        {
            return Color.Lerp(dawnAmbientColor, dayAmbientColor, daylightFactor / 0.25f);
        }

        if (currentHour > sunsetHour - 1.5f)
        {
            float duskFactor = Mathf.InverseLerp(sunsetHour - 1.5f, sunsetHour, currentHour);
            return Color.Lerp(dayAmbientColor, duskAmbientColor, duskFactor);
        }

        return Color.Lerp(dawnAmbientColor, dayAmbientColor, daylightFactor);
    }

    private Light FindDirectionalLight()
    {
        Light[] lights = FindObjectsOfType<Light>();
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i].type == LightType.Directional)
            {
                return lights[i];
            }
        }

        return null;
    }
}
