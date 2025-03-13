using UnityEngine;

public class Barbarian : TroopCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 6;
        this.power = 5;
        this.health = 2;
        this.extraText = "Start of combat: All opposing Troops lose 1 Power.";
        this.artistText = "Julien Delval\nDominion: Allies\n(Barbarian)";
    }

    public override void StartOfCombat(Entity entity, int logged)
    {
        for (int i = 0; i<5; i++)
        {
            MovingTroop troop = Manager.inst.FindOpposingTroop(entity.player, i);
            if (troop != null)
                troop.ChangeStatsRPC(-1, 0, logged, this.name);
        }
    }
}
