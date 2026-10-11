using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PasswordToggle : MonoBehaviour
{
    [SerializeField] private TMP_InputField passwordField;
    [SerializeField] private Image icon;
    [SerializeField] private Sprite showIcon;   // eye
    [SerializeField] private Sprite hideIcon;   // eye with slash

    private bool isVisible;

    private void OnEnable()
    {
        isVisible = false;
        Apply();
    }

    // Hook this to the button's OnClick in the Inspector
    public void Toggle()
    {
        isVisible = !isVisible;
        Apply();
    }

    private void Apply()
    {
        passwordField.contentType = isVisible
            ? TMP_InputField.ContentType.Standard
            : TMP_InputField.ContentType.Password;
        passwordField.ForceLabelUpdate();
        icon.sprite = isVisible ? hideIcon : showIcon;
    }
}