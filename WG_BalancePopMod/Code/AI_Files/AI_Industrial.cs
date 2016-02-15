﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices; 
using System.Linq;
using System.Text;
using ColossalFramework.Math;
using ColossalFramework.Plugins;
using UnityEngine;

namespace WG_BalancedPopMod
{
    class IndustrialBuildingAIMod : IndustrialBuildingAI
    {
        private static Dictionary<ulong, buildingWorkVisitorStruct> buildingEmployCache = new Dictionary<ulong, buildingWorkVisitorStruct>(DataStore.CACHE_SIZE);
        private static Dictionary<ulong, consumeStruct> consumeCache = new Dictionary<ulong, consumeStruct>(DataStore.CACHE_SIZE);

        public static void clearCache()
        {
            buildingEmployCache.Clear();
            consumeCache.Clear();
        }

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
        public override void CalculateWorkplaceCount(Randomizer r, int width, int length, out int level0, out int level1, out int level2, out int level3)
        {
            ulong seed = r.seed;
            BuildingInfo item = this.m_info;
            int level = (int)(item.m_class.m_level >= 0 ? item.m_class.m_level : 0); // Force it to 0 if the level was set to None

            bool needRefresh = true;
            buildingWorkVisitorStruct cachedLevel;
            if (buildingEmployCache.TryGetValue(seed, out cachedLevel))
            {
                needRefresh = cachedLevel.level != level;
            }

            if (needRefresh)
            {
                buildingEmployCache.Remove(seed);
                consumeCache.Remove(seed);

                prefabEmployStruct output;
                // If not seen prefab, calculate
                if (!DataStore.prefabWorkerVisit.TryGetValue(item.gameObject.GetHashCode(), out output))
                {
                    int[] array = getArray(item.m_class, level);
                    AI_Utils.calculateprefabWorkerVisit(width, length, ref item, 3, ref array, out output);
                    DataStore.prefabWorkerVisit.Add(item.gameObject.GetHashCode(), output);
                }

                cachedLevel.level = level;
                cachedLevel.level0 = output.level0;
                cachedLevel.level1 = output.level1;
                cachedLevel.level2 = output.level2;
                cachedLevel.level3 = output.level3;

                // Update the level for the new building
                buildingEmployCache.Add(seed, cachedLevel);
            }

            level0 = cachedLevel.level0;
            level1 = cachedLevel.level1;
            level2 = cachedLevel.level2;
            level3 = cachedLevel.level3;
        }


        public override void ReleaseBuilding(ushort buildingID, ref Building data)
        {
            buildingEmployCache.Remove(new Randomizer((int)buildingID).seed);
            consumeCache.Remove(new Randomizer((int)buildingID).seed);
            base.ReleaseBuilding(buildingID, ref data);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="r"></param>
        /// <param name="productionRate"></param>
        /// <param name="electricityConsumption"></param>
        /// <param name="waterConsumption"></param>
        /// <param name="sewageAccumulation"></param>
        /// /// <param name="garbageAccumulation"></param>
        /// <param name="incomeAccumulation"></param>
        public override void GetConsumptionRates(Randomizer r, int productionRate, out int electricityConsumption, out int waterConsumption, out int sewageAccumulation, out int garbageAccumulation, out int incomeAccumulation)
        {
            ulong seed = r.seed;
            ItemClass item = this.m_info.m_class;
            consumeStruct output;
            bool needRefresh = true;

            if (consumeCache.TryGetValue(seed, out output))
            {
                needRefresh = output.productionRate != productionRate;
            }

            if (needRefresh)
            {
                consumeCache.Remove(seed);
                int level = (int)(item.m_level >= 0 ? item.m_level : 0); // Force it to 0 if the level was set to None
                int[] array = getArray(item, level);

                electricityConsumption = array[DataStore.POWER];
                waterConsumption = array[DataStore.WATER];
                sewageAccumulation = array[DataStore.SEWAGE];
                garbageAccumulation = array[DataStore.GARBAGE];
                incomeAccumulation = array[DataStore.INCOME];

                if (electricityConsumption != 0)
                {
                    electricityConsumption = Mathf.Max(100, productionRate * electricityConsumption + r.Int32(100u)) / 100;
                }
                if (waterConsumption != 0)
                {
                    int num = r.Int32(100u);
                    waterConsumption = Mathf.Max(100, productionRate * waterConsumption + num) / 100;
                    if (sewageAccumulation != 0)
                    {
                        sewageAccumulation = Mathf.Max(100, productionRate * sewageAccumulation + num) / 100;
                    }
                }
                else if (sewageAccumulation != 0)
                {
                    sewageAccumulation = Mathf.Max(100, productionRate * sewageAccumulation + r.Int32(100u)) / 100;
                }
                if (garbageAccumulation != 0)
                {
                    garbageAccumulation = Mathf.Max(100, productionRate * garbageAccumulation + r.Int32(100u)) / 100;
                }
                if (incomeAccumulation != 0)
                {
                    incomeAccumulation = productionRate * incomeAccumulation;
                }

                output.productionRate = productionRate;
                output.electricity = electricityConsumption;
                output.water = waterConsumption;
                output.sewage = sewageAccumulation;
                output.garbage = garbageAccumulation;
                output.income = incomeAccumulation;

                consumeCache.Add(seed, output);
            }
            else
            {
                productionRate = output.productionRate;
                electricityConsumption = output.electricity;
                waterConsumption = output.water;
                sewageAccumulation = output.sewage;
                garbageAccumulation = output.garbage;
                incomeAccumulation = output.income;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="productionRate"></param>
        /// <param name="cityPlanningPolicies"></param>
        /// <param name="groundPollution"></param>
        /// <param name="noisePollution"></param>
        public override void GetPollutionRates(int productionRate, DistrictPolicies.CityPlanning cityPlanningPolicies, out int groundPollution, out int noisePollution)
        {
            ItemClass @class = this.m_info.m_class;
            groundPollution = 0;
            noisePollution = 0;
            int level = (int)(@class.m_level >= 0 ? @class.m_level : 0); // Force it to 0 if the level was set to None
            int[] array = getArray(@class, level);

            groundPollution = (productionRate * array[DataStore.GROUND_POLLUTION]) / 100;
            noisePollution = (productionRate * array[DataStore.NOISE_POLLUTION]) / 100;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="r"></param>
        /// <param name="width"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public override int CalculateProductionCapacity(Randomizer r, int width, int length)
        {
            ItemClass @class = this.m_info.m_class;
            int level = (int)(@class.m_level >= 0 ? @class.m_level : 0); // Force it to 0 if the level was set to None
            int[] array = getArray(@class, level);
            return Mathf.Max(100, width * length * array[DataStore.PRODUCTION] + r.Int32(100u)) / 100;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int[] getArray(ItemClass item, int level)
        {
            switch (item.m_subService)
            {
                case ItemClass.SubService.IndustrialOre:
                    return DataStore.industry_ore[level + 1];

                case ItemClass.SubService.IndustrialForestry:
                    return DataStore.industry_forest[level + 1];

                case ItemClass.SubService.IndustrialFarming:
                    return DataStore.industry_farm[level + 1];

                case ItemClass.SubService.IndustrialOil:
                    return DataStore.industry_oil[level + 1];

                case ItemClass.SubService.IndustrialGeneric:  // Deliberate fall through
                default:
                    return DataStore.industry[level];
            }
        }
    }
}