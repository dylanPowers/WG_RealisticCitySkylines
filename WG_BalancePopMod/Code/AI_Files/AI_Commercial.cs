﻿using ColossalFramework.Math;
using UnityEngine;
using Boformer.Redirection;

namespace WG_BalancedPopMod
{
    [TargetType(typeof(CommercialBuildingAI))]
    public class CommercialBuildingAIMod : CommercialBuildingAI
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="r"></param>
        /// <param name="width"></param>
        /// <param name="length"></param>
        /// <param name="level0"></param>
        /// <param name="level1"></param>
        /// <param name="level2"></param>
        /// <param name="level3"></param>
        [RedirectMethod(true)]
        public override void CalculateWorkplaceCount(Randomizer r, int width, int length, out int level0, out int level1, out int level2, out int level3)
        {
            ulong seed = r.seed;
            BuildingInfo item = this.m_info;
            int level = (int)(item.m_class.m_level >= 0 ? item.m_class.m_level : 0); // Force it to 0 if the level was set to None

            prefabEmployStruct output;
            // If not seen prefab, calculate
            if (!DataStore.prefabWorkerVisit.TryGetValue(item.gameObject.GetHashCode(), out output))
            {
                int[] array = getArray(this.m_info, level);
                AI_Utils.calculateprefabWorkerVisit(width, length, ref item, 4, ref array, out output);
                DataStore.prefabWorkerVisit.Add(item.gameObject.GetHashCode(), output);
            }

            level0 = output.level0;
            level1 = output.level1;
            level2 = output.level2;
            level3 = output.level3;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="r"></param>
        /// <param name="productionRate"></param>
        /// <param name="electricityConsumption"></param>
        /// <param name="waterConsumption"></param>
        /// <param name="sewageAccumulation"></param>
        /// <param name="garbageAccumulation"></param>
        /// <param name="incomeAccumulation"></param>
        [RedirectMethod(true)]
        public override void GetConsumptionRates(Randomizer r, int productionRate, out int electricityConsumption, out int waterConsumption, out int sewageAccumulation, out int garbageAccumulation, out int incomeAccumulation)
        {
            ItemClass item = this.m_info.m_class;

            int level = (int)(item.m_level >= 0 ? item.m_level : 0); // Force it to 0 if the level was set to None
            int[] array = getArray(this.m_info, level);

            electricityConsumption = array[DataStore.POWER];
            waterConsumption = array[DataStore.WATER];
            sewageAccumulation = array[DataStore.SEWAGE];
            garbageAccumulation = array[DataStore.GARBAGE];

            int landVal = AI_Utils.getLandValueIncomeComponent(r.seed);
            incomeAccumulation = array[DataStore.INCOME] + landVal;

            electricityConsumption = Mathf.Max(100, productionRate * electricityConsumption) / 100;
            waterConsumption = Mathf.Max(100, productionRate * waterConsumption) / 100;
            sewageAccumulation = Mathf.Max(100, productionRate * sewageAccumulation) / 100;
            garbageAccumulation = Mathf.Max(100, productionRate * garbageAccumulation) / 100;
            incomeAccumulation = productionRate * incomeAccumulation;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="productionRate"></param>
        /// <param name="cityPlanningPolicies"></param>
        /// <param name="groundPollution"></param>
        /// <param name="noisePollution"></param>
        [RedirectMethod(true)]
        public override void GetPollutionRates(int productionRate, DistrictPolicies.CityPlanning cityPlanningPolicies, out int groundPollution, out int noisePollution)
        {
            ItemClass item = this.m_info.m_class;
            int level = (int)(item.m_level >= 0 ? item.m_level : 0); // Force it to 0 if the level was set to None
            int[] array = getArray(this.m_info, level);

            groundPollution = array[DataStore.GROUND_POLLUTION];
            noisePollution = (productionRate * array[DataStore.NOISE_POLLUTION]) / 100;
            if (item.m_subService == ItemClass.SubService.CommercialLeisure)
            {
              if ((cityPlanningPolicies & DistrictPolicies.CityPlanning.NoLoudNoises) != DistrictPolicies.CityPlanning.None)
              {
                  noisePollution /= 2;
              }
            }
        }

        [RedirectMethod(true)]
        public override int CalculateVisitplaceCount(Randomizer r, int width, int length)
        {
            int returnVal = 0;
            prefabEmployStruct visitors;
            if (DataStore.prefabWorkerVisit.TryGetValue(this.m_info.gameObject.GetHashCode(), out visitors))
            {
                returnVal = visitors.visitors;
                // All commercial places will need visitors. CalcWorkplaces is called first. But just return 0 otherwise.
            }
            return returnVal;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        private int[] getArray(BuildingInfo item, int level)
        {
            int[][] array = DataStore.commercialLow;

            try
            {
                switch (item.m_class.m_subService)
                {
                    case ItemClass.SubService.CommercialLeisure:
                        array = DataStore.commercialLeisure;
                        break;
                
                    case ItemClass.SubService.CommercialTourist:
                        array = DataStore.commercialTourist;
                        break;
                
                    case ItemClass.SubService.CommercialHigh:
                        array = DataStore.commercialHigh;
                        break;

                    case ItemClass.SubService.CommercialLow:
                    default:
                        break;
                }

                return array[level];
            }
            catch (System.Exception)
            {
                string error = item.gameObject.name + " attempted to be use " + item.m_class.m_subService.ToString() + " with level " + level + ". Returning as level 0.";
                Debugging.writeDebugToFile(error);
                return array[0];
            }
        }
    }
}