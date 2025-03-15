using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Exorcist : TroopCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 4;
        this.power = 1;
        this.health = 3;
        this.extraText = "When you play this: You may destroy an Aura.";
        this.artistText = "Joshua Stewart\nDominion: Nocturne\n(Exorcist)";
    }

    public override void DonePlaying(Player player, Entity createdEntity, int logged)
    {
        Log.inst.RememberStep(this, StepType.UndoPoint, () => CardDecision(player, createdEntity, logged));
    }

    void CardDecision(Player player, Entity createdEntity, int logged)
    { 
        List<Row> withAuras = Manager.inst.allRows.Where(row => row.auraHere != null).ToList();
        List<string> actions = new() { $"Decline" };

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
                //Debug.Log($"{chainTracker}, {currentChain.decisions.Count}");
                player.NewChains(-1, withAuras.Count, 1);
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
                player.ChooseButton(actions, Vector3.zero, "Destroy an Aura?", Excavation);
                player.ChooseRow(withAuras, "Destroy an Aura?", null);
            }
        }

        void Excavation()
        {
            if (player.choice < withAuras.Count)
            {
                Row toRemove = withAuras[player.choice];
                MovingAura enviro = toRemove.auraHere;
                enviro.DestroyEntityRPC(logged);
            }
            else
            {
                Log.inst.PreserveTextRPC($"{this.name} doesn't destroy an Aura.", logged);
            }
            base.DonePlaying(player, createdEntity, logged);
        }
    }
}
