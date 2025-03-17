using System.Collections.Generic;
using UnityEngine;

public class Commander : TroopCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 4;
        this.power = 3;
        this.health = 1;
        this.extraText = "When you play this: One of your Troops does an attack.";
        this.artistText = "Julien Delval\nDominion: Plunder\n(First Mate)";
    }

    public override void DonePlaying(Player player, Entity createdEntity, int logged)
    {
        Log.inst.RememberStep(this, StepType.UndoPoint, () => CardDecision(player, createdEntity, logged));
    }

    void CardDecision(Player player, Entity createdEntity, int logged)
    {
        List<Row> withTroops = player.FilterRows(true);

        if (player.myType == PlayerType.Bot)
        {
            if (player.chainTracker < player.currentChain.decisions.Count)
            {
                int next = player.currentChain.decisions[player.chainTracker];
                player.inReaction.Add(DoAttack);
                player.DecisionMade(next);
            }
            else
            {
                player.NewChains(player.RowsToInts(withTroops));
            }
        }
        else
        {
            player.ChooseRow(withTroops, "Choose one of your troops to do an attack.", DoAttack);
        }

        void DoAttack()
        {
            Row targetRow = Manager.inst.allRows[player.choice];
            MovingTroop targetTroop = targetRow.playerTroops[player.playerPosition];
            targetTroop.Attack(true, logged);
            base.DonePlaying(player, createdEntity, logged);
        }
    }
}
