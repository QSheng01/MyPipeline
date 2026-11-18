using System;
using System.Collections.Generic;
using UnityEngine;

namespace PostProcess.Scripts.ExpandFunctionality
{
    // [Serializable]
    // public class PriorityQueue : IMono
    // {
    //     [Header("全局区域")]
    //     private static List<ISingleCell> m_priorityQueueItems = new List<ISingleCell>();
    //     public  static List<ISingleCell> PriorityQueueItems => m_priorityQueueItems;
    //     
    //     private static ISingleCell m_FirstItem = null;
    //     
    //     public static T GetFirstItem<T>() where T : MonoSynthesizer
    //     {
    //         return m_FirstItem as T;
    //     }
    //     
    //     [Header("对象区域")]
    //     private int lastID = 0;
    //     
    //     public void OnEnable(ISingleCell cell)
    //     {
    //         if (!m_priorityQueueItems.Contains(cell))
    //         {
    //             m_priorityQueueItems.Add(cell);
    //         }
    //         
    //         RefreshFirstItem();
    //     }
    //     
    //     public void OnDisable(ISingleCell cell)
    //     {
    //         if (m_priorityQueueItems is { })
    //         {
    //             m_priorityQueueItems.Remove(cell);
    //         }
    //         
    //         RefreshFirstItem();
    //     }
    //     
    //     public void OnUpdate(ISingleCell cell)
    //     {
    //         // var item = cell as MonoSynthesizer;
    //         // if (item is { } && lastID != item.ID)
    //         // {
    //         //     RefreshFirstItem();
    //         // }
    //         // 
    //         // if (item is { }) lastID = item.ID;
    //     }
    //     
    //     
    //     public void RefreshFirstItem()
    //     {
    //         // MonoSynthesizer minNode = null;
    //         // foreach (var item in m_priorityQueueItems)
    //         // {
    //         //     var node = item as MonoSynthesizer;
    //         //     
    //         //     if (node.enabled == false)
    //         //     {
    //         //         continue;    
    //         //     }
    //         // 
    //         //     if (minNode is null)
    //         //     {
    //         //         minNode = node;
    //         //     }
    //         //     else
    //         //     {
    //         //         if (node.ID < minNode.ID)
    //         //         {
    //         //             minNode = node;
    //         //         }
    //         //     }   
    //         // }
    //         // 
    //         // m_FirstItem = minNode;
    //     }
    // }
}