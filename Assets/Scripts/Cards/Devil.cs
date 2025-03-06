using System.Collections.Generic;
using UnityEngine;

public class Devil : TroopCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 5;
        this.power = 6;
        this.health = 6;
        this.extraText = "When you play this: Deal 3 damage to one of your Troops (could be this).";
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
                player.inReaction.Add(DealDamage);
                player.DecisionMade(next);
            }
            else
            {
                player.NewChains(0, withTroops.Count, 1);
            }
        }
        else
        {
            player.ChooseRow(withTroops, "Choose one of your troops to deal 3 damage to.", DealDamage);
        }

        void DealDamage()
        {
            Row targetRow = withTroops[player.choice];
            MovingTroop targetTroop = targetRow.playerTroops[player.playerPosition];
            targetTroop.ChangeStatsRPC(0, -3, logged);
            base.DonePlaying(player, createdEntity, logged);
        }
    }
}
