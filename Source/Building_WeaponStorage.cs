﻿using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Verse;

namespace WeaponStorage
{
    public class Building_WeaponStorage : Building_Storage, IStoreSettingsParent
    {

        private List<ThingWithComps> storedWeapons = new List<ThingWithComps>();
        private Map CurrentMap { get; set; }

        static Building_WeaponStorage()
        {
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            this.CurrentMap = map;

            if (settings == null)
            {
                base.settings = new StorageSettings(this);
                base.settings.CopyFrom(this.def.building.defaultStorageSettings);
                base.settings.filter.SetDisallowAll();
            }
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            try
            {
                this.Dispose();
                base.Destroy(mode);
            }
            catch (Exception e)
            {
                Log.Error(
                    this.GetType().Name + ".Destroy\n" +
                    e.GetType().Name + " " + e.Message + "\n" +
                    e.StackTrace);
            }
        }

        public override void Discard()
        {
            try
            {
                this.Dispose();
                base.Discard();
            }
            catch (Exception e)
            {
                Log.Error(
                    this.GetType().Name + ".Discard\n" +
                    e.GetType().Name + " " + e.Message + "\n" +
                    e.StackTrace);
            }
        }

        public override void DeSpawn()
        {
            try
            {
                this.Dispose();
                base.DeSpawn();
            }
            catch (Exception e)
            {
                Log.Error(
                    this.GetType().Name + ".DeSpawn\n" +
                    e.GetType().Name + " " + e.Message + "\n" +
                    e.StackTrace);
            }
        }

        private void Dispose()
        {
            try
            {
                if (this.StoredWeapons != null)
                {
                    DropWeapons(this.StoredWeapons);
                    this.StoredWeapons.Clear();
                }
            }
            catch (Exception e)
            {
                Log.Error(
                    this.GetType().Name + ".Dispose\n" +
                    e.GetType().Name + " " + e.Message + "\n" +
                    e.StackTrace);
            }
        }

        private void DropWeapons(List<ThingWithComps> weapons, bool makeForbidden = true)
        {
            try
            {
                if (weapons != null)
                {
                    foreach (ThingWithComps t in weapons)
                    {
                        this.DropThing(t, makeForbidden);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(
                    this.GetType().Name + ".DropApparel\n" +
                    e.GetType().Name + " " + e.Message + "\n" +
                    e.StackTrace);
            }
        }

        private Random random = null;
        private void DropThing(Thing a, bool makeForbidden = true)
        {
            try
            {
                Thing t;
                if (!a.Spawned)
                {
                    GenThing.TryDropAndSetForbidden(a, base.Position, this.CurrentMap, ThingPlaceMode.Near, out t, makeForbidden);
                    if (!a.Spawned)
                    {
                        GenPlace.TryPlaceThing(a, base.Position, this.CurrentMap, ThingPlaceMode.Near);
                    }
                }
                if (a.Position.Equals(base.Position))
                {
                    IntVec3 pos = a.Position;
                    if (this.random == null)
                        this.random = new System.Random();
                    int dir = this.random.Next(2);
                    int amount = this.random.Next(2);
                    if (amount == 0)
                        amount = -1;
                    if (dir == 0)
                        pos.x = pos.x + amount;
                    else
                        pos.z = pos.z + amount;
                    a.Position = pos;
                }
            }
            catch (Exception e)
            {
                Log.Error(
                    this.GetType().Name + ".DropApparel\n" +
                    e.GetType().Name + " " + e.Message + "\n" +
                    e.StackTrace);
            }
        }

        public override void Notify_ReceivedThing(Thing newItem)
        {
            if (!(newItem is ThingWithComps) || !((ThingWithComps)newItem).def.IsWeapon)
            {
                DropThing(newItem);
                return;
            }

            base.Notify_ReceivedThing(newItem);
            if (!this.storedWeapons.Contains((ThingWithComps)newItem))
            {
                if (newItem.Spawned)
                    newItem.DeSpawn();
                this.storedWeapons.Add((ThingWithComps)newItem);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Collections.Look(ref this.storedWeapons, "storedWeapons", LookMode.Deep, new object[0]);
        }

        public override string GetInspectString()
        {
            this.Tick();
            StringBuilder sb = new StringBuilder(base.GetInspectString());
            sb.Append("WeaponStorage.StoragePriority".Translate());
            sb.Append(": ");
            sb.Append(("StoragePriority" + base.settings.Priority).Translate());
            sb.Append("\n");
            sb.Append("WeaponStorage.Count".Translate());
            sb.Append(": ");
            sb.Append(this.storedWeapons.Count);
            return sb.ToString();
        }

        public List<ThingWithComps> StoredWeapons
        {
            get
            {
                if (this.storedWeapons == null)
                    this.storedWeapons = new List<ThingWithComps>();
                return this.storedWeapons;
            }
            set
            {
                this.storedWeapons = value;
                if (this.storedWeapons == null)
                    this.storedWeapons = new List<ThingWithComps>();
            }
        }

        public void Remove(ThingWithComps weapon, bool forbidden = true)
        {
            try
            {
                this.DropThing(weapon, forbidden);
                this.storedWeapons.Remove(weapon);
            }
            catch (Exception e)
            {
                Log.Error(
                    this.GetType().Name + ".Remove(ThingWithComp)\n" +
                    e.GetType().Name + " " + e.Message + "\n" +
                    e.StackTrace);
            }
        }

        private readonly Stopwatch stopWatch = new Stopwatch();
        public override void TickLong()
        {
            try
            {
                if (!this.stopWatch.IsRunning)
                    this.stopWatch.Start();
                else
                {
                    // Do this every minute
                    if (this.stopWatch.ElapsedMilliseconds > 60000)
                    {
                        for (int i = this.storedWeapons.Count - 1; i >= 0; --i)
                        {
                            ThingWithComps t = this.storedWeapons[i];
                            if (!this.settings.filter.Allows(t))
                            {
                                this.DropThing(t, false);
                                this.storedWeapons.RemoveAt(i);
                            }
                        }
                        this.stopWatch.Reset();
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(
                    this.GetType().Name + ".TickLong\n" +
                    e.GetType().Name + " " + e.Message + "\n" +
                    e.StackTrace);
            }
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            IEnumerable<Gizmo> enumerables = base.GetGizmos();

            List<Gizmo> l;
            if (enumerables != null)
                l = new List<Gizmo>(enumerables);
            else
                l = new List<Gizmo>(1);

            int groupKey = 987767542;

            Command_Action a = new Command_Action();
            a.icon = ContentFinder<UnityEngine.Texture2D>.Get("UI/assignweapons", true);
            a.defaultDesc = "WeaponStorage.AssignWeaponsDesc".Translate();
            a.defaultLabel = "WeaponStorage.AssignWeapons".Translate();
            a.activateSound = SoundDef.Named("Click");
            a.action = delegate { Find.WindowStack.Add(new UI.AssignUI(this)); };
            a.groupKey = groupKey;
            l.Add(a);

            a = new Command_Action();
            a.icon = ContentFinder<UnityEngine.Texture2D>.Get("UI/empty", true);
            a.defaultDesc = "WeaponStorage.EmptyDesc".Translate();
            a.defaultLabel = "WeaponStorage.Empty".Translate();
            a.activateSound = SoundDef.Named("Click");
            a.action =
                delegate
                {
                    this.DropWeapons(this.storedWeapons, false);
                    this.storedWeapons.Clear();
                };
            a.groupKey = groupKey + 1;
            l.Add(a);

            return SaveStorageSettingsUtil.SaveStorageSettingsGizmoUtil.AddSaveLoadGizmos(l, "Weapon_Management", this.settings.filter);
        }
    }
}