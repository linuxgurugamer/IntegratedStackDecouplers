using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.UI.Screens;


namespace IntegratedStackedTankDecouplers
{
    public class IntegratedDecoupler : ModuleToggleCrossfeed, IPartMassModifier, IPartCostModifier
    {
        static int cnt = 0;

#if DEBUG
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true)]
#endif
        int lcnt = -1;

        List<IntegratedDecoupler> toInitialize = new List<IntegratedDecoupler>();

        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = false)]
        public bool integratedDecoupler = false;

        [KSPEvent(name = "integratedDecoupler", guiName = "No decoupler", guiActiveEditor = true, active = true)]
        public void ToggleIntegratedDecoupler()
        {
            Log.Info("ToggleIntegratedDecoupler");
            SetAllStatus(!integratedDecoupler);
        }

        //        [KSPField(guiName = "Seperatron Count", isPersistant = true, guiActiveEditor = true),
        //            UI_FloatRange(stepIncrement = 1f, maxValue = 100f, minValue = 0f)]
        // public float seperatronCnt = 0;
        //float oldSepCnt;

        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false)]
        public float decouplerMass = 0.15f;

        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false)]
        public float decouplerCost = 250f;

        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false)]
        public new string techRequired = "";

        AttachNode topNode = null;
        AttachNode topNode01 = null;
        AttachNode topNode02 = null;

        Part lastTopNodeAttachedPart = null;
        BaseEventList pmEvents;

        void SetStatus(bool b, IntegratedDecoupler symIntegratedDecoupler)
        {
            Part counterpart = symIntegratedDecoupler.part;

            ModuleToggleCrossfeed symCrossfeedToggleModule = counterpart.FindModuleImplementing<ModuleToggleCrossfeed>();

            symIntegratedDecoupler.integratedDecoupler = b;
            if (integratedDecoupler)
                enableDecoupler(symIntegratedDecoupler, symCrossfeedToggleModule);
            else
                disableDecoupler(symIntegratedDecoupler, symCrossfeedToggleModule);

            SetEvents(symIntegratedDecoupler.decouplerModule, symCrossfeedToggleModule);
        }

        void SetAllStatus(bool b, bool inEditorModified = false, bool doSymmetry = true)
        {
            //integratedDecoupler = b;
            Log.Info("lcnt: " + lcnt + ",   SetStatus: " + b);
            
            for (int i = 0; i < toInitialize.Count; i++)
            { 
                IntegratedDecoupler symIntegratedDecoupler = toInitialize[i];
                SetStatus(b, symIntegratedDecoupler);     
            }
            

            Log.Info("SetStatus 1");
            if (!inEditorModified)
                GameEvents.onEditorShipModified.Fire(EditorLogic.fetch.ship);
            Log.Info("SetStatus 1");
        }

        void SetEvents(ModuleDecouple decouplerModule, ModuleToggleCrossfeed crossfeedToggleModule)
        {
            crossfeedToggleModule.Events["ToggleEvent"].guiActive = false;
            crossfeedToggleModule.Events["ToggleEvent"].active = false;
            crossfeedToggleModule.Actions["ToggleAction"].active = false;
            crossfeedToggleModule.Actions["EnableAction"].active = false;
            crossfeedToggleModule.Actions["DisableAction"].active = false;

            if (decouplerModule != null)
                decouplerModule.part.UpdateStageability(true, true);
            else
                Log.Error("No ModuleDecouple to update");

        }
        void enableDecoupler(IntegratedDecoupler id, ModuleToggleCrossfeed crossfeedToggleModule)
        {
            Log.Info("lcnt: " + lcnt +  "  enableDecoupler");
            KSPActionParam kap = null;
            base.enabled = true;
            if (id.decouplerModule != null)
            {
                id.decouplerModule.SetStaging(true);
                id.decouplerModule.Events["ToggleStaging"].guiActiveEditor = true; // Only seen when AdvancedTweakables is enabled
                id.decouplerModule.Fields["ejectionForcePercent"].guiActiveEditor = true;
                id.decouplerModule.enabled = true;
                id.decouplerModule.stagingEnabled = true;
                if (crossfeedToggleModule != null)
                    crossfeedToggleModule.DisableAction(kap);

                // if (decouplerModule.part != null)
                id.pmEvents["ToggleIntegratedDecoupler"].guiName = "Integrated Decoupler";
            }
        }

        void disableDecoupler(IntegratedDecoupler id, ModuleToggleCrossfeed crossfeedToggleModule)
        {
            Log.Info("lcnt: " + lcnt + "  disableDecoupler");
            KSPActionParam kap = null;

            if (id.decouplerModule != null)
            {
                id.decouplerModule.SetStaging(false);
                id.decouplerModule.Events["ToggleStaging"].guiActiveEditor = false; // Only seen when AdvancedTweakables is enabled
                id.decouplerModule.Fields["ejectionForcePercent"].guiActiveEditor = false;
                id.decouplerModule.enabled = false;
                id.decouplerModule.stagingEnabled = false;

                if (part != null && part.Modules != null)
                    foreach (PartModule m in part.Modules)
                    {
                        if (!ReferenceEquals(m, id.decouplerModule))
                        {
                            m.SetStaging(false);
                            m.Events["ToggleStaging"].guiActiveEditor = false;
                        }
                    }

                if (crossfeedToggleModule != null)
                    crossfeedToggleModule.EnableAction(kap);

                //if (decouplerModule.part != null  && decouplerModule.part.Events != null)
                id.pmEvents["ToggleIntegratedDecoupler"].guiName = "No decoupler";

            }
        }

        ModuleDecouple decouplerModule = null;
        PartModule decouplerPartModule = null;
        ModuleToggleCrossfeed crossfeedToggleModule = null;

        bool techAvailable
        {
            get
            {
                var b = true;
                if (this.DecouplerRequiresTech())
                    b = this.DecouplerHasTech();
                return b;
            }
        }

        AttachNode GetNode(string nodeId, Part p)
        {
            foreach (var attachNode in p.attachNodes.Where(an => an != null))
            {
                if (p.srfAttachNode != null && attachNode == p.srfAttachNode)
                    continue;
                if (attachNode.id == nodeId)
                    return attachNode;

            }
            return null;
        }


        public override void OnStart(StartState state)
        {
            Log.Info("IntegratedDecoupler.OnStart");
            base.OnStart(state);
            //topNodeChecked = false;

            lcnt = ++cnt;

  

            topNode = GetNode("top", part);
            if (topNode == null)
            {
                topNode01 = GetNode("top01", part);
                if (topNode01 != null)
                    topNode02 = GetNode("top02", part);
            }

            GameEvents.onEditorShipModified.Add(onEditorShipModified);
            GameEvents.onEditorPartPlaced.Add(onEditorPartPlaced);

        }

       // bool isOnEditorShipModified = false;

        private void onEditorPartPlaced(Part part)
        {
            if (part == null || part != this.part) return;
            Log.Info("onEditorPartPlaced, lcnt: " + lcnt);

            decouplerModule = this.part.FindModuleImplementing<ModuleDecouple>();
            decouplerPartModule = decouplerModule;

            crossfeedToggleModule = this.part.FindModuleImplementing<ModuleToggleCrossfeed>();

            ModuleJettison moduleJettison = this.part.FindModuleImplementing<ModuleJettison>();
            pmEvents = Events;

            if (decouplerModule == null)
                Log.Info("ModuleDecouple NOT found");
            else
                Log.Info("ModuleDecouple found");

            // Collect a list of modules to initialize from the part and all its children.
            toInitialize.Clear();
            toInitialize.Add(this);
            CollectToInitializerList(part, toInitialize);

            // Also any symmetry counterparts it may have, and *their* children.
            if (part.symmetryCounterparts != null)
            {
                for (int i = 0; i < part.symmetryCounterparts.Count; ++i)
                {
                    Part counterpart = part.symmetryCounterparts[i];
                    if (!ReferenceEquals(this, counterpart)) // probably unnecessary
                    {
                        CollectToInitializerList(counterpart, toInitialize);
                    }
                }
            }
            Log.Info("onEditorPartPlaced.toInitialize.Count: " + toInitialize.Count);
            if (!techAvailable) // || moduleJettison != null
            {
                this.Events["ToggleIntegratedDecoupler"].guiActiveEditor = false;
                // this.Fields["seperatronCnt"].guiActiveEditor = false;
                Log.Info("Disabling IntegratedDecoupler due to no tech");

                SetStatus(false, this);
 
                return;
            }
            Log.Info("lcnt: " + lcnt + ",  Doing initial SetStatus");
            SetStatus(false, this);
            lastTopNodeAttachedPart = null;

            EditorLogic.fetch.SetBackup();


            Log.Info("OnEditorPartPlaced, lcnt: " + lcnt + ",  toInitialize.Count: " + toInitialize.Count);
        }

        /// <summary>
        /// Scan the provided part and all its children recursively, looking for any of them that have
        /// a ModuleVesselCategorizer.  Add any found modules to the list.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="toInitialize"></param>
        private static void CollectToInitializerList(Part root, List<IntegratedDecoupler> toInitialize)
        {
            if (root == null) return;

            for (int i = 0; i < root.symmetryCounterparts.Count; ++i)
            {
                Part counterpart = root.symmetryCounterparts[i];
                if (!ReferenceEquals(root, counterpart))
                {
                    IntegratedDecoupler symIntegratedDecoupler = counterpart.FindModuleImplementing<IntegratedDecoupler>();
                    if (!toInitialize.Contains(symIntegratedDecoupler))
                        toInitialize.Add(symIntegratedDecoupler);
#if false
                    if (root.children == null) return;
                    for (int i1 = 0; i1 < root.children.Count; ++i1)
                    {
                        CollectToInitializerList(root.children[i1], toInitialize);
                    }
#endif
                }
            }
        }


        void CheckIfModified()
        {
            Log.Info("lcnt: " + lcnt + ",  CheckIfModified");
            Part attachedPart = null;
            if (topNode != null && topNode.attachedPart != null)
            {
                Log.Info("lcnt: " + lcnt + " part attached to node: " + topNode.attachedPart.partInfo.name);
                attachedPart = topNode.attachedPart;
            }
            else
            {
                if (topNode01 != null && topNode01.attachedPart != null)
                {
                    Log.Info("lcnt: " + lcnt + " part attached to node01: " + topNode01.attachedPart.partInfo.name);
                    attachedPart = topNode01.attachedPart;
                }
                if (topNode02 != null && topNode02.attachedPart != null)
                {
                    Log.Info("lcnt: " + lcnt + " part attached to node: " + topNode.attachedPart.partInfo.name);
                    attachedPart = topNode02.attachedPart;
                }
            }

            // If attached part is an engine, and it's attached to the bottom node, enable this at start
            if (attachedPart != null &&
                (attachedPart.Modules.Contains("ModuleEngines") || attachedPart.Modules.Contains("ModuleEnginesFX"))) //is part an engine?
            {

                if (lastTopNodeAttachedPart != attachedPart)
                {
                    Log.Info("lcnt: " + lcnt + ",   lastTopNodeAttachedPart != attachedPart, setting status true");
                    SetStatus(true, this);
                }

                AttachNode bottomNode = null;
#if ENGINES_W_2_BOTTOMNODES
                    AttachNode bottomNode01 = null;
                    AttachNode bottomNode02 = null;
#endif
                Part bottomNodeAttachedPart = null;

                bottomNode = GetNode("bottom", attachedPart);
                if (bottomNode != null)
                {
                    bottomNodeAttachedPart = bottomNode.attachedPart;
                }
#if ENGINES_W_2_BOTTOMNODES
                    else
                    { 
                        bottomNode01 = GetNode("bottom01", attachedPart);
                        if (bottomNode01 != null)
                            bottomNodeAttachedPart = bottomNode01.attachedPart;
                        else
                        {
                            bottomNode02 = GetNode("bottom02", attachedPart);
                            if (bottomNode02 != null)
                                bottomNodeAttachedPart = bottomNode02.attachedPart;
                        }
                    }
#endif

                if (part != bottomNodeAttachedPart)
                {
                    Log.Info("lcnt: " + lcnt + "   Top node not attached to bottom node of engine");
                    ScreenMessages.PostScreenMessage(lcnt + "Top node not attached to bottom node of engine", 3f, ScreenMessageStyle.UPPER_CENTER);

                    SetStatus(false, this);
                    Events["ToggleIntegratedDecoupler"].guiActiveEditor = false;
                }
                else
                {
                    //SetStatus(true, this);
                    Log.Info("lcnt: " + lcnt + "   Top node is attached to bottom node of engine");
                    Events["ToggleIntegratedDecoupler"].guiActiveEditor = true;
                }
                lastTopNodeAttachedPart = attachedPart;
            }
            else
            {
                Log.Info("lcnt: " + lcnt + "  No engine detected attached to top node");
                Events["ToggleIntegratedDecoupler"].guiActiveEditor = true;
                if (attachedPart != null)
                {
                    if (lastTopNodeAttachedPart != null && lastTopNodeAttachedPart != attachedPart)
                        SetStatus(false, this);
                    lastTopNodeAttachedPart = attachedPart;
                }
            }

        }

        void onEditorShipModified(ShipConstruct s)
        {
            //Log.Info("onEditorShipModified 1, lcnt: " + lcnt );

            // if (isOnEditorShipModified)
            //     return;
            // isOnEditorShipModified = true;
            if (toInitialize.Count == 0)
                return;


            Log.Info("onEditorShipModified 2, lcnt: " + lcnt + ",   toInitialize.Count: " + toInitialize.Count);


            if (topNode != null || topNode01 != null || topNode02 != null)
            {
                Log.Info("top attach node found, lcnt: " + lcnt);

                for (int i = 0; i < toInitialize.Count; i++)
                {
                    toInitialize[i].CheckIfModified();
                }

            }
            //isOnEditorShipModified = false;
        }



        private void OnDestroy()
        {
            GameEvents.onEditorShipModified.Remove(onEditorShipModified);
            GameEvents.onEditorPartPlaced.Remove(onEditorPartPlaced);
        }

        public override string GetInfo()
        {
            // need to add code here
            Log.Info("GetInfo");
            string s = "";
            //if (techAvailable)
            {
                s = string.Format("Optional built-in decoupler\nOptional built-in Sepratrons");
            }
            Log.Info("GetInfo returning: " + s);
            return s;
        }

        public float GetModuleMass(float m, ModifierStagingSituation mss)
        {
            if (integratedDecoupler)
            {
                return decouplerMass;
            }
            return 0;
        }
        public ModifierChangeWhen GetModuleMassChangeWhen()
        {
            return ModifierChangeWhen.CONSTANTLY;
        }

        public float GetModuleCost(float m, ModifierStagingSituation mss)
        {
            if (integratedDecoupler)
            {
                return decouplerCost;
            }
            return 0;
        }
        public ModifierChangeWhen GetModuleCostChangeWhen()
        {
            return ModifierChangeWhen.FIXED;

        }

        public bool DecouplerRequiresTech()
        {
            return !string.IsNullOrEmpty(this.techRequired);
        }

        public string DecouplerTech()
        {
            return this.techRequired;
        }

        public bool DecouplerHasTech()
        {
            return (HighLogic.CurrentGame.Mode != Game.Modes.CAREER && HighLogic.CurrentGame.Mode != Game.Modes.SCIENCE_SANDBOX) ||
                ResearchAndDevelopment.GetTechnologyState(this.techRequired) == RDTech.State.Available;
        }
    }
}
