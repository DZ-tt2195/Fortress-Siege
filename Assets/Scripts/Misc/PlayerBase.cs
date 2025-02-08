using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Photon.Pun;

public class PlayerBase : Entity
{
    TMP_Text myText;
    public int myHealth { get; private set; }

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
        this.myHealth = health;

        this.transform.SetParent(Manager.inst.canvas.transform);
        this.image.color = (player.playerPosition == 0) ? Color.white : Color.black;
        myText.color = (player.playerPosition == 0) ? Color.black : Color.white;
        this.transform.localPosition = new(player.playerPosition == 0 ? -1100 : 470, 225);

        UpdateText();
    }

    public void ChangeHealthRPC(int health, int logged)
    {
        Log.inst.RememberStep(this, StepType.Revert, () => ChangeHealth(false, health, logged));
    }

    [PunRPC]
    void ChangeHealth(bool undo, int health, int logged)
    {
        if (undo)
        {
            myHealth -= health;
        }
        else
        {
            myHealth += health;
            if (health > 0)
                Log.inst.AddText($"{player.name} gets +{health} Health.", logged);
            else if (health < 0)
                Log.inst.AddText($"{player.name} loses {Mathf.Abs(health)} Health.", logged);
        }
    }
}
