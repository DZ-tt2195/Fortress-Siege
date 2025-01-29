using System.Collections.Generic;
using UnityEngine;
using MyBox;
using System.Reflection;
using Photon.Pun;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.IO; 
using System.Linq;
using UnityEngine.Networking;
using System;

public class CarryVariables : MonoBehaviour
{

#region Setup

    public static CarryVariables instance;
    [Foldout("Prefabs", true)]
    public Player playerPrefab;
    public CardLayout cardPrefab;
    public Popup textPopup;
    public Popup cardPopup;
    public SliderChoice sliderPopup;
    public GameObject movingTroopPrefab;
    public PlayerBase playerBasePrefab;

    [Foldout("Right click", true)]
    [SerializeField] Transform rightClickBackground;
    [SerializeField] CardLayout rightClickCard;
    [SerializeField] TMP_Text rightClickText;

    [Foldout("Misc", true)]
    [SerializeField] Transform permanentCanvas;
    public Sprite faceDownSprite;
    [ReadOnly] public List<string> cardScripts = new();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            Application.targetFrameRate = 60;
            GetScripts();
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    #endregion

#region Right click

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
            rightClickBackground.gameObject.SetActive(false);
    }

    public void RightClickDisplay(Card card, float alpha)
    {
        rightClickBackground.gameObject.SetActive(true);
        rightClickCard.FillInCards(card);
        rightClickCard.cg.alpha = (alpha == 0) ? 0 : 1;
    }

    #endregion

#region Find Cards

    void GetScripts()
    {
        if (Application.isEditor)
        {
            string filePath = $"Assets/Resources/AvailableScripts.txt";
            List<string[]> allStrings = new() { ScriptsInRange("Cards") };
            File.WriteAllText(filePath, Format(allStrings));
        }

        string[] ScriptsInRange(string range)
        {
            string[] list = Directory.GetFiles($"Assets/Scripts/{range}", "*.cs", SearchOption.TopDirectoryOnly);
            string[] answer = new string[list.Length];
            for (int i = 0; i < list.Length; i++)
                answer[i] = Path.GetFileNameWithoutExtension(list[i]);

            return answer;
        }

        string Format(List<string[]> allStrings)
        {
            string content = "{\n";
            for (int i = 0; i < allStrings.Count; i++)
            {
                content += "  [\n";
                for (int j = 0; j < allStrings[i].Length; j++)
                {
                    content += $"    \"{allStrings[i][j]}\"";
                    if (j < allStrings[i].Length - 1)
                        content += ",";
                    content += "\n";
                }
                content += "  ]";
                if (i < allStrings.Count - 1)
                    content += ",";
                content += "\n";
            }
            content += "}\n";
            return content;
        }

        var data = ReadFile("AvailableScripts");
        for (int i = 0; i < data[1].Length; i++)
            data[1][i].Trim().Replace("\"", "");

        string[] nextArray = new string[data[1].Length];

        for (int j = 0; j < data[1].Length; j++)
        {
            string nextObject = data[1][j].Replace("\"", "").Replace("\\", "").Replace("]", "").Trim();
            nextArray[j] = nextObject;
        }

        cardScripts = nextArray.ToList();
    }

    string[][] ReadFile(string range)
    {
        TextAsset data = Resources.Load($"{range}") as TextAsset;
        string editData = data.text;
        editData = editData.Replace("],", "").Replace("{", "").Replace("}", "");

        string[] numLines = editData.Split("[");
        string[][] list = new string[numLines.Length][];

        for (int i = 0; i < numLines.Length; i++)
            list[i] = numLines[i].Split("\",");
        return list;
    }

    #endregion

}
