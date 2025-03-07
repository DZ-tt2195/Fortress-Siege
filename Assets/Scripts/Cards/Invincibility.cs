using UnityEngine;

public class Invincibility : AuraCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 5;
        this.extraText = "Start of combat: Your Troop here becomes Shielded.";
    }

    public override void StartOfCombat(Entity entity, int logged)
    {
        MovingTroop myTroop = Manager.inst.FindMyTroop(entity.player, entity.currentRow);
        if (myTroop != null && !myTroop.shielded)
            myTroop.ShieldStatusRPC(true, logged, this.name);
    }
}
