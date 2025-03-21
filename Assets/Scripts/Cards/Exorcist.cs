using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Exorcist : TroopCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 5;
        this.power = 3;
        this.health = 3;
        this.extraText = "When you play this: Bounce an Aura.";
        this.artistText = "Joshua Stewart\nDominion: Nocturne\n(Exorcist)";
    }

    public override void DonePlaying(Player player, Entity createdEntity, int logged)
    {
        Log.inst.RememberStep(this, StepType.UndoPoint, () => CardDecision(player, createdEntity, logged));
    }

    void CardDecision(Player player, Entity createdEntity, int logged)
    { 
        List<Row> withAuras = Manager.inst.allRows.Where(row => row.auraHere != null).ToList();

        if (player.myType == PlayerType.Bot)
        {
            if (player.chainTracker < player.currentChain.decisions.Count)
            {
                int next = player.currentChain.decisions[player.chainTracker];
                //Debug.Log($"resolved continue turn with choice {next}");
                player.inReaction.Add(Excavation);
                player.DecisionMade(next);
            }
            else
            {
                player.NewChains(player.RowsToInts(withAuras));
            }
        }
        else
        {
            if (withAuras.Count == 0)
            {
                Log.inst.undoToThis = null;
                base.DonePlaying(player, createdEntity, logged);
                player.DecisionMade(-1);
            }
            else
            {
                player.ChooseRow(withAuras, "Bounce an Aura.", null);
            }
        }

        void Excavation()
        {
            if (player.choice >= 0)
            {
                Row toRemove = Manager.inst.allRows[player.choice];
                MovingAura aura = toRemove.auraHere;
                aura.player.BounceCardRPC(aura, logged);
            }
            else
            {
                Log.inst.PreserveTextRPC($"{this.name} doesn't bounce an Aura.", logged);
            }
            base.DonePlaying(player, createdEntity, logged);
        }
    }
}
