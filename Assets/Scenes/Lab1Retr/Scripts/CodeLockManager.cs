using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class CodeLockManager : MonoBehaviour
{
    [Header("Correct Symbols")]
    [SerializeField] private string[] correctSymbols = new string[3];

    [Header("Slots for Symbols")]
    [SerializeField] private Image[] displaySlots;

    [Header("Door")]
    [SerializeField] private GameObject doorObject;
    [SerializeField] private Animation doorAnimation;

    [Header("Visual for Error")]
    [SerializeField] private float wrongFlashDuration = 0.1f;
    [SerializeField] private int wrongFlashCount = 2;
    [SerializeField] private Color wrongColor = Color.red;

    [Header("Visual for Correct")]
    [SerializeField] private float correctFlashDuration = 0.2f;
    [SerializeField] private int correctFlashCount = 3;
    [SerializeField] private Color correctColor = Color.green;

    private string[] currentSymbols = new string[3];
    private Animator doorAnimator;
    private bool isDoorOpened = false;
    private bool isProcessing = false;

    private void Start()
    {
        for (int i = 0; i < 3; i++)
        {
            currentSymbols[i] = "";
            if (displaySlots[i] != null)
            {
                displaySlots[i].sprite = null;
                Color transparentColor = displaySlots[i].color;
                transparentColor.a = 0f;
                displaySlots[i].color = transparentColor;
            }
        }
        
        if (doorObject != null)
            doorAnimator = doorObject.GetComponent<Animator>();
    }

    public void RegisterButton(SymbolButton button, int row, int column) { }

    public void OnSymbolSelected(int row, string symbol, Sprite symbolSprite)
    {
        if (isProcessing) return;

        currentSymbols[row] = symbol;

        if (displaySlots[row] != null)
        {
            displaySlots[row].sprite = symbolSprite;
            Color visibleColor = displaySlots[row].color;
            visibleColor.a = 1f;
            displaySlots[row].color = visibleColor;
        }

        Debug.Log($"Выбран символ {symbol} в ряду {row}. Текущие символы: [{string.Join(", ", currentSymbols)}]");
    }

    public void OnConfirm()
    {
        if (isDoorOpened || isProcessing) return;

        if (HasEmptySymbols())
        {
            Debug.Log("Не все символы выбраны!");
            StartCoroutine(WrongCodeFeedback());
            return;
        }
        
        if (IsCodeCorrect())
        {
            Debug.Log("Код верен!");
            StartCoroutine(CorrectCodeFeedback());
        }
        else
        {
            Debug.Log("Неверный код!");
            StartCoroutine(WrongCodeFeedback());
        }
    }
    
    private bool HasEmptySymbols()
    {
        for (int i = 0; i < 3; i++)
        {
            if (string.IsNullOrEmpty(currentSymbols[i]))
                return true;
        }
        return false;
    }
    
    private bool IsCodeCorrect()
    {
        return new HashSet<string>(currentSymbols).SetEquals(correctSymbols);
    }
    
    private IEnumerator WrongCodeFeedback()
    {
        isProcessing = true;

        Color[] originalColors = new Color[3];
        for (int i = 0; i < 3; i++)
            originalColors[i] = displaySlots[i].color;

        for (int i = 0; i < wrongFlashCount; i++)
        {
            SetSlotsColor(wrongColor);
            yield return new WaitForSeconds(wrongFlashDuration);
            SetSlotsColor(Color.white);
            yield return new WaitForSeconds(wrongFlashDuration);
        }

        for (int i = 0; i < 3; i++)
            displaySlots[i].color = originalColors[i];

        isProcessing = false;
    }

    private IEnumerator CorrectCodeFeedback()
    {
        isProcessing = true;

        Color[] originalColors = new Color[3];
        for (int i = 0; i < 3; i++)
            originalColors[i] = displaySlots[i].color;

        for (int i = 0; i < correctFlashCount; i++)
        {
            SetSlotsColor(correctColor);
            yield return new WaitForSeconds(correctFlashDuration);

            if (i < correctFlashCount - 1)
            {
                SetSlotsColor(originalColors[i % originalColors.Length]);
                yield return new WaitForSeconds(correctFlashDuration);
            }
        }

        SetSlotsColor(correctColor);

        isProcessing = false;
        OpenDoor();
    }

    private void SetSlotsColor(Color color)
    {
        foreach (Image slot in displaySlots)
            if (slot != null) slot.color = color;
    }

    private void OpenDoor()
    {
        if (isDoorOpened) return;
        isDoorOpened = true;
    
        Debug.Log("Дверь открывается!");
    
        if (doorAnimator != null)
            doorAnimator.SetTrigger("Open");
        else if (doorAnimation != null)
            doorAnimation.Play();
        else if (doorObject != null)
            doorObject.SetActive(false);
    }
}