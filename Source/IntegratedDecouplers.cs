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

        [KSPEvent(name= "integratedDecoupler", guiName = "No decoupler", guiActiveEditor = true, active = true)]
        public void ToggleIntegratedDecoupler()
        {
            integratedDecoupler = !integratedDecoupler;
            SetStatus();           
        }

//        [KSPField(guiName = "Seperatron Count", isPersistant = true, guiActiveEditor = true),
//            UI_FloatRange(stepIncrement = 1f, maxValue = 100f, minValue = 0f)]
        public float seperatronCnt = 0;
        float oldSepCnt;

        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false)]
        public float decouplerMass = 0.15f;

        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false)]
        public float decouplerCost = 250f;

        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false)]
        public new string techRequired = "";

        NodeControl newNodes;


        void SetStatus()
        {
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
            
            GameEvents.onEditorShipModified.Fire(EditorLogic.fetch.ship);
        }

        void enableDecoupler()
        {
            KSPActionParam kap = null;
            base.enabled = true;
            if (decouplerModule != null)
            {
                decouplerModule.SetStaging(true);
                decouplerModule.Events["ToggleStaging"].guiActiveEditor = true;
                decouplerModule.Fields["ejectionForcePercent"].guiActiveEditor = true;
                stagingEnabled = true;
                crossfeedToggleModule.DisableAction(kap);
            }

            newNodes = new NodeControl(this.part, (int)seperatronCnt);
            

            Events["ToggleIntegratedDecoupler"].guiName = "Integrated Decoupler";

        }
        void disableDecoupler()
        {
            KSPActionParam kap = null;

            if (decouplerModule != null)
            {
                decouplerModule.SetStaging(false);
                decouplerModule.Events["ToggleStaging"].guiActiveEditor = false;
                decouplerModule.Fields["ejectionForcePercent"].guiActiveEditor = false;

                stagingEnabled = false;

                crossfeedToggleModule.EnableAction(kap);
            }
            base.enabled = false;
            Events["ToggleIntegratedDecoupler"].guiName = "No decoupler";
            if (newNodes != null)
                newNodes.DelNodes(this.part);
        }
        ModuleDecouple decouplerModule = null;
        PartModule decouplerPartModule = null;
        ModuleToggleCrossfeed crossfeedToggleModule = null;

        bool techAvailable { get
        {
                var b = true;
                if (this.DecouplerRequiresTech())
                    b = this.DecouplerHasTech();
                return b;
            }
        }
        public override void OnStart(StartState state)
        {
            Log.Info("IntegratedDecoupler.OnStart");
            base.OnStart(state);


            decouplerModule = this.part.FindModuleImplementing<ModuleDecouple>();
            decouplerPartModule = decouplerModule;

            crossfeedToggleModule = this.part.FindModuleImplementing<ModuleToggleCrossfeed>();

            ModuleJettison moduleJettison = this.part.FindModuleImplementing<ModuleJettison>();            

            if (decouplerModule == null)
                Log.Info("ModuleDecouple NOT found");
            else
                Log.Info("ModuleDecouple found");

            if (!techAvailable || moduleJettison != null)
            {
                this.Events["ToggleIntegratedDecoupler"].guiActiveEditor = false;
                // this.Fields["seperatronCnt"].guiActiveEditor = false;
                Log.Info("Disabling IntegratedDecoupler due to no tech");
                disableDecoupler();
                if (decouplerModule != null)
                    part.UpdateStageability(true, true);

                //GameEvents.onEditorShipModified.Fire(EditorLogic.fetch.ship);
                return;
            }

            SetStatus();
            GameEvents.onEditorShipModified.Fire(EditorLogic.fetch.ship);
            EditorLogic.fetch.SetBackup();
            oldSepCnt = seperatronCnt;
            GameEvents.onEditorShipModified.Add(onEditorShipModified);
        }

        void onEditorShipModified(ShipConstruct sc)
        {
            if (oldSepCnt != seperatronCnt)
            {
                oldSepCnt = seperatronCnt;
                if (newNodes != null)
                {
                    newNodes.DelNodes(this.part);
                    newNodes.AddNodes(this.part, (int)seperatronCnt);
                }
            }
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
