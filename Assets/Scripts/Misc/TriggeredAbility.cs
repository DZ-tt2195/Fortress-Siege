using System;
using UnityEngine;

public class TriggeredAbility
{
    Func<string, object[], bool> CanBeTriggered;
    Func<int, object[], object> WhenTriggered;
    public PhotonCompatible source { get; protected set; }

    protected TriggeredAbility(PhotonCompatible source, Func<string, object[], bool> condition, Func<int, object[], object> ability)
    {
        this.source = source;
        CanBeTriggered = condition;
        WhenTriggered = ability;
    }

    public bool CheckAbility(string condition, object[] parameters = null)
    {
        try
        {
            return CanBeTriggered(condition, parameters);
        }
        catch
        {
            return false;
        }
    }

    public object ResolveAbility(int logged, object[] parameters = null)
    {
        return WhenTriggered(logged, parameters);
    }
}
