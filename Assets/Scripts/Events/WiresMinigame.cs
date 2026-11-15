using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Міні-гра "полагодити проводку" як в Among Us.
/// Весь Canvas генерується з коду, у сцені нічого створювати не треба.
/// Повісьте скрипт на будь-який об'єкт і викликайте Open().
/// </summary>
public class WiresMinigame : MonoBehaviour
{
    [Header("Налаштування")]
    [Range(2, 6)] public int wireCount = 4;
    [Tooltip("Відкрити одразу при старті (для тесту)")]
    public bool openOnStart = false;
    public string title = "Полагодьте проводку";

    [Header("Події")]

    public bool IsOpen => root != null;

    private static readonly Color[] Palette =
    {
        new Color(0.90f, 0.15f, 0.15f), // червоний
        new Color(0.20f, 0.35f, 0.95f), // синій
        new Color(0.97f, 0.85f, 0.15f), // жовтий
        new Color(0.95f, 0.30f, 0.80f), // рожевий
        new Color(0.20f, 0.80f, 0.35f), // зелений
        new Color(1.00f, 0.55f, 0.10f), // помаранчевий
    };

    private const float PanelW = 1000f;
    private const float PanelH = 700f;
    private const float SideX = 380f;
    private const float LineThickness = 14f;

    private GameObject root;
    private RectTransform panel, wiresLayer;
    private Image panelImage;

    private RectTransform[] leftPlugs, rightPlugs, lines;
    private int[] leftColor, rightColor;
    private bool[] connected, rightUsed;
    private int connectedCount;
    private bool finished;

    private CursorLockMode prevLock;
    private bool prevVisible;

    private void Start()
    {
        if (openOnStart) Open();
    }

    // ───────────── Відкрити / закрити ─────────────

    public void Open()
    {
        if (IsOpen) return;

        finished = false;
        connectedCount = 0;

        prevLock = Cursor.lockState;
        prevVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Build();
    }

    public void Close()
    {
        if (!IsOpen) return;

        Destroy(root);
        root = null;

        Cursor.lockState = prevLock;
        Cursor.visible = prevVisible;
    }

    // ───────────── Генерація UI ─────────────

    private void Build()
    {
        if (EventSystem.current == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        root = new GameObject("WiresCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(transform, false);

        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // затемнення фону (блокує кліки по грі)
        var dim = CreateImage("Dim", root.transform, new Color(0f, 0f, 0f, 0.65f));
        Stretch(dim.rectTransform);

        // панель
        panelImage = CreateImage("Panel", root.transform, new Color(0.16f, 0.18f, 0.22f, 1f));
        panel = panelImage.rectTransform;
        panel.sizeDelta = new Vector2(PanelW, PanelH);

        CreateText("Title", panel, title, 44, new Vector2(0f, 290f), new Vector2(800f, 70f));
        CreateCloseButton();

        // бокові панелі
        int n = wireCount;
        float sideH = 520f;
        var leftBox = CreateImage("LeftBox", panel, new Color(0.08f, 0.09f, 0.11f));
        leftBox.raycastTarget = false;
        SetRect(leftBox.rectTransform, new Vector2(-SideX - 50f, -40f), new Vector2(120f, sideH));
        var rightBox = CreateImage("RightBox", panel, new Color(0.08f, 0.09f, 0.11f));
        rightBox.raycastTarget = false;
        SetRect(rightBox.rectTransform, new Vector2(SideX + 50f, -40f), new Vector2(120f, sideH));

        // шар для ліній (під штекерами)
        var layerGo = new GameObject("Wires", typeof(RectTransform));
        layerGo.transform.SetParent(panel, false);
        wiresLayer = layerGo.GetComponent<RectTransform>();
        Stretch(wiresLayer);

        // дані
        leftPlugs = new RectTransform[n];
        rightPlugs = new RectTransform[n];
        lines = new RectTransform[n];
        connected = new bool[n];
        rightUsed = new bool[n];
        leftColor = Shuffled(n);
        rightColor = Shuffled(n);

        float top = 150f, bottom = -230f;
        for (int i = 0; i < n; i++)
        {
            float y = Mathf.Lerp(top, bottom, i / (float)(n - 1));

            leftPlugs[i] = CreatePlug("L" + i, Palette[leftColor[i]], new Vector2(-SideX, y));
            rightPlugs[i] = CreatePlug("R" + i, Palette[rightColor[i]], new Vector2(SideX, y));

            int idx = i;
            var handle = leftPlugs[i].gameObject.AddComponent<WireHandle>();
            handle.onDown = e => OnDown(idx, e);
            handle.onDrag = e => OnDrag(idx, e);
            handle.onUp = e => OnUp(idx, e);
        }
    }

    // ───────────── Логіка перетягування ─────────────

    private void OnDown(int i, PointerEventData e)
    {
        if (finished || connected[i] || e.button != PointerEventData.InputButton.Left) return;

        if (lines[i] == null) lines[i] = CreateLine(i);
        lines[i].gameObject.SetActive(true);

        Vector2 start = leftPlugs[i].anchoredPosition;
        SetLine(lines[i], start, start);
    }

    private void OnDrag(int i, PointerEventData e)
    {
        if (finished || connected[i] || lines[i] == null || !lines[i].gameObject.activeSelf) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(panel, e.position, e.pressEventCamera, out Vector2 local);
        SetLine(lines[i], leftPlugs[i].anchoredPosition, local);
    }

    private void OnUp(int i, PointerEventData e)
    {
        if (finished || connected[i] || lines[i] == null || !lines[i].gameObject.activeSelf) return;

        int target = -1;
        for (int j = 0; j < rightPlugs.Length; j++)
        {
            if (rightUsed[j]) continue;
            if (!RectTransformUtility.RectangleContainsScreenPoint(rightPlugs[j], e.position, e.pressEventCamera)) continue;

            if (rightColor[j] == leftColor[i]) target = j;
            break;
        }

        if (target < 0)
        {
            // не влучили або не той колір -> дріт повертається
            lines[i].gameObject.SetActive(false);
            return;
        }

        rightUsed[target] = true;
        connected[i] = true;
        SetLine(lines[i], leftPlugs[i].anchoredPosition, rightPlugs[target].anchoredPosition);

        connectedCount++;
        if (connectedCount >= wireCount) StartCoroutine(Finish());
    }

    private IEnumerator Finish()
    {
        finished = true;
        panelImage.color = new Color(0.12f, 0.30f, 0.18f, 1f);

        yield return new WaitForSecondsRealtime(0.8f);

        Close();
        EnergyCost.broken = false;
    }

    // ───────────── Допоміжні методи ─────────────

    private RectTransform CreatePlug(string name, Color color, Vector2 pos)
    {
        var img = CreateImage(name, panel, color);
        SetRect(img.rectTransform, pos, new Vector2(110f, 56f));

        var outline = img.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
        outline.effectDistance = new Vector2(3f, -3f);

        return img.rectTransform;
    }

    private RectTransform CreateLine(int i)
    {
        var img = CreateImage("Wire" + i, wiresLayer, Palette[leftColor[i]]);
        img.raycastTarget = false; // лінія не блокує кліки

        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        return rt;
    }

    private static void SetLine(RectTransform line, Vector2 a, Vector2 b)
    {
        Vector2 d = b - a;
        line.anchoredPosition = a;
        line.sizeDelta = new Vector2(d.magnitude, LineThickness);
        line.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
    }

    private void CreateCloseButton()
    {
        var img = CreateImage("CloseButton", panel, new Color(0.75f, 0.15f, 0.15f));
        SetRect(img.rectTransform, new Vector2(PanelW / 2f - 50f, PanelH / 2f - 50f), new Vector2(60f, 60f));

        var btn = img.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(Close);

        var label = CreateText("X", img.rectTransform, "X", 36, Vector2.zero, new Vector2(60f, 60f));
        label.raycastTarget = false;
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = color;
        return img;
    }

    private static Text CreateText(string name, Transform parent, string content, int size, Vector2 pos, Vector2 boxSize)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);

        var t = go.GetComponent<Text>();
        t.text = content;
        t.fontSize = size;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
        t.raycastTarget = false;
#if UNITY_2022_2_OR_NEWER
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
        t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif

        SetRect(t.rectTransform, pos, boxSize);
        return t;
    }

    private static void SetRect(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static int[] Shuffled(int n)
    {
        var arr = new int[n];
        for (int i = 0; i < n; i++) arr[i] = i;

        for (int i = n - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            int tmp = arr[i];
            arr[i] = arr[j];
            arr[j] = tmp;
        }
        return arr;
    }
}

/// <summary>Передає події миші/тачу зі штекера в WiresMinigame.</summary>
class WireHandle : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public Action<PointerEventData> onDown, onDrag, onUp;

    public void OnPointerDown(PointerEventData e) => onDown?.Invoke(e);
    public void OnDrag(PointerEventData e) => onDrag?.Invoke(e);
    public void OnPointerUp(PointerEventData e) => onUp?.Invoke(e);
}