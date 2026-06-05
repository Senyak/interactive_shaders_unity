using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class SymbolButton : MonoBehaviour
{
    [Header("Data of Symbols")]
    [SerializeField] private int row; 
    [SerializeField] private int column; 
    [SerializeField] private string symbolID; 
    [SerializeField] private Sprite symbolSprite; 
    private Button button;
    private CodeLockManager lockManager;

    private void Awake()
    {
        button = GetComponent<Button>();
        lockManager = FindObjectOfType<CodeLockManager>();
        
        if (lockManager != null)
            lockManager.RegisterButton(this, row, column);

        button.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        if (lockManager != null)
            lockManager.OnSymbolSelected(row, symbolID, symbolSprite);
    }
}