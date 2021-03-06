﻿using System.Collections.Generic;
using Verse;

namespace WeaponStorage
{
    public class AssignedWeaponContainer : IExposable
    {
        public string PawnId = "";
        public List<ThingWithComps> Weapons = new List<ThingWithComps>();

        public void ExposeData()
        {
            Scribe_Values.Look(ref this.PawnId, "pawn", "", true);
            Scribe_Collections.Look(ref this.Weapons, "weapons", LookMode.Deep, new object[0]);
        }
    }
}
