using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KSP.UI.Screens;

namespace IntegratedStackedTankDecouplers
{
    class RadialCrossFeedControl: PartModule
    {
        public enum FuelFlowtype { none, biDirectional, uniDirectional }

        List<RadialCrossFeedControl> radialSymParts = new List<RadialCrossFeedControl>();

        [KSPField(isPersistant = true)]
        public FuelFlowtype CrossfeedType = FuelFlowtype.none;

        [KSPField(isPersistant = true)]
        bool isActive = false;

        FuelFlowtype defaultCrossfeedType = FuelFlowtype.none;

        [KSPEvent(name = "ToggleBiDirectional", guiName = "Crossfeed disabled", guiActiveEditor = true, active = true)]
        public void ToggleBiDirectional()
        {
            Log.Info("CrossfeedType: " + CrossfeedType);
            CrossfeedType++;
            if (CrossfeedType > FuelFlowtype.uniDirectional)
                CrossfeedType = FuelFlowtype.none;
            //SetCrossfeedType(CrossfeedType);
            for (int i = 0; i <  radialSymParts.Count; i++)
            {
                radialSymParts[i].SetCrossfeedType(CrossfeedType);
            }
        }
        void SetCrossfeedType(FuelFlowtype newCrossfeedType)
        {
            CrossfeedType = newCrossfeedType;
            if (crossfeedToggleModule != null)
            {
                switch (newCrossfeedType)
                {
                    case FuelFlowtype.none:
                        Events["ToggleBiDirectional"].guiName = "Crossfeed disabled";
                        if (crossfeedToggleModule.crossfeedStatus)
                            crossfeedToggleModule.ToggleEvent();
                        //crossfeedToggleModule.defaultCrossfeedStatus = false;

                        break;
                    case FuelFlowtype.biDirectional:
                        Events["ToggleBiDirectional"].guiName = "Bi-Directional Crossfeed";
                        if (!crossfeedToggleModule.crossfeedStatus)
                            crossfeedToggleModule.ToggleEvent();
                        //crossfeedToggleModule.defaultCrossfeedStatus = true;

                        break;
                    case FuelFlowtype.uniDirectional:
                        Events["ToggleBiDirectional"].guiName = "UniDirectional Crossfeed";
                        if (!crossfeedToggleModule.crossfeedStatus)
                            crossfeedToggleModule.ToggleEvent();
                        //crossfeedToggleModule.defaultCrossfeedStatus = true;

                        break;
                }
            }
            //else
            //    Log.Error("Part: " + this.part.partInfo.title + " missing crossfeedToggleModule");
        }

        Part observedPart = null;
        ModuleToggleCrossfeed crossfeedToggleModule = null;


        void GetSymList()
        {
            // Collect a list of modules to initialize from the part and all its children.
            radialSymParts.Clear();
            radialSymParts.Add(this);

            for (int i = 0; i < this.part.symmetryCounterparts.Count; ++i)
            {
                Part counterpart = part.symmetryCounterparts[i];
                //if (!ReferenceEquals(part, counterpart))
                if (counterpart != part)
                {
                    RadialCrossFeedControl symRadialCrossFeedControl = counterpart.FindModuleImplementing<RadialCrossFeedControl>();
                    if (symRadialCrossFeedControl != null)
                        radialSymParts.Add(symRadialCrossFeedControl);

                }
            }
            Log.Info("GetSymList, part: " + this.part.partInfo.title + ",  part.symmetryCounterparts.Count: " + part.symmetryCounterparts.Count + ",  partCount: " + radialSymParts.Count);
        }

        /// <summary>
        /// run whenever part is created (used in editor), which in the editor is as soon as part list is clicked or symmetry count increases
        /// </summary>
        void Start()
        {

            crossfeedToggleModule = part.FindModuleImplementing<ModuleToggleCrossfeed>();
            if (crossfeedToggleModule == null)
            {
                Log.Info("Missing ModuleToggleCrossfeed in part: " + this.part.partInfo.title);

                Events["ToggleBiDirectional"].active = false;
                Events["ToggleBiDirectional"].guiActive = false;
                Events["ToggleBiDirectional"].guiActiveEditor = false;
                return;
            }
            isActive = true;
            if (crossfeedToggleModule.crossfeedStatus)
                crossfeedToggleModule.ToggleEvent();
            GameEvents.onEditorShipModified.Add(onEditorShipModified);
            GameEvents.onVesselWasModified.Add(onVesselWasModified);

#if true
            crossfeedToggleModule.Events["ToggleEvent"].guiActive = false;
            crossfeedToggleModule.Events["ToggleEvent"].active = false;
            crossfeedToggleModule.Actions["ToggleAction"].active = false;
            crossfeedToggleModule.Actions["EnableAction"].active = false;
            crossfeedToggleModule.Actions["DisableAction"].active = false;
#endif

            if (!HighLogic.LoadedSceneIsEditor)
                return;

            zAwake();
            this.part.OnEditorAttach += new Callback(UpdateOnEditorAttach);
            this.part.OnEditorDetach += new Callback(UpdateOnEditorDetach);

            if (base.part.partInfo == null || base.part.partInfo.partPrefab == null)
            {
                this.defaultCrossfeedType = FuelFlowtype.none;
            }

            if (!this.moduleIsEnabled)
                CrossfeedType = defaultCrossfeedType;

        }

        void  zAwake()
        {
            GetSymList();
            defaultCrossfeedType = CrossfeedType;
            SetCrossfeedType(CrossfeedType);
            //Log.Info("zAwake, CrossfeedType: " + CrossfeedType + "   crossfeedToggleModule.crossfeedStatus: " + crossfeedToggleModule.crossfeedStatus);
        }

        private void OnDestroy()
        {
            if (!isActive)
                return;
            GameEvents.onEditorShipModified.Remove(onEditorShipModified);
            GameEvents.onVesselWasModified.Remove(onVesselWasModified);
        }

        public void UpdateOnEditorAttach()
        {

            GetSymList();
            if (radialSymParts.Count == 0)
                return;

            for (int i = 0; i < radialSymParts.Count; i++)
            {
                radialSymParts[i].SetCrossfeedType(defaultCrossfeedType);
            }
        }

        public void UpdateOnEditorDetach()
        {

        }

        /// <summary>
        /// Called when ship is modified, checks to see if this part is modified
        /// </summary>
        /// <param name="s"></param>
        void onEditorShipModified(ShipConstruct s)
        {
            Log.Info("onEditorShipModified");
            GetSymList();        
        }

        List<int> availableResourceIDs = new List<int>();
        List<int> engineResourceIDs = new List<int>();
        HashSet<Part> hashPartSet = new HashSet<Part>();
        PartSet partSet = null;

        void GetAvailableResourceIDs(Part p, bool first = false)
        {
            if (first)
                availableResourceIDs = new List<int>();
            hashPartSet.Add(p);
#if false
            foreach (PartResource resource in p.Resources)
            {
                if (resource.amount > 0)
                {
                    var id = PartResourceLibrary.Instance.GetDefinition(resource.resourceName).id;
                    if (!availableResourceIDs.Contains(id))
                        availableResourceIDs.Add(id);

                }
            }
#endif

            //
            // Add all resources used by attached engines
            //
            List<Propellant> propellents = null;
            if (p.Modules.Contains("ModuleEngines") | p.Modules.Contains("ModuleEnginesFX")) //is part an engine?
            {
                foreach (PartModule TWR1PartModule in p.Modules) 
                {
                    propellents = null;
                    if (TWR1PartModule.moduleName == "ModuleEngines") //find partmodule engine on th epart
                    {
                        var me = (ModuleEngines)TWR1PartModule;
                       propellents = me.propellants;
                         
                    }
                    if (TWR1PartModule.moduleName == "ModuleEnginesFX") //find partmodule engine on th epart
                    {
                        var mex = (ModuleEnginesFX)TWR1PartModule;

                        propellents = mex.propellants;
                    }
                    if (propellents != null)
                    {
                        for  (int i = 0; i <propellents.Count; i++)
                        {
                            PartResourceDefinition prd = PartResourceLibrary.Instance.GetDefinition(propellents[i].name);
                            if (prd.resourceFlowMode != ResourceFlowMode.ALL_VESSEL &&
                                prd.resourceFlowMode != ResourceFlowMode.ALL_VESSEL_BALANCE)
                            {
                                Log.Info("Engine resource: " + propellents[i].name);
                                if (!availableResourceIDs.Contains(prd.id))
                                    availableResourceIDs.Add(prd.id);
                            }
                        }
                    }
                }
            }


            foreach (var p1 in p.children)
                GetAvailableResourceIDs(p1);
        }

        /// <summary>
        /// This should be called when staging, and wil lthen rebuild the partSet used to check for resources
        /// </summary>
        /// <param name="v"></param>
        void onVesselWasModified(Vessel v)
        {
            Log.Info("onVesselWasModified");
            if (v == this.vessel)
            {
                if (hashPartSet != null)
                    hashPartSet.Clear();
                if (partSet != null)
                    partSet = null;
                initFlight();
            }
        }

        bool flightInitialized = false;

        void initFlight()
        {
            Log.Info("Initializing flight");
            zAwake();
            if (crossfeedToggleModule != null)
                Log.Info("initFlight, CrossfeedType: " + CrossfeedType + "   crossfeedToggleModule.crossfeedStatus: " + crossfeedToggleModule.crossfeedStatus);
            this.GetAvailableResourceIDs(this.part, true);
            partSet = new PartSet(hashPartSet);

            flightInitialized = true;
        }

        void FixedUpdate()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                if (!flightInitialized)
                    initFlight();
 
                if (CrossfeedType == FuelFlowtype.uniDirectional)
                {

                    if (part.children.Count > 0)
                    {
                        if (observedPart == null)
                        {
                            observedPart = part.children[0];
                        }
                    }
                    else
                        observedPart = null;

                    if (observedPart != null && observedPart.Resources.Count > 0)
                    {
                        for (int i = 0; i < availableResourceIDs.Count; i++)
                        {
                            double amount, maxAmount;
                            
                            partSet.GetConnectedResourceTotals(availableResourceIDs[i], out amount, out maxAmount, true);
                            if (amount == 0)
                            {
                                if (crossfeedToggleModule.crossfeedStatus)
                                {
                                    Log.Info("Setting part crossfeed to false");
                                    crossfeedToggleModule.ToggleEvent();
                                }
                            }
                        }
                    }
                }
            }
            else
                flightInitialized = false;
        }
    }
}
