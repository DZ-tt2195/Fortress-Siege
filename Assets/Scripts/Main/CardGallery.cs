using System.Collections.Generic;
using UnityEngine;
using MyBox;
using TMPro;
using UnityEngine.UI;
using System;
using System.Linq;
using Photon.Pun;
using System.Text.RegularExpressions;

public class CardGallery : MonoBehaviour
{

#region Setup

    [SerializeField] TMP_Text searchResults;
    [SerializeField] RectTransform storeCards;
    [SerializeField] TMP_InputField searchInput;
    [SerializeField] Scrollbar cardScroll;
    List<Card> allCards = new();

    private void Start()
    {
        searchInput.onValueChanged.AddListener(ChangeSearch);

        foreach (string cardName in CarryVariables.inst.cardScripts)
        { 
            GameObject nextObject = Instantiate(CarryVariables.inst.cardPrefab.gameObject);
            Type type = Type.GetType(cardName);
            nextObject.AddComponent(type);
            nextObject.name = Regex.Replace(cardName, "(?<=[a-z])(?=[A-Z])", " ");

            Card card = nextObject.GetComponent<Card>();
            card.layout.FillInCards(card);
            allCards.Add(card);
        }

        SearchCards();
    }

    #endregion

#region Card Search

    bool CompareStrings(string searchBox, string comparison)
    {
        if (searchBox.IsNullOrEmpty())
            return true;
        return (comparison.IndexOf(searchBox, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    void ChangeSearch(string text)
    {
        SearchCards();
    }

    void ChangeDropdown(int n)
    {
        SearchCards();
    }

    void SearchCards()
    {
        foreach (Card card in allCards)
        {
            bool matches = (CompareStrings(searchInput.text, card.extraText) || CompareStrings(searchInput.text, card.name));
            card.transform.SetParent(matches ? storeCards : null);
            if (matches) card.transform.SetAsLastSibling();
        }

        storeCards.transform.localPosition = new Vector3(0, -1050, 0);
        storeCards.sizeDelta = new Vector3(2560, Math.Max(750, 250 * (3+Mathf.Ceil(storeCards.childCount / 8f))));
        searchResults.text = $"Found {storeCards.childCount} Cards";
    }

    #endregion

}
