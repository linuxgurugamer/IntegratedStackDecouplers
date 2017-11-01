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
        List<IntegratedDecoupler> partsInSymmetry = new List<IntegratedDecoupler>();

        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = false)]
        public bool initialized = false;

        public enum DecouplerType { none, Enabled, EnabledStagingDisabled }

        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = false)]
        //public bool integratedDecoupler = false;
        DecouplerType integratedDecoupler = DecouplerType.none;

        [KSPEvent(name = "integratedDecoupler", guiName = "No decoupler", guiActiveEditor = true, active = true)]
        public void ToggleIntegratedDecoupler()
        {
            integratedDecoupler++;
            if (integratedDecoupler > DecouplerType.EnabledStagingDisabled)
                integratedDecoupler = DecouplerType.none;
            SetAllStatus(integratedDecoupler);
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


        void SetToggleName(BaseEventList pmEvents)
        {
            switch (integratedDecoupler)
            {
                case DecouplerType.none:
                    pmEvents["ToggleIntegratedDecoupler"].guiName = "No decoupler";
                    break;
                case DecouplerType.Enabled:
                    pmEvents["ToggleIntegratedDecoupler"].guiName = "Integrated Decoupler";
                    break;
                case DecouplerType.EnabledStagingDisabled:
                    pmEvents["ToggleIntegratedDecoupler"].guiName = "Integrated Decoupler, staging disabled";
                    break;
                
            }
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            //topNodeChecked = false;

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
                    SetEvents(DecouplerType.none, decouplerModule, crossfeedToggleModule);

                }
            }
            else
            {
                decouplerModule.Events["ToggleStaging"].guiActiveEditor = false;

                SetToggleName(pmEvents);
                if (integratedDecoupler != DecouplerType.none)
                {
                    //pmEvents["ToggleIntegratedDecoupler"].guiName = "Integrated Decoupler";
                   // decouplerModule.Events["ToggleStaging"].guiActiveEditor = true;
                }
                else
                {
                    decouplerModule.Events["ToggleStaging"].guiActiveEditor = false;
                    decouplerModule.Fields["ejectionForcePercent"].guiActiveEditor = false;
                    if (crossfeedToggleModule != null)
                    {
                        KSPActionParam kap = null;
                        crossfeedToggleModule.EnableAction(kap);
                        SetEvents(DecouplerType.none, decouplerModule, crossfeedToggleModule);

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
        void SetStatus(DecouplerType status, IntegratedDecoupler symIntegratedDecoupler)
        {
            Part counterpart = symIntegratedDecoupler.part;

            ModuleToggleCrossfeed symCrossfeedToggleModule = counterpart.FindModuleImplementing<ModuleToggleCrossfeed>();

            symIntegratedDecoupler.integratedDecoupler = status;
            if (integratedDecoupler != DecouplerType.none)
                enableDecoupler(symIntegratedDecoupler, symCrossfeedToggleModule);
            else
                disableDecoupler(symIntegratedDecoupler, symCrossfeedToggleModule);

            SetEvents(status, symIntegratedDecoupler.decouplerModule, symCrossfeedToggleModule);
        }

        void SetAllStatus(DecouplerType decouplerType, bool inEditorModified = false, bool doSymmetry = true)
        {            
            for (int i = 0; i < partsInSymmetry.Count; i++)
            { 
                IntegratedDecoupler symIntegratedDecoupler = partsInSymmetry[i];
                SetStatus(decouplerType, symIntegratedDecoupler);     
            }
            

            Log.Info("SetStatus 1");
            //if (!inEditorModified)
                GameEvents.onEditorShipModified.Fire(EditorLogic.fetch.ship);
            Log.Info("SetStatus 1");
        }


        void enableDecoupler(IntegratedDecoupler id, ModuleToggleCrossfeed crossfeedToggleModule)
        {
            KSPActionParam kap = null;
            base.enabled = true;
            if (id.decouplerModule != null)
            {
                id.decouplerModule.SetStaging(true);
                id.decouplerModule.Events["ToggleStaging"].guiActiveEditor = true; // Only seen when AdvancedTweakables is enabled
                id.decouplerModule.Fields["ejectionForcePercent"].guiActiveEditor = true;
                id.decouplerModule.enabled = true;
                //id.decouplerModule.stagingEnabled = true;
                if (integratedDecoupler == DecouplerType.Enabled)
                    id.decouplerModule.SetStaging(true);
                else
                    id.decouplerModule.SetStaging(false);
                if (crossfeedToggleModule != null)
                    crossfeedToggleModule.DisableAction(kap);

                // if (decouplerModule.part != null)
                SetToggleName(id.pmEvents);
                //id.pmEvents["ToggleIntegratedDecoupler"].guiName = "Integrated Decoupler";
                id.decouplerModule.Events["ToggleStaging"].guiActiveEditor = true;
                id.decouplerModule.Events["ToggleStaging"].guiActiveEditor = false;
                id.part.UpdateStageability(true, true);

            }
        }

        void disableDecoupler(IntegratedDecoupler id, ModuleToggleCrossfeed crossfeedToggleModule)
        {
            KSPActionParam kap = null;

            if (id.decouplerModule != null)
            {
                id.decouplerModule.SetStaging(false);
                id.decouplerModule.Events["ToggleStaging"].guiActiveEditor = false; // Only seen when AdvancedTweakables is enabled
                id.decouplerModule.Fields["ejectionForcePercent"].guiActiveEditor = false;
                id.decouplerModule.enabled = false;
                //id.decouplerModule.stagingEnabled = false;
                id.SetStaging(false);

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
                //id.pmEvents["ToggleIntegratedDecoupler"].guiName = "No decoupler";
                SetToggleName(id.pmEvents);
                id.decouplerModule.Events["ToggleStaging"].guiActiveEditor = false;
                id.part.UpdateStageability(true, true);
            }
        }

        void SetEvents(DecouplerType decouplerType, ModuleDecouple decouplerModule, ModuleToggleCrossfeed crossfeedToggleModule)
        {
            bool status = (decouplerType != DecouplerType.none);
            crossfeedToggleModule.Events["ToggleEvent"].guiActive = status;
            crossfeedToggleModule.Events["ToggleEvent"].active = status;
            crossfeedToggleModule.Actions["ToggleAction"].active = status;
            crossfeedToggleModule.Actions["EnableAction"].active = status;
            crossfeedToggleModule.Actions["DisableAction"].active = status;

            //crossfeedToggleModule.Actions["ToggleStaging"].active = false;
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
            if (!techAvailable) // || moduleJettison != null
            {
                this.Events["ToggleIntegratedDecoupler"].guiActiveEditor = false;
                // this.Fields["seperatronCnt"].guiActiveEditor = false;
                Log.Info("Disabling IntegratedDecoupler due to no tech");

                SetStatus(DecouplerType.none, this);

                return;
            }
            GetSymList();
            SetStatus(DecouplerType.none, this);
            lastTopNodeAttachedPart = null;

            EditorLogic.fetch.SetBackup();
        }



        void CheckIfAttachedToEngine()
        {
            Part attachedPart = null;
            if (topNode != null && topNode.attachedPart != null)
            {
                attachedPart = topNode.attachedPart;
            }
            else
            {
                if (topNode01 != null && topNode01.attachedPart != null)
                {
                    attachedPart = topNode01.attachedPart;
                }
                if (topNode02 != null && topNode02.attachedPart != null)
                {
                    attachedPart = topNode02.attachedPart;
                }
            }

            // If attached part is an engine, and it's attached to the bottom node, enable this at start
            if (attachedPart != null &&
                (attachedPart.Modules.Contains("ModuleEngines") || attachedPart.Modules.Contains("ModuleEnginesFX"))) //is part an engine?
            {

                if (lastTopNodeAttachedPart != attachedPart)
                {
                    Log.Info("lastTopNodeAttachedPart != attachedPart, setting status true");
                    SetStatus(DecouplerType.none, this);
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
                    ScreenMessages.PostScreenMessage("Top node not attached to bottom node of engine", 3f, ScreenMessageStyle.UPPER_CENTER);

                    SetStatus(DecouplerType.none, this);
                    Events["ToggleIntegratedDecoupler"].guiActiveEditor = false;
                }
                else
                {
                    //SetStatus(true, this);
                    Events["ToggleIntegratedDecoupler"].guiActiveEditor = true;
                }
                lastTopNodeAttachedPart = attachedPart;
            }
            else
            {
                Log.Info("  No engine detected attached to top node");
                Events["ToggleIntegratedDecoupler"].guiActiveEditor = true;
                if (attachedPart != null)
                {
                    if (lastTopNodeAttachedPart != null && lastTopNodeAttachedPart != attachedPart)
                        SetStatus(DecouplerType.none, this);
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


            Log.Info("onEditorShipModified 2,   toInitialize.Count: " + partsInSymmetry.Count);


            if (topNode != null || topNode01 != null || topNode02 != null)
            {
                Log.Info("top attach node found" );

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
