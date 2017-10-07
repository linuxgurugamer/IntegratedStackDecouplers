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
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = false)]
        public bool integratedDecoupler = false;

        [KSPEvent(name = "integratedDecoupler", guiName = "No decoupler", guiActiveEditor = true, active = true)]
        public void ToggleIntegratedDecoupler()
        {
            Log.Info("ToggleIntegratedDecoupler");
            SetStatus(!integratedDecoupler);
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

        void SetStatus(bool b, bool inEditorModified = false)
        {
            integratedDecoupler = b;
            if (integratedDecoupler)
            {
                enableDecoupler();
            }
            else
            {
                disableDecoupler();
            }
            crossfeedToggleModule.Events["ToggleEvent"].guiActive = false;
            crossfeedToggleModule.Events["ToggleEvent"].active = false;
            crossfeedToggleModule.Actions["ToggleAction"].active = false;
            crossfeedToggleModule.Actions["EnableAction"].active = false;
            crossfeedToggleModule.Actions["DisableAction"].active = false;

            if (decouplerModule != null)
                part.UpdateStageability(true, true);
            else
                Log.Error("No ModuleDecouple to update");

            if (!inEditorModified)
                GameEvents.onEditorShipModified.Fire(EditorLogic.fetch.ship);
        }

        void enableDecoupler()
        {
            Log.Info("enableDecoupler");
            KSPActionParam kap = null;
            base.enabled = true;
            if (decouplerModule != null)
            {
                decouplerModule.SetStaging(true);
                decouplerModule.Events["ToggleStaging"].guiActiveEditor = true; // Only seen when AdvancedTweakables is enabled
                decouplerModule.Fields["ejectionForcePercent"].guiActiveEditor = true;
                decouplerModule.enabled = true;
                stagingEnabled = true;
                crossfeedToggleModule.DisableAction(kap);
            }

            Events["ToggleIntegratedDecoupler"].guiName = "Integrated Decoupler";

        }
        void disableDecoupler()
        {
            Log.Info("disableDecoupler");
            KSPActionParam kap = null;

            if (decouplerModule != null)
            {
                decouplerModule.SetStaging(false);
                decouplerModule.Events["ToggleStaging"].guiActiveEditor = false;
                decouplerModule.Fields["ejectionForcePercent"].guiActiveEditor = false;
                decouplerModule.enabled = false;
                stagingEnabled = false;

                foreach (PartModule m in part.Modules)
                {
                    m.SetStaging(false);
                    m.Events["ToggleStaging"].guiActiveEditor = false;
                }

                crossfeedToggleModule.EnableAction(kap);
            }
            base.enabled = false;
            Events["ToggleIntegratedDecoupler"].guiName = "No decoupler";

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

            decouplerModule = this.part.FindModuleImplementing<ModuleDecouple>();
            decouplerPartModule = decouplerModule;

            crossfeedToggleModule = this.part.FindModuleImplementing<ModuleToggleCrossfeed>();

            ModuleJettison moduleJettison = this.part.FindModuleImplementing<ModuleJettison>();

            if (decouplerModule == null)
                Log.Info("ModuleDecouple NOT found");
            else
                Log.Info("ModuleDecouple found");

            if (!techAvailable) // || moduleJettison != null
            {
                this.Events["ToggleIntegratedDecoupler"].guiActiveEditor = false;
                // this.Fields["seperatronCnt"].guiActiveEditor = false;
                Log.Info("Disabling IntegratedDecoupler due to no tech");

                SetStatus(false);


                //GameEvents.onEditorShipModified.Fire(EditorLogic.fetch.ship);
                return;
            }


            SetStatus(integratedDecoupler);

            EditorLogic.fetch.SetBackup();

            topNode = GetNode("top", part);
            if (topNode == null)
            {
                topNode01 = GetNode("top01", part);
                if (topNode01 != null)
                    topNode02 = GetNode("top02", part);
            }

            GameEvents.onEditorShipModified.Add(onEditorShipModified);

        }

        bool isOnEditorShipModified = false;


        void onEditorShipModified(ShipConstruct s)
        {
            Log.Info("onEditorShipModified 1");
            if (isOnEditorShipModified)
                return;
            isOnEditorShipModified = true;

            Log.Info("onEditorShipModified 2");

            if (topNode != null || topNode01 != null || topNode02 != null)
            {
                Log.Info("top attach node found");
                Part attachedPart = null;
                if (topNode != null && topNode.attachedPart != null)
                {
                    Log.Info("part attached to node: " + topNode.attachedPart.partInfo.name);
                    attachedPart = topNode.attachedPart;
                }
                else
                {
                    if (topNode01 != null && topNode01.attachedPart != null)
                    {
                        Log.Info("part attached to node01: " + topNode01.attachedPart.partInfo.name);
                        attachedPart = topNode01.attachedPart;
                    }
                    if (topNode02 != null && topNode02.attachedPart != null)
                    {
                        Log.Info("part attached to node: " + topNode.attachedPart.partInfo.name);
                        attachedPart = topNode02.attachedPart;
                    }
                }
                // If attached part is an engine, and it's attached to the bottom node, enable this at start
                if (attachedPart != null &&
                    (attachedPart.Modules.Contains("ModuleEngines") || attachedPart.Modules.Contains("ModuleEnginesFX"))) //is part an engine?
                {
                    if (lastTopNodeAttachedPart != attachedPart)
                        SetStatus(true);

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
                        ScreenMessages.PostScreenMessage("Top node not attached to bottom node ", 3f, ScreenMessageStyle.UPPER_CENTER);

                        SetStatus(false, true);
                        Events["ToggleIntegratedDecoupler"].guiActiveEditor = false;
                    }
                    else
                        Events["ToggleIntegratedDecoupler"].guiActiveEditor = true;
                    lastTopNodeAttachedPart = attachedPart;
                }
                else
                {
                    Log.Info("No engine detected attached to top node");
                    if (attachedPart != null)
                    {
                        if (lastTopNodeAttachedPart != null && lastTopNodeAttachedPart != attachedPart)
                            SetStatus(false);
                        lastTopNodeAttachedPart = attachedPart;
                    }
                }


               
            }
            isOnEditorShipModified = false;
        }



        private void OnDestroy()
        {
            GameEvents.onEditorShipModified.Remove(onEditorShipModified);
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
