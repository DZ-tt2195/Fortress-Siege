using UnityEngine;

public class Spirit : TroopCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 2;
        this.power = 3;
        this.health = 1;
        this.extraText = "If there's an Aura here, this gets +2 Health.";
        this.artistText = "Julien Delval\nDominion: Nocturne\n(Will-o'-Wisp)";
    }

    public override (int, int) PassiveStats(MovingTroop troop, MovingAura enviro = null)
    {
        if (Manager.inst.allRows[troop.currentRow].auraHere == null)
            return (0, 0);
        else
            return (0, 2);
    }
}
