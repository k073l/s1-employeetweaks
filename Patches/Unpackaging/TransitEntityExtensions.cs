#if MONO
using FishNet.Object;
using ScheduleOne.ItemFramework;
using ScheduleOne.Management;
#else
using Il2CppScheduleOne.Management;
using Il2CppFishNet.Object;
using Il2CppScheduleOne.ItemFramework;
#endif
using UnityEngine;

namespace EmployeeTweaks.Patches.Unpackaging;

public static class TransitEntityExtensions
{
    public static List<ItemSlot> ReserveOutputSlotsForItem(this ITransitEntity entity, ItemInstance item,
        NetworkObject locker)
    {
        List<ItemSlot> list = [];
        var num = item.Quantity;
        for (var i = 0; i < entity.OutputSlots.Count; i++)
        {
            var capacityForItem = entity.OutputSlots[i].GetCapacityForItem(item);
            if (capacityForItem != 0)
            {
                var num2 = Mathf.Min(capacityForItem, num);
                num -= num2;
                entity.OutputSlots[i].ApplyLock(locker, "Employee is about to place an item here");
                list.Add(entity.OutputSlots[i]);
                if (num <= 0)
                {
                    break;
                }
            }
        }

        return list;
    }

    public static void RemoveOutputSlotLocks(this ITransitEntity entity, NetworkObject locker)
    {
        for (var i = 0; i < entity.OutputSlots.Count; i++)
        {
            if (entity.OutputSlots[i].ActiveLock != null && entity.OutputSlots[i].ActiveLock.LockOwner == locker)
            {
                entity.OutputSlots[i].RemoveLock();
            }
        }
    }
}