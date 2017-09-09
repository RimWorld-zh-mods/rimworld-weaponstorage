﻿using RimWorld;
using UnityEngine;
using Verse;
using System;
using System.Collections.Generic;

namespace WeaponStorage.UI
{
    public class AssignUI : Window
    {
        public enum ApparelFromEnum { Pawn, Storage };
        private readonly Building_WeaponStorage weaponStorage;

        private List<WeaponSelected> selectedWeapons;
        Vector2 scrollPosition = new Vector2(0, 0);

        private Pawn selectedPawn = null;
        private string SelectedPawnName
        {
            get
            {
                if (this.selectedPawn != null)
                {
                    return this.selectedPawn.Name.ToStringShort;
                }
                return null;
            }
        }

        private Dictionary<string, Pawn> pawnLookup = null;
        private List<Pawn> selectablePawns = null;
        private List<Pawn> PlayerPawns
        {
            get
            {
                if (selectablePawns == null)
                {
                    selectablePawns = new List<Pawn>();
                    pawnLookup = new Dictionary<string, Pawn>();
                    foreach (Pawn p in PawnsFinder.AllMapsAndWorld_Alive)
                    {
                        if (p.Faction == Faction.OfPlayer && p.def.defName.Equals("Human"))
                        {
                            selectablePawns.Add(p);
                            pawnLookup.Add(p.ThingID, p);
                        }
                    }

                    for (int i = AssignedWeaponContainer.AssignedWeapons.Count - 1; i >= 0; --i)
                    {
                        bool delete = false;
                        Pawn pawn = null;
                        if (!pawnLookup.TryGetValue(AssignedWeaponContainer.AssignedWeapons[i].PawnId, out pawn))
                        {
                            delete = true;
                        }
                        else if (pawn.Dead)
                        {
                            delete = true;
                        }

                        if (delete)
                        {
                            this.weaponStorage.StoredWeapons.AddRange(AssignedWeaponContainer.AssignedWeapons[i].Weapons);
                            AssignedWeaponContainer.AssignedWeapons.RemoveAt(i);
                        }
                    }
                }
                return selectablePawns;
            }
        }

        public AssignUI(Building_WeaponStorage weaponStorage)
        {
            this.weaponStorage = weaponStorage;

            this.selectedWeapons = null;

            this.closeOnEscapeKey = true;
            this.doCloseButton = true;
            this.doCloseX = true;
            this.absorbInputAroundWindow = true;
            this.forcePause = true;
        }

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(650f, 600f);
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            try
            {
                Widgets.Label(new Rect(0, 0, 150, 30), "WeaponStorage.AssignTo".Translate());
                if (Widgets.ButtonText(new Rect(175, 0, 150, 30), this.SelectedPawnName))
                {
                    List<FloatMenuOption> options = new List<FloatMenuOption>();
                    foreach (Pawn p in PlayerPawns)
                    {
                        options.Add(new FloatMenuOption(p.Name.ToStringShort, delegate
                        {
                            if (this.selectedPawn != null)
                            {
                                this.SetAssignedWeapons(this.selectedPawn, this.selectedWeapons);
                                this.selectedWeapons.Clear();
                            }

                            this.selectedPawn = p;

                            AssignedWeaponContainer assignedWeapons;
                            if (!AssignedWeaponContainer.TryGetAssignedWeapons(p, out assignedWeapons))
                            {
                                assignedWeapons = new AssignedWeaponContainer();
                                assignedWeapons.PawnId = p.ThingID;
                            }

                            this.selectedWeapons = new List<WeaponSelected>(assignedWeapons.Weapons.Count + this.weaponStorage.StoredWeapons.Count + 1);
                            foreach (ThingWithComps t in assignedWeapons.Weapons)
                            {
                                if (t != null)
                                {
                                    this.selectedWeapons.Add(new WeaponSelected(t, true));
                                }
                            }
                            foreach (ThingWithComps t in this.weaponStorage.StoredWeapons)
                            {
                                if (t != null)
                                {
                                    this.selectedWeapons.Add(new WeaponSelected(t, false));
                                }
                            }
                            ThingWithComps primary = p.equipment.Primary;
                            if (primary != null)
                            {
                                this.selectedWeapons.Add(new WeaponSelected(primary, true));
                            }
                        }, MenuOptionPriority.Default, null, null, 0f, null, null));
                    }
                    Find.WindowStack.Add(new FloatMenu(options));
                }
                
                int count = (this.selectedWeapons != null) ? this.selectedWeapons.Count : ((this.weaponStorage.StoredWeapons != null) ? this.weaponStorage.StoredWeapons.Count : 0);
                Rect r = new Rect(0, 0, 334, count * 26);
                scrollPosition = GUI.BeginScrollView(new Rect(40, 50, 350, 400), scrollPosition, r);
                r = r.ContractedBy(4);
                Listing_Standard lst = new Listing_Standard();
                lst.Begin(r);

                if (this.selectedWeapons != null)
                {
                    for (int i = 0; i < this.selectedWeapons.Count; ++i)
                    {
                        WeaponSelected selected = this.selectedWeapons[i];
                        bool isChecked = selected.isChecked;
                        lst.CheckboxLabeled(selected.thing.Label, ref isChecked);
                        selected.isChecked = isChecked;
                        this.selectedWeapons[i] = selected;
                    }
                }
                else if (this.weaponStorage.StoredWeapons != null)
                {
                    for (int i = 0; i < this.weaponStorage.StoredWeapons.Count; ++i)
                    {
                        ThingWithComps t = this.weaponStorage.StoredWeapons[i];
                        lst.Label(t.Label);
                    }
                }

                lst.End();
                GUI.EndScrollView();
            }
            catch (Exception e)
            {
                String msg = this.GetType().Name + " closed due to: " + e.GetType().Name + " " + e.Message;
                Log.Error(msg);
                Messages.Message(msg, MessageSound.Negative);
                base.Close();
            }
            finally
            {
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
            }
        }

        public override void PostClose()
        {
            base.PostClose();
            this.SetAssignedWeapons(this.selectedPawn, this.selectedWeapons);

            if (this.pawnLookup != null)
            {
                this.pawnLookup.Clear();
                this.pawnLookup = null;
            }
            if (this.selectablePawns != null)
            {
                this.selectablePawns.Clear();
                this.selectablePawns = null;
            }
        }

        private void SetAssignedWeapons(Pawn p, List<WeaponSelected> weapons)
        {
            if (p == null || weapons == null)
            {
                return;
            }
            
            AssignedWeaponContainer assignedWeapons;
            if (!AssignedWeaponContainer.TryGetAssignedWeapons(p, out assignedWeapons))
            {
                assignedWeapons = new AssignedWeaponContainer();
                assignedWeapons.PawnId = p.ThingID;
            }
            assignedWeapons.Weapons.Clear();
            this.weaponStorage.StoredWeapons.Clear();

            ThingWithComps primary = p.equipment.Primary;
            foreach (WeaponSelected selected in weapons)
            {
                if (primary != null && 
                    selected.thing.thingIDNumber == primary.thingIDNumber)
                {
                    if (!selected.isChecked)
                    {
                        p.equipment.Remove(primary);
                    }
                    // else - Do Nothing
                }
                else if (selected.isChecked)
                {
                    
                    assignedWeapons.Weapons.Add(selected.thing);
                }
                else
                {
                    this.weaponStorage.StoredWeapons.Add(selected.thing);
                }
            }

            if (assignedWeapons.Weapons.Count == 0)
            {
                AssignedWeaponContainer.Remove(p);
            }
            else
            {
                AssignedWeaponContainer.Set(assignedWeapons);
            }
        }
    }
}
