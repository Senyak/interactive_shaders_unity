using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class NumericButton : MonoBehaviour
{
    [Header("Digits Buttons")]
    [SerializeField] private string digit; 
    
    [Header("Button Type")]
    [SerializeField] private ButtonType buttonType = ButtonType.Digit;
    
    public enum ButtonType
    {
        Digit, 
        Backspace,
        Confirm 
    }
    
    private Button button;
    private NumericCodeLockManager lockManager;
    
    private void Awake()
    {
        button = GetComponent<Button>();
        lockManager = FindObjectOfType<NumericCodeLockManager>();
        button.onClick.AddListener(OnClick);
    }
    
    private void OnClick()
    {
        if (lockManager == null) return;
        
        switch (buttonType)
        {
            case ButtonType.Digit:
                lockManager.OnDigitPressed(digit);
                break;
            case ButtonType.Backspace:
                lockManager.OnBackspacePressed();
                break;
            case ButtonType.Confirm:
                lockManager.OnConfirmPressed();
                break;
        }
    }
}