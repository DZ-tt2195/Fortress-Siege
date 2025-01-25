using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PlayerBase : MovingTroop
{
    TMP_Text myText;

    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        myText = this.transform.Find("Personal Text").GetComponent<TMP_Text>();
    }

    public void UpdateText()
    {
        myText.text = $"{player.name}" +
            $"\n${player.coins}" +
            $"\n{player.cardsInHand.Count} Cards";
    }

    internal void AssignPlayer(Player player, int health)
    {
        this.player = player;
        this.name = player.name;
        this.currentHealth = health;

        this.transform.SetParent(Manager.instance.canvas.transform);
        this.image.color = (player.playerPosition == 0) ? Color.white : Color.black;
        myText.color = (player.playerPosition == 0) ? Color.black : Color.white;
        this.transform.localPosition = new(player.playerPosition == 0 ? -1100 : 225, 225);

        UpdateText();
    }

}
