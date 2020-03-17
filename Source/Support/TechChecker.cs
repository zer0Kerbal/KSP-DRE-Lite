using System;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;

namespace Proximity
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class TechChecker : MonoBehaviour
    {
        public static bool TechAvailable
        {
            get;
            set;
        }
        
        public static bool RealChutes
        {
            get;
            set;
        }

        private static bool initialized = false;

        private void Start()
        {
            TechAvailable = false;
            RealChutes = false;

            try
            {
                if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER || HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX)
                {
                    AvailablePart availablePart = PartLoader.getPartInfoByName("Proximity");

                    if (availablePart != null)
                    {
                        // This looks weird, because we get the part, find the node that it is in, loop through the parts in that node,
                        // and look at PartModelPurchased() when we get to the part we started with. 
                        // Why not just call PartModelPurchased() on the part that we first get? Because it doesn't work, it always returns true.
                        // So this code looks as if it was written by the Department of Redundancy Department, but it is necessary to do it this way.

                        ProtoTechNode techNode = ResearchAndDevelopment.Instance.GetTechState(availablePart.TechRequired);

                        if (techNode != null)
                        {
                            List<AvailablePart> lap = techNode.partsPurchased;

                            foreach (AvailablePart p in lap)
                            {
                                if (p.name == "Proximity")
                                {
                                    if (ResearchAndDevelopment.PartModelPurchased(p))
                                    {
                                        Initialize();
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    Initialize();
                }
            }
            catch (Exception ex) { print("Proximity startup exception - " + ex.Message); }
        }

        private void Initialize()
        {
            TechAvailable = true;
            print("Proximity initializing");

            if (!initialized)
            {
                GameEvents.onHideUI.Add(new EventVoid.OnEvent(OnHideUI));
                GameEvents.onShowUI.Add(new EventVoid.OnEvent(OnShowUI));

                CheckRealChutes();

                initialized = true;
            }
        }

        private void CheckRealChutes()
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.FullName.Contains("RealChute"))
                {
                    Type[] assemblyTypes = assembly.GetTypes();
                    foreach (Type t in assemblyTypes)
                    {
                        if (t.Name.Contains("RealChuteModule"))
                        {
                            RealChutes = true;
                            return;
                        }
                    }
                }
            }
        }

        private void OnShowUI()
        {
            Proximity.HideUI = false;
        }

        private void OnHideUI()
        {
            Proximity.HideUI = true;
        }
    }
}
