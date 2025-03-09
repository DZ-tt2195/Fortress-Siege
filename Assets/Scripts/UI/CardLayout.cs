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

    Image damageImage;
    TMP_Text damageText;
    Image heartImage;
    TMP_Text heartText;

    Card myCard;

    private void Awake()
    {
        cg = transform.Find("Canvas Group").GetComponent<CanvasGroup>();
        background = cg.transform.Find("Background").GetComponent<Image>();
        cardName = cg.transform.Find("Card Name").GetComponent<TMP_Text>();
        costText = cg.transform.Find("Coin Text").GetComponent<TMP_Text>();
        description = cg.transform.Find("Card Description").GetComponent<TMP_Text>();
        artBox = cg.transform.Find("Art Box").GetComponent<Image>();
        damageImage = cg.transform.Find("Damage Image").GetComponent<Image>();
        damageText = cg.transform.Find("Damage Text").GetComponent<TMP_Text>();
        heartImage = cg.transform.Find("Heart Image").GetComponent<Image>();
        heartText = cg.transform.Find("Heart Text").GetComponent<TMP_Text>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            CarryVariables.inst.RightClickDisplay(this.myCard, cg.alpha);
        }
    }

    public void FillInCards(Card card)
    {
        myCard = card;
        try {artBox.sprite = Resources.Load<Sprite>($"Card Art/{card.name}");} catch { Debug.LogError($"no art for {card.name}"); }
        background.color = card.MyColor();
        description.text = KeywordTooltip.instance.EditText(card.extraText);
        cardName.text = card.name;
        costText.text = $"{card.coinCost}";

        if (card is TroopCard convertedCard)
        {
            damageImage.gameObject.SetActive(true);
            damageText.gameObject.SetActive(true);
            damageText.text = convertedCard.power.ToString();

            heartImage.gameObject.SetActive(true);
            heartText.gameObject.SetActive(true);
            heartText.text = convertedCard.health.ToString();
        }
        else
        {
            damageImage.gameObject.SetActive(false);
            damageText.gameObject.SetActive(false);
            heartImage.gameObject.SetActive(false);
            heartText.gameObject.SetActive(false);
        }
    }
}
