using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class StartupFade : MonoBehaviour
{
    public Canvas targetCanvas;
    public Texture2D centerImage;

    public float displayTime = 1.5f;
    public float fadeDuration = 1f;

    private Image overlayImage;
    private RawImage logoImage;

    void Start()
    {
        CreateOverlay();
        StartCoroutine(FadeSequence());
    }

    void CreateOverlay()
    {
        // 🔹 Create ROOT canvas (not a child of your existing one)
        GameObject canvasGO = new GameObject("StartupCanvas");

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        // 🔥 THIS is the key line
        canvas.sortingOrder = 9999;

        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        // 🔹 Now create overlay under THIS canvas
        GameObject overlayGO = new GameObject("StartupOverlay");
        overlayGO.transform.SetParent(canvasGO.transform, false);

        RectTransform rect = overlayGO.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        overlayImage = overlayGO.AddComponent<Image>();
        overlayImage.color = new Color(0, 0, 0, 1f);

        // 🔹 Logo
        GameObject logoGO = new GameObject("Logo");
        logoGO.transform.SetParent(overlayGO.transform, false);

        RectTransform logoRect = logoGO.AddComponent<RectTransform>();
        logoRect.sizeDelta = new Vector2(156, 80);
        logoRect.anchorMin = new Vector2(0.5f, 0.5f);
        logoRect.anchorMax = new Vector2(0.5f, 0.5f);
        logoRect.anchoredPosition = Vector2.zero;


        logoImage = logoGO.AddComponent<RawImage>();
        logoImage.texture = centerImage;
        logoImage.color = new Color(1, 1, 1, 0f);
    }

    IEnumerator FadeSequence()
    {
        float t;

        // 🔹 1. Fade IN logo
        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Clamp01(t / fadeDuration);
            logoImage.color = new Color(1, 1, 1, alpha);
            yield return null;
        }

        // 🔹 2. Hold logo on screen
        yield return new WaitForSeconds(displayTime);

        // 🔹 3. Fade OUT logo
        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float alpha = 1f - Mathf.Clamp01(t / fadeDuration);
            logoImage.color = new Color(1, 1, 1, alpha);
            yield return null;
        }

        // 🔹 4. Fade OUT background LAST
        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float alpha = 1f - Mathf.Clamp01(t / fadeDuration);
            overlayImage.color = new Color(0, 0, 0, alpha);
            yield return null;
        }

        Destroy(overlayImage.gameObject);
    }
}