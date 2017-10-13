using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IntegratedStackedTankDecouplers
{
    partial class IntegratedDecoupler
    {

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

        void GetReferences()
        {
            decouplerModule = this.part.FindModuleImplementing<ModuleDecouple>();
            decouplerPartModule = decouplerModule;

            crossfeedToggleModule = this.part.FindModuleImplementing<ModuleToggleCrossfeed>();

            ModuleJettison moduleJettison = this.part.FindModuleImplementing<ModuleJettison>();
            pmEvents = Events;

            if (decouplerModule == null)
                Log.Info("ModuleDecouple NOT found");
            else
                Log.Info("ModuleDecouple found");
        }

        void GetSymList()
        {
            // Collect a list of modules to initialize from the part and all its children.
            partsInSymmetry.Clear();
            partsInSymmetry.Add(this);
            CollectToInitializerList(part, partsInSymmetry);

            // Also any symmetry counterparts it may have, and *their* children.
            if (part.symmetryCounterparts != null)
            {
                for (int i = 0; i < part.symmetryCounterparts.Count; ++i)
                {
                    Part counterpart = part.symmetryCounterparts[i];
                    if (!ReferenceEquals(this, counterpart)) // probably unnecessary
                    {
                        CollectToInitializerList(counterpart, partsInSymmetry);
                    }
                }
            }
            Log.Info("GetSymList, toInitialize.Count: " + partsInSymmetry.Count);
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

    }
}
