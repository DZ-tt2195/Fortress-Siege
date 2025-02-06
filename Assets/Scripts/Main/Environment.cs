using UnityEngine;
using Photon.Pun;

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
                this.name = myCard.name;
                this.image.sprite = Resources.Load<Sprite>($"Card Art/{this.name}");
            }
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

        Recalculate(oldPosition);
        Recalculate(newPosition);

        void Recalculate(int row)
        {
            if (row > -1)
            {
                foreach (MovingTroop troop in Manager.inst.allRows[row].playerTroops)
                {
                    if (troop != null)
                        troop.RecalculateStats(logged);
                }
            }
        }
    }

    #endregion

}