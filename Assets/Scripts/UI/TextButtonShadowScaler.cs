using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class TextButtonShadowScaler : MonoBehaviour,
    IPointerEnterHandler,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerExitHandler
{
    [SerializeField] private TextMeshProUGUI mainText;
    [SerializeField] private TextMeshProUGUI shadowText;
    [SerializeField] private Button button;

    [Header("Content")]
    [SerializeField] private string text = "start";
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private Color shadowColor = new Color32(253, 32, 107, 255);

    [Header("Text")]
    [SerializeField, Min(0.01f)] private float fontSize = 150f;

    [Header("Shadow Scaling")]
    [SerializeField, Min(0.01f)] private float referenceFontSize = 150f;
    [SerializeField] private Vector2 referenceShadowOffset = new Vector2(10f, -10f);
    [SerializeField] private Vector2 releasedTextPosition = Vector2.zero;

    [Header("Interaction")]
    [SerializeField, Range(0f, 1f)] private float hoverDepth = 0.35f;

    private float lastFontSize = -1f;
    private string lastText;
    private Color lastTextColor;
    private Color lastShadowColor;
    private bool isHovered;
    private bool isPressed;

    private void Reset()
    {
        ResolveReferences();
        CaptureReferenceValues();
        Apply();
    }

    private void OnEnable()
    {
        ResolveReferences();
        Apply();
    }

    private void OnDisable()
    {
        isHovered = false;
        SetPressed(false);
    }

    private void OnValidate()
    {
        ResolveReferences();
        Apply();
    }

    private void LateUpdate()
    {
        if (mainText == null || shadowText == null)
        {
            ResolveReferences();
        }

        if (mainText == null || shadowText == null)
        {
            return;
        }

        if (!Mathf.Approximately(fontSize, lastFontSize)
            || text != lastText
            || textColor != lastTextColor
            || shadowColor != lastShadowColor)
        {
            Apply();
        }
        else if (!Mathf.Approximately(mainText.fontSize, lastFontSize))
        {
            fontSize = mainText.fontSize;
            Apply();
        }
        else if (mainText.text != lastText)
        {
            text = mainText.text;
            Apply();
        }
        else if (mainText.color != lastTextColor)
        {
            textColor = mainText.color;
            Apply();
        }
        else if (shadowText.color != lastShadowColor)
        {
            shadowColor = shadowText.color;
            Apply();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button == null || button.IsInteractable())
        {
            isHovered = true;
            UpdateInteractionPosition();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (button == null || button.IsInteractable())
        {
            SetPressed(true);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        SetPressed(false);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        SetPressed(false);
    }

    private void ResolveReferences()
    {
        if (mainText == null)
        {
            mainText = FindChildText("Text") ?? FindChildText("White");
        }

        if (shadowText == null)
        {
            shadowText = FindChildText("Shadow");
        }

        if (button == null)
        {
            button = GetComponent<Button>();
        }
    }

    private TextMeshProUGUI FindChildText(string childName)
    {
        Transform child = transform.Find(childName);
        return child != null ? child.GetComponent<TextMeshProUGUI>() : null;
    }

    private void CaptureReferenceValues()
    {
        if (mainText != null)
        {
            text = mainText.text;
            textColor = mainText.color;
            fontSize = Mathf.Max(0.01f, mainText.fontSize);
            referenceFontSize = fontSize;
            releasedTextPosition = mainText.rectTransform.anchoredPosition;
        }

        if (shadowText != null)
        {
            shadowColor = shadowText.color;
            referenceShadowOffset = shadowText.rectTransform.anchoredPosition;
        }
    }

    private void Apply()
    {
        if (mainText == null || shadowText == null)
        {
            return;
        }

        fontSize = Mathf.Max(0.01f, fontSize);
        float scale = fontSize / Mathf.Max(0.01f, referenceFontSize);

        mainText.text = text;
        mainText.color = textColor;
        mainText.fontSize = fontSize;

        shadowText.text = text;
        shadowText.color = shadowColor;
        shadowText.fontSize = fontSize;
        shadowText.rectTransform.anchoredPosition = referenceShadowOffset * scale;

        UpdateInteractionPosition();

        lastFontSize = fontSize;
        lastText = text;
        lastTextColor = textColor;
        lastShadowColor = shadowColor;
    }

    private void SetPressed(bool pressed)
    {
        isPressed = pressed;
        UpdateInteractionPosition();
    }

    private void UpdateInteractionPosition()
    {
        if (mainText == null || shadowText == null)
        {
            return;
        }

        Vector2 pressedPosition = shadowText.rectTransform.anchoredPosition;

        if (isPressed)
        {
            mainText.rectTransform.anchoredPosition = pressedPosition;
        }
        else if (isHovered)
        {
            mainText.rectTransform.anchoredPosition = Vector2.Lerp(
                releasedTextPosition,
                pressedPosition,
                hoverDepth);
        }
        else
        {
            mainText.rectTransform.anchoredPosition = releasedTextPosition;
        }
    }
}
