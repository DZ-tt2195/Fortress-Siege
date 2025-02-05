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

        this.transform.SetParent(Manager.inst.canvas.transform);
        this.image.color = (player.playerPosition == 0) ? Color.white : Color.black;
        myText.color = (player.playerPosition == 0) ? Color.black : Color.white;
        this.transform.localPosition = new(player.playerPosition == 0 ? -1100 : 470, 225);

        UpdateText();
    }

    protected override void LogHealth(int healthChange, int logged)
    {
        if (healthChange > 0)
            Log.inst.AddText($"{player.name} gains {healthChange} health.", logged);
        else if (healthChange < 0)
            Log.inst.AddText($"{player.name} loses {Mathf.Abs(healthChange)} health.", logged);
    }
}
