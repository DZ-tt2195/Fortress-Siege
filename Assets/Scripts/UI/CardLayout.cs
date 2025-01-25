using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class CardLayout : MonoBehaviour, IPointerClickHandler
{
    public CanvasGroup cg { get; private set; }
    Image background;
    Image artBox;
    TMP_Text description;
    TMP_Text cardName;
    TMP_Text costText;
    Card myCard;

    private void Awake()
    {
        cg = transform.Find("Canvas Group").GetComponent<CanvasGroup>();
        background = cg.transform.Find("Background").GetComponent<Image>();
        cardName = cg.transform.Find("Card Name").GetComponent<TMP_Text>();
        costText = cg.transform.Find("Coin Text").GetComponent<TMP_Text>();
        description = cg.transform.Find("Card Description").GetComponent<TMP_Text>();
        artBox = cg.transform.Find("Art Box").GetComponent<Image>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            CarryVariables.instance.RightClickDisplay(this.myCard, cg.alpha);
        }
    }

    public void FillInCards(Card card)
    {
        myCard = card;
        try
        {
            artBox.sprite = Resources.Load<Sprite>($"Card Art/{card.name}");
        }
        catch { }
        background.color = card.MyColor();
        description.text = card.TextBox();
        cardName.text = card.name;
        costText.text = $"{card.dataFile.cost}";
    }
}
