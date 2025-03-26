using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Photon.Pun;

public class PlayerBase : Entity
{
    TMP_Text myText;
    TMP_Text heartText;
    public int myHealth { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        heartText = this.transform.Find("Heart Text").GetComponent<TMP_Text>();
        myText = this.transform.Find("Personal Text").GetComponent<TMP_Text>();
    }

    public void UpdateText()
    {
        heartText.text = $"{myHealth}";
        myText.text = KeywordTooltip.instance.EditText($"{player.name}\n{player.coins} Coin\n{player.cardsInHand.Count} Card");
    }

    internal void AssignPlayer(Player player, int health)
    {
        this.player = player;
        this.name = player.name;
        this.myHealth = health;

        this.transform.SetParent(Manager.inst.canvas.transform);
        this.transform.localScale = Vector3.Lerp(Vector3.one, Manager.inst.canvas.transform.localScale, 0.5f);
        this.image.color = (player.playerPosition == 0) ? Color.red : Color.blue;
        this.transform.localPosition = new(player.playerPosition == 0 ? -1075 : 400, 225);
        image.transform.localEulerAngles = new(0, 0, player.playerPosition == 0 ? 90 : -90);

        UpdateText();
    }

    public void ChangeHealthRPC(int health, int logged, string source = "")
    {
        Log.inst.RememberStep(this, StepType.Revert, () => ChangeHealth(false, health, logged, source));
    }

    [PunRPC]
    void ChangeHealth(bool undo, int health, int logged, string source)
    {
        string parathentical = source == "" ? "" : $" ({source})";
        if (undo)
        {
            myHealth -= health;
        }
        else
        {
            myHealth += health;
            if (health > 0)
                Log.inst.AddTextRPC($"{player.name} gets +{health} Health{parathentical}.", LogAdd.Personal, logged);
            else if (health < 0)
                Log.inst.AddTextRPC($"{player.name} loses {Mathf.Abs(health)} Health{parathentical}.", LogAdd.Personal, logged);
        }
        UpdateText();
    }
}
