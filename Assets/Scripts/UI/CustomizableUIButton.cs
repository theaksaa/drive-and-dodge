using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(Image), typeof(Button))]
public sealed class CustomizableUIButton : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    ICancelHandler
{
    [Header("Content")]
    [SerializeField] private string label = "start";

    [Header("Sprites")]
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite pressedSprite;

    [Header("Icon")]
    [SerializeField] private Sprite iconSprite;

    [Header("Pressed State")]
    [SerializeField, Min(0f)] private float pressedTextDropPixels = 3f;

    [Header("References")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [SerializeField] private RectTransform textRoot;
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private TextMeshProUGUI shadowText;

    private Vector2 releasedTextPosition;
    private bool hasReleasedTextPosition;
    private Vector2 releasedIconPosition;
    private bool hasReleasedIconPosition;

    public string Label
    {
        get => label;
        set
        {
            label = value;
            Apply();
        }
    }

    public Sprite NormalSprite
    {
        get => normalSprite;
        set
        {
            normalSprite = value;
            Apply();
        }
    }

    public Sprite PressedSprite
    {
        get => pressedSprite;
        set
        {
            pressedSprite = value;
            Apply();
        }
    }

    public Sprite IconSprite
    {
        get => iconSprite;
        set
        {
            iconSprite = value;
            Apply();
        }
    }

    private void Reset()
    {
        ResolveReferences();

        if (normalSprite == null && backgroundImage != null)
        {
            normalSprite = backgroundImage.sprite;
        }

        if (iconSprite == null && iconImage != null)
        {
            iconSprite = iconImage.sprite;
        }

        Apply();
        CaptureReleasedContentPositions();
    }

    private void OnEnable()
    {
        ResolveReferences();
        Apply();
        CaptureReleasedContentPositions();
    }

    private void OnDisable()
    {
        SetContentPressed(false);
    }

    private void OnValidate()
    {
        ResolveReferences();
        Apply();

        if (!Application.isPlaying)
        {
            CaptureReleasedContentPositions();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (button != null && button.IsInteractable())
        {
            SetContentPressed(true);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        SetContentPressed(false);
    }

    public void OnCancel(BaseEventData eventData)
    {
        SetContentPressed(false);
    }

    [ContextMenu("Apply Button Settings")]
    public void Apply()
    {
        ResolveReferences();

        if (text != null)
        {
            text.text = label;
        }

        if (shadowText != null)
        {
            shadowText.text = label;
        }

        if (iconImage != null && iconSprite != null)
        {
            iconImage.sprite = iconSprite;
        }

        if (backgroundImage != null && normalSprite != null)
        {
            backgroundImage.sprite = normalSprite;
        }

        if (button == null)
        {
            return;
        }

        button.targetGraphic = backgroundImage;
        button.transition = Selectable.Transition.SpriteSwap;

        SpriteState state = button.spriteState;
        state.pressedSprite = pressedSprite;
        button.spriteState = state;
    }

    private void ResolveReferences()
    {
        if (backgroundImage == null)
        {
            backgroundImage = GetComponent<Image>();
        }

        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (iconImage == null)
        {
            Transform icon = transform.Find("Icon");
            iconImage = icon != null ? icon.GetComponent<Image>() : null;
        }

        if (textRoot == null)
        {
            textRoot = transform.Find("Text") as RectTransform;
        }

        if (text == null)
        {
            text = FindText("Text/Text") ?? FindText("Text") ?? FindText("White");
        }

        if (shadowText == null)
        {
            shadowText = FindText("Text/Shadow") ?? FindText("Shadow");
        }
    }

    private TextMeshProUGUI FindText(string path)
    {
        Transform child = transform.Find(path);
        return child != null ? child.GetComponent<TextMeshProUGUI>() : null;
    }

    private void CaptureReleasedContentPositions()
    {
        if (textRoot != null)
        {
            releasedTextPosition = textRoot.anchoredPosition;
            hasReleasedTextPosition = true;
        }

        if (iconImage != null)
        {
            releasedIconPosition = iconImage.rectTransform.anchoredPosition;
            hasReleasedIconPosition = true;
        }
    }

    private void SetContentPressed(bool pressed)
    {
        if (textRoot == null || iconImage == null)
        {
            ResolveReferences();
        }

        if (textRoot == null && iconImage == null)
        {
            return;
        }

        if ((textRoot != null && !hasReleasedTextPosition)
            || (iconImage != null && !hasReleasedIconPosition))
        {
            CaptureReleasedContentPositions();
        }

        Vector2 offset = pressed ? CalculatePressedContentOffset() : Vector2.zero;

        if (textRoot != null)
        {
            textRoot.anchoredPosition = releasedTextPosition + offset;
        }

        if (iconImage != null)
        {
            iconImage.rectTransform.anchoredPosition = releasedIconPosition + offset;
        }
    }

    private Vector2 CalculatePressedContentOffset()
    {
        if (backgroundImage == null || normalSprite == null)
        {
            return Vector2.down * pressedTextDropPixels;
        }

        float spriteHeight = normalSprite.rect.height;
        if (spriteHeight <= 0f)
        {
            return Vector2.down * pressedTextDropPixels;
        }

        float displayedHeight = Mathf.Abs(backgroundImage.rectTransform.rect.height);
        float unitsPerSourcePixel = displayedHeight / spriteHeight;
        return Vector2.down * (pressedTextDropPixels * unitsPerSourcePixel);
    }
}
