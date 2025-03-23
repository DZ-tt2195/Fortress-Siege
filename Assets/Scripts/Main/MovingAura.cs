using UnityEngine;
using Photon.Pun;
using System.Text.RegularExpressions;

public class MovingAura : Entity
{

#region Setup

    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
    }

    public void AssignCardRPC(Player newPlayer, Card newCard)
    {
        Log.inst.RememberStep(this, StepType.Revert, () => AssignCardInfo
        (false, newPlayer.playerPosition, (myCard == null) ? -1 : myCard.pv.ViewID, newCard.pv.ViewID));
    }

    [PunRPC]
    void AssignCardInfo(bool undo, int playerPosition, int oldCard, int newCard)
    {
        this.player = Manager.inst.playersInOrder[playerPosition];
        this.image.transform.localScale = new(playerPosition == 0 ? 1 : -1, 1, 1);
        if (border != null)
            border.color = this.player.playerPosition == 0 ? Color.white : Color.black;

        if (undo && oldCard < 0) return;
        myCard = PhotonView.Find(undo ? oldCard : newCard).GetComponent<AuraCard>();

        this.name = myCard.name;
        this.image.sprite = Resources.Load<Sprite>($"Card Art/{this.name}");
    }

    #endregion

#region Gameplay

    public override void MoveEntityRPC(int newPosition, int logged)
    {
        int oldRow = this.currentRow;
        Log.inst.RememberStep(this, StepType.Revert, () => MoveEnviro(false, oldRow, newPosition, logged));
    }

    [PunRPC]
    void MoveEnviro(bool undo, int oldPosition, int newPosition, int logged)
    {
        if (undo)
        {
            if (newPosition > -1)
                Manager.inst.allRows[newPosition].auraHere = null;

            this.currentRow = oldPosition;
        }
        else
        {
            if (oldPosition > -1)
                Manager.inst.allRows[oldPosition].auraHere = null;

            this.currentRow = newPosition;
            if (currentRow >= 0)
                Log.inst.AddTextRPC($"{player.name} moves {this.name} to row {newPosition + 1}.", LogAdd.Personal, logged);
            else
                Log.inst.AddTextRPC($"{player.name}'s {this.name} is destroyed.", LogAdd.Personal, logged);
        }

        if (currentRow > -1)
        {
            player.availableEnviros.Remove(this);
            Row spawnPoint = Manager.inst.allRows[currentRow];
            spawnPoint.auraHere = this;

            this.transform.SetParent(spawnPoint.button.transform);
            this.transform.localPosition = new((player.playerPosition) == 0 ? -100 : 100, 0);
            this.transform.localScale = Vector3.one;
        }
        else
        {
            player.availableEnviros.Add(this);
            this.transform.SetParent(null);
        }

        Recalculate(oldPosition);
        Recalculate(newPosition);

        void Recalculate(int row)
        {
            if (row > -1)
            {
                foreach (MovingTroop troop in Manager.inst.allRows[row].playerTroops)
                {
                    if (troop != null)
                        troop.RecalculateStats();
                }
            }
        }
    }

    #endregion

}