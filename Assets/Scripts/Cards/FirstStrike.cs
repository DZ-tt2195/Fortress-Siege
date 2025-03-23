using UnityEngine;

public class FirstStrike : AuraCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 4;
        this.extraText = "Start of combat: Your Troop here does an attack.";
        this.artistText = "Michael Watson\nDominion: Cornucopia & Guilds\n(Joust)";
    }

    public override void StartOfCombat(Entity entity, int logged)
    {
        MovingTroop troop = Manager.inst.FindMyTroop(entity.player, entity.currentRow);
        if (troop != null)
            troop.Attack(true, logged);
    }
}
