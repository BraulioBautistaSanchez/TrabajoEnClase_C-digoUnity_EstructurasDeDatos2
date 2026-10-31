using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "GameScene";

    [Header("Paneles")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject customPanel;

    [Header("Panel Custom MOde")]
    [SerializeField] private TMP_Text orderText;
    [SerializeField] private Button startCustomButton;
   
    [SerializeField] private Button[] cheeseButtons;

    private readonly List<CheeseType> chosen = new List<CheeseType>();

    private void Start()
    {
        ShowMain();
    }

    // ---------- Botones del menú principal ----------

    public void OnNormalPressed()
    {
        GameSettings.Mode = RouteMode.Normal;
        SceneManager.LoadScene(gameSceneName);
    }

    public void OnReversedPressed()
    {
        GameSettings.Mode = RouteMode.Reversed;
        SceneManager.LoadScene(gameSceneName);
    }

    public void OnCustomPressed()
    {
        mainPanel.SetActive(false);
        customPanel.SetActive(true);
        ResetCustom();
    }

    public void PickCheese(int typeIndex)
    {
        CheeseType type = (CheeseType)typeIndex;
        if (chosen.Contains(type)) return;

        chosen.Add(type);
        cheeseButtons[typeIndex].interactable = false;
        RefreshUI();
    }

    public void ResetCustom()
    {
        chosen.Clear();
        foreach (Button b in cheeseButtons) b.interactable = true;
        RefreshUI();
    }

    public void ConfirmCustom()
    {
        if (chosen.Count < cheeseButtons.Length) return;

        GameSettings.Mode = RouteMode.Custom;
        GameSettings.CustomOrder = new List<CheeseType>(chosen); 
        SceneManager.LoadScene(gameSceneName);
    }

    public void ShowMain()
    {
        mainPanel.SetActive(true);
        customPanel.SetActive(false);
    }

    private void RefreshUI()
    {
        orderText.text = chosen.Count == 0
            ? "Elige el orden de los quesos..."
            : string.Join(" -> ", chosen);

        startCustomButton.interactable = chosen.Count == cheeseButtons.Length;
    }
}
