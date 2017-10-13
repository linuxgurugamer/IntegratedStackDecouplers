using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.UI.Screens;


namespace IntegratedStackedTankDecouplers
{
    public partial class IntegratedDecoupler : ModuleToggleCrossfeed
    {
        static int cnt = 0;

#if DEBUG
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true)]
#endif
        int lcnt = -1;

        List<IntegratedDecoupler> partsInSymmetry = new List<IntegratedDecoupler>();

        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = false)]
        public bool initialized = false;

        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = false)]
        public bool integratedDecoupler = false;

        [KSPEvent(name = "integratedDecoupler", guiName = "No decoupler", guiActiveEditor = true, active = true)]
        public void ToggleIntegratedDecoupler()
        {
            Log.Info("ToggleIntegratedDecoupler, lcnt: " + lcnt);
            SetAllStatus(!integratedDecoupler);
        }

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


        ModuleDecouple decouplerModule = null;
        PartModule decouplerPartModule = null;
        ModuleToggleCrossfeed crossfeedToggleModule = null;




        public override void OnStart(StartState state)
        {
            Log.Info("lcnt: " + lcnt + ",  IntegratedDecoupler.OnStart");
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
            //GameEvents.onEditorPartPlaced.Add(onEditorPartPlaced);
            part.OnEditorAttach += new Callback(OnEditorAttach);

            GetReferences();
            if (!initialized)
            {
                initialized = true;
                decouplerModule.Events["ToggleStaging"].guiActiveEditor = false;
                decouplerModule.Fields["ejectionForcePercent"].guiActiveEditor = false;
                if (crossfeedToggleModule != null)
                {
                    KSPActionParam kap = null;
                    crossfeedToggleModule.EnableAction(kap);
                    SetEvents(false, decouplerModule, crossfeedToggleModule);

                }
            }
            else
            {
                if (integratedDecoupler)
                {
                    pmEvents["ToggleIntegratedDecoupler"].guiName = "Integrated Decoupler";
                    decouplerModule.Events["ToggleStaging"].guiActiveEditor = true;
                }
                else
                {
                    decouplerModule.Events["ToggleStaging"].guiActiveEditor = false;
                    decouplerModule.Fields["ejectionForcePercent"].guiActiveEditor = false;
                    if (crossfeedToggleModule != null)
                    {
                        KSPActionParam kap = null;
                        crossfeedToggleModule.EnableAction(kap);
                        SetEvents(false, decouplerModule, crossfeedToggleModule);

                    }
                }
            }
            GetSymList();

        }

        /// <summary>
        /// Set the decoupler status
        /// </summary>
        /// <param name="status"></param>
        /// <param name="symIntegratedDecoupler"></param>
        void SetStatus(bool status, IntegratedDecoupler symIntegratedDecoupler)
        {
            Part counterpart = symIntegratedDecoupler.part;

            ModuleToggleCrossfeed symCrossfeedToggleModule = counterpart.FindModuleImplementing<ModuleToggleCrossfeed>();

            symIntegratedDecoupler.integratedDecoupler = status;
            if (integratedDecoupler)
                enableDecoupler(symIntegratedDecoupler, symCrossfeedToggleModule);
            else
                disableDecoupler(symIntegratedDecoupler, symCrossfeedToggleModule);

            SetEvents(integratedDecoupler, symIntegratedDecoupler.decouplerModule, symCrossfeedToggleModule);
        }

        void SetAllStatus(bool b, bool inEditorModified = false, bool doSymmetry = true)
        {
            //integratedDecoupler = b;
            Log.Info("lcnt: " + lcnt + ",   SetStatus: " + b);
            
            for (int i = 0; i < partsInSymmetry.Count; i++)
            { 
                IntegratedDecoupler symIntegratedDecoupler = partsInSymmetry[i];
                SetStatus(b, symIntegratedDecoupler);     
            }
            

            Log.Info("SetStatus 1");
            if (!inEditorModified)
                GameEvents.onEditorShipModified.Fire(EditorLogic.fetch.ship);
            Log.Info("SetStatus 1");
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
                id.decouplerModule.Events["ToggleStaging"].guiActiveEditor = true;
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
                id.decouplerModule.Events["ToggleStaging"].guiActiveEditor = false;

            }
        }

        void SetEvents(bool status, ModuleDecouple decouplerModule, ModuleToggleCrossfeed crossfeedToggleModule)
        {
            crossfeedToggleModule.Events["ToggleEvent"].guiActive = status;
            crossfeedToggleModule.Events["ToggleEvent"].active = status;
            crossfeedToggleModule.Actions["ToggleAction"].active = status;
            crossfeedToggleModule.Actions["EnableAction"].active = status;
            crossfeedToggleModule.Actions["DisableAction"].active = status;

#if false
            if (decouplerModule != null &&
                (topNode != null || topNode01 != null || topNode02 != null))
                decouplerModule.part.UpdateStageability(true, true);
            else
                Log.Error("No ModuleDecouple to update");
#endif
        }




        void OnEditorAttach()
        {
            Log.Info("lcnt: " + lcnt + ", OnEditorAttach");
            if (!techAvailable) // || moduleJettison != null
            {
                this.Events["ToggleIntegratedDecoupler"].guiActiveEditor = false;
                // this.Fields["seperatronCnt"].guiActiveEditor = false;
                Log.Info("Disabling IntegratedDecoupler due to no tech");

                SetStatus(false, this);

                return;
            }
            GetSymList();
            Log.Info("lcnt: " + lcnt + ",  Doing initial SetStatus");
            SetStatus(false, this);
            lastTopNodeAttachedPart = null;

            EditorLogic.fetch.SetBackup();
        }



        void CheckIfAttachedToEngine()
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

        /// <summary>
        /// Called when ship is modified, checks to see if this part is modified
        /// </summary>
        /// <param name="s"></param>
        void onEditorShipModified(ShipConstruct s)
        {
            //Log.Info("onEditorShipModified 1, lcnt: " + lcnt );

            // if (isOnEditorShipModified)
            //     return;
            // isOnEditorShipModified = true;
            if (partsInSymmetry.Count == 0)
                return;


            Log.Info("onEditorShipModified 2, lcnt: " + lcnt + ",   toInitialize.Count: " + partsInSymmetry.Count);


            if (topNode != null || topNode01 != null || topNode02 != null)
            {
                Log.Info("top attach node found, lcnt: " + lcnt);

                for (int i = 0; i < partsInSymmetry.Count; i++)
                {
                    partsInSymmetry[i].CheckIfAttachedToEngine();
                }

            }
            //isOnEditorShipModified = false;
        }



        private void OnDestroy()
        {
            GameEvents.onEditorShipModified.Remove(onEditorShipModified);
            //GameEvents.onEditorPartPlaced.Remove(onEditorPartPlaced);
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
