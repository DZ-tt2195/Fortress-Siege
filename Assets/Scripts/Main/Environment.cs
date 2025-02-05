using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class Environment : PhotonCompatible, IPointerClickHandler
{

#region Setup

    protected Image image;
    Image border;

    public EnviroCard myCard { get; private set; }
    public Player player { get; protected set; }
    public int currentRow { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        currentRow = -1;
        try
        {
            image = this.transform.Find("Art Box").GetComponent<Image>();
            border = this.transform.Find("border").GetComponent<Image>();
        }
        catch { }
    }

    [PunRPC]
    internal void AssignCardInfo(bool undo, int playerPosition, int cardID)
    {
        if (!undo)
        {
            //Debug.Log("assigned card info");
            this.player = Manager.inst.playersInOrder[playerPosition];
            this.image.transform.localScale = new(playerPosition == 0 ? 1 : -1, 1, 1);
            if (border != null)
                border.color = this.player.playerPosition == 0 ? Color.white : Color.black;

            if (cardID >= 0)
            {
                myCard = PhotonView.Find(cardID).GetComponent<EnviroCard>();
                this.name = myCard.name;
                this.image.sprite = Resources.Load<Sprite>($"Card Art/{this.name}");
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right && myCard != null)
        {
            CarryVariables.inst.RightClickDisplay(this.myCard, 1);
        }
    }

    #endregion

#region Gameplay

    [PunRPC]
    internal void MoveEnviro(bool undo, int oldPosition, int newPosition, int logged)
    {
        if (undo)
        {
            if (newPosition > -1)
                Manager.inst.allRows[newPosition].environment = null;

            this.currentRow = oldPosition;
        }
        else
        {
            if (oldPosition > -1)
                Manager.inst.allRows[oldPosition].environment = null;

            this.currentRow = newPosition;
            Log.inst.AddText($"{player.name} moves {this.name} to row {newPosition + 1}.", logged);
        }

        if (currentRow > -1)
        {
            player.availableEnviros.Remove(this);
            Row spawnPoint = Manager.inst.allRows[currentRow];
            spawnPoint.environment = this;

            this.transform.SetParent(spawnPoint.button.transform);
            this.transform.localPosition = new((player.playerPosition) == 0 ? -100 : 100, 0);
            this.transform.localScale = Vector3.one;
        }
        else
        {
            player.availableEnviros.Add(this);
            this.transform.SetParent(null);
            if (!undo)
                Log.inst.AddText($"{player.name}'s {this.name} has been replaced.", logged);
        }
    }

    #endregion

}
