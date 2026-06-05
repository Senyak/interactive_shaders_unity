using UnityEngine;
using TMPro;
using System.Collections;

public class NumericCodeLockManager : MonoBehaviour
{
    [Header("Password Settings")]
    [SerializeField] private string correctCode = "1234";
    [SerializeField] private int maxCodeLength = 4;
    
    [Header("UI Elements")]
    [SerializeField] private TMP_Text displayText;
    [SerializeField] private GameObject doorObject;
    [SerializeField] private Animation doorAnimation;
    
    [Header("Visual for Error")]
    [SerializeField] private float wrongFlashDuration = 0.2f;
    [SerializeField] private int wrongFlashCount = 3;
    [SerializeField] private Color wrongColor = Color.red;
    
    [Header("Visual for Correct")]
    [SerializeField] private float correctFlashDuration = 0.2f;
    [SerializeField] private int correctFlashCount = 3;
    [SerializeField] private Color correctColor = Color.green;
    
    private string currentCode = "";
    private Animator doorAnimator;
    private Color originalDisplayColor;
    private bool isDoorOpen = false;
    private bool isProcessing = false;
    
    private void Start()
    {
        UpdateDisplay();
        
        if (doorObject != null)
            doorAnimator = doorObject.GetComponent<Animator>();
        
        if (displayText != null)
        {
            originalDisplayColor = displayText.color;
            Debug.Log($"Оригинальный цвет дисплея: {originalDisplayColor}");
        }
    }
    
    public void OnDigitPressed(string digit)
    {
        if (isDoorOpen || isProcessing) return;
        
        if (currentCode.Length < maxCodeLength)
        {
            currentCode += digit;
            UpdateDisplay();
            Debug.Log($"Текущий код: {currentCode}");
        }
    }
    
    public void OnBackspacePressed()
    {
        if (isDoorOpen || isProcessing) return;
        
        if (currentCode.Length > 0)
        {
            currentCode = currentCode.Substring(0, currentCode.Length - 1);
            UpdateDisplay();
            Debug.Log($"Стереть. Текущий код: {currentCode}");
        }
    }
    
    public void OnConfirmPressed()
    {
        if (isDoorOpen || isProcessing) return;
        
        if (currentCode.Length != maxCodeLength)
        {
            Debug.Log($"Код должен состоять из {maxCodeLength} цифр. Сейчас: {currentCode.Length}");
            StartCoroutine(WrongCodeFeedback(clearAfter: false));
            return;
        }
        
        if (currentCode == correctCode)
        {
            Debug.Log("Правильный код!");
            StartCoroutine(CorrectCodeFeedback());
        }
        else
        {
            Debug.Log($"Неверный код! Введено: {currentCode}, Ожидалось: {correctCode}");
            StartCoroutine(WrongCodeFeedback(clearAfter: true));
        }
    }
    
    public void ClearCode()
    {
        currentCode = "";
        UpdateDisplay();
        Debug.Log("Код очищен");
    }
    
    private void UpdateDisplay()
    {
        if (displayText != null)
            displayText.text = currentCode;
    }
    
    private IEnumerator WrongCodeFeedback(bool clearAfter = false)
    {
        isProcessing = true;
        
        if (displayText != null)
        {
            for (int i = 0; i < wrongFlashCount; i++)
            {
                displayText.color = wrongColor;
                displayText.ForceMeshUpdate();
                yield return new WaitForSeconds(wrongFlashDuration);
                
                displayText.color = originalDisplayColor;
                displayText.ForceMeshUpdate();
                yield return new WaitForSeconds(wrongFlashDuration);
            }
            
            if (clearAfter)
                ClearCode();
        }
        
        isProcessing = false;
    }
    
    private IEnumerator CorrectCodeFeedback()
    {
        isProcessing = true;
        
        if (displayText != null)
        {
            for (int i = 0; i < correctFlashCount; i++)
            {
                displayText.color = correctColor;
                displayText.ForceMeshUpdate();
                yield return new WaitForSeconds(correctFlashDuration);
                
                if (i < correctFlashCount - 1)
                {
                    displayText.color = originalDisplayColor;
                    displayText.ForceMeshUpdate();
                    yield return new WaitForSeconds(correctFlashDuration);
                }
            }
            displayText.color = correctColor;
            displayText.ForceMeshUpdate();
        }
        
        isProcessing = false;
        OpenDoor();
    }
    
    private void OpenDoor()
    {
        if (isDoorOpen) return;
        isDoorOpen = true;
        
        Debug.Log("Дверь открыта! Правильный код!");
        
        if (doorAnimator != null)
            doorAnimator.SetTrigger("Open");
        else if (doorAnimation != null)
            doorAnimation.Play();
        else if (doorObject != null)
            doorObject.SetActive(false);
    }
}