using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IntegratedStackedTankDecouplers
{
    partial class IntegratedDecoupler:  IPartCostModifier, IPartMassModifier
    {

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
            if (integratedDecoupler != DecouplerType.none)
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
            if (integratedDecoupler != DecouplerType.none)
            {
                return decouplerCost;
            }
            return 0;
        }
        public ModifierChangeWhen GetModuleCostChangeWhen()
        {
            return ModifierChangeWhen.FIXED;

        }

    }
}
