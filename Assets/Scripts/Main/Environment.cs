using UnityEngine;
using Photon.Pun;
using System.Text.RegularExpressions;

public class Environment : Entity
{

#region Setup

    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
    }

    [PunRPC]
    internal void AssignCardInfo(bool undo, int playerPosition, int cardID)
    {
        if (!undo)
        {
            this.player = Manager.inst.playersInOrder[playerPosition];
            this.image.transform.localScale = new(playerPosition == 0 ? 1 : -1, 1, 1);
            if (border != null)
                border.color = this.player.playerPosition == 0 ? Color.white : Color.black;

            if (cardID >= 0)
            {
                myCard = PhotonView.Find(cardID).GetComponent<EnviroCard>();
                this.name = Regex.Replace(myCard.name, "(?<=[a-z])(?=[A-Z])", " ");
                this.image.sprite = Resources.Load<Sprite>($"Card Art/{this.name}");
            }
        }
    }

    #endregion

#region Gameplay

    public void MoveEnviroRPC(int newPosition, int logged)
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
                Manager.inst.allRows[newPosition].environment = null;

            this.currentRow = oldPosition;
        }
        else
        {
            if (oldPosition > -1)
                Manager.inst.allRows[oldPosition].environment = null;

            this.currentRow = newPosition;
            if (currentRow >= 0)
                Log.inst.AddText($"{player.name} moves {this.name} to row {newPosition + 1}.", logged);
            else
                Log.inst.AddText($"{player.name}'s {this.name} is destroyed.", logged);
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