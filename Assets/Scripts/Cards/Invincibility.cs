using UnityEngine;

public class Invincibility : AuraCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 5;
        this.extraText = "Start of combat: Your Troop here becomes Shielded.";
        this.artistText = "Matt Stewart\nMTG: Ravnica Allegiance\n(Unbreakable Formation)";
    }

    public override void StartOfCombat(Entity entity, int logged)
    {
        MovingTroop myTroop = Manager.inst.FindMyTroop(entity.player, entity.currentRow);
        if (myTroop != null && !myTroop.statusDict[StatusEffect.Shielded])
            myTroop.StatusEffectRPC(StatusEffect.Shielded, true, logged, this.name);
    }
}
