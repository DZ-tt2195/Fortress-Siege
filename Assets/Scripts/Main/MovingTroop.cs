using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEngine.EventSystems;

public class MovingTroop : PhotonCompatible, IPointerClickHandler
{

#region Setup

    protected Image image;
    Image border;
    TMP_Text damageText;
    TMP_Text heartText;

    public TroopCard myCard { get; private set; }
    public Player player { get; protected set; }

    int _currentHealth;
    public int currentHealth
    {
        get { return _currentHealth; }
        protected set { _currentHealth = value; try { heartText.text = _currentHealth.ToString(); } catch { } }
    }
    int _damageStat;

    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        try
        {
            heartText = this.transform.Find("Heart Text").GetComponent<TMP_Text>();
            image = this.transform.Find("Art Box").GetComponent<Image>();
            damageText = this.transform.Find("Damage Text").GetComponent<TMP_Text>();
            border = this.transform.Find("border").GetComponent<Image>();
        }
        catch { }
    }

    [PunRPC]
    internal void AssignCardInfo(int cardID)
    {
        if (cardID >= 0)
        {
            myCard = Manager.instance.allCards[cardID].GetComponent<TroopCard>();
            this.name = myCard.name;
            this.image.sprite = Resources.Load<Sprite>($"Card Art/{this.name}");
        }
    }

    [PunRPC]
    internal void ChangePlayer(int playerPosition)
    {
        this.player = Manager.instance.playersInOrder[playerPosition];
        this.image.transform.localScale = new(playerPosition == 0 ? 1 : -1, 1, 1);
        if (border != null)
            border.color = this.player.playerPosition == 0 ? Color.white : Color.black;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right && myCard != null)
        {
            CarryVariables.instance.RightClickDisplay(this.myCard, 1);
        }
    }

    #endregion

#region Gameplay
    /*
    internal void WhoToAttack()
    {
        Player otherPlayer = Manager.instance.OtherPlayer(player);

        if (player.playerPosition == 0 && this.myCard.dataFile.range + this.mySlot.column >= 9)
        {
            this.whoToAttack = otherPlayer.myBase;
            Debug.Log($"{this.name} locks onto {otherPlayer.name}");
            return;
        }
        else if (player.playerPosition == 1 && this.mySlot.column - this.myCard.dataFile.range <= 0)
        {
            this.whoToAttack = otherPlayer.myBase;
            Debug.Log($"{this.name} locks onto {otherPlayer.name}");
            return;
        }

        List<MapSlot> currentSearch = new() { this.mySlot };
        for (int i = 0; i<myCard.dataFile.range; i++)
        {
            List<MapSlot> newSearch = new();
            foreach (MapSlot slot in currentSearch)
                newSearch.AddRange(Manager.instance.EightWayAdjacent(slot));
            currentSearch = newSearch.Distinct().ToList();

            foreach (MapSlot slot in currentSearch)
            {
                if (otherPlayer.HasMyTroop(slot))
                {
                    whoToAttack = slot.troopHere;
                    Debug.Log($"{this.name} locks onto {whoToAttack.name}");
                    return;
                }
            }
        }
    }

    [PunRPC]
    internal void MoveTroop(int newPosition, int logged)
    {
        if (mySlot != null)
            mySlot.troopHere = null;

        MapSlot newSlot = Manager.instance.allSlots[newPosition];
        this.mySlot = newSlot;
        newSlot.troopHere = this;
        Log.instance.AddText($"{player.name} moves {this.name} to {newSlot.PositionToString()}.", logged);

        this.transform.SetParent(newSlot.transform);
        this.transform.SetAsFirstSibling();
        this.transform.localPosition = Vector3.zero;
        this.transform.localScale = Vector3.one;
    }

    protected virtual void Died()
    {
        Log.instance.AddText($"{player.name}'s {this.name} is destroyed.", 1);
        this.mySlot.troopHere = null;

        if (player.InControl())
            myCard.DeathEffect(this);

        Invoke(nameof(DestroyMe), 0.1f);
    }

    public virtual int CalculateDamage()
    {
        Debug.Log((myCard.dataFile.damage + damageStat) * this.currentUnits);
        return (myCard.dataFile.damage + damageStat) * this.currentUnits;
    }
    */
    #endregion

}
