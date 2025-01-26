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
    public int currentRow { get; private set; }

    int _currentHealth;
    public int currentHealth
    {
        get { return _currentHealth; }
        protected set { _currentHealth = value; try { heartText.text = _currentHealth.ToString(); } catch { } }
    }
    int _currentDamage;
    public int currentDamage
    {
        get { return _currentDamage; }
        protected set { _currentDamage = value; try { damageText.text = _currentDamage.ToString(); } catch { } }
    }

    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        currentRow = -1;
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
            myCard = PhotonView.Find(cardID).GetComponent<TroopCard>();
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

    [PunRPC]
    internal void MoveTroop(int newPosition, int logged)
    {
        if (currentRow > -1)
            Manager.instance.allRows[currentRow].playerTroops[player.playerPosition] = null;

        this.currentRow = newPosition;
        Row row = Manager.instance.allRows[currentRow];
        row.playerTroops[player.playerPosition] = this;
        Log.instance.AddText($"{player.name} moves {this.name} to row {newPosition}.", logged);

        this.transform.SetParent(row.button.transform);
        this.transform.localPosition = new((player.playerPosition) == 0 ? -575 : 575, 0);
        this.transform.localScale = Vector3.one;
    }

    #endregion

}
