using System.Collections.Generic;
using UnityEngine;

public static class UIBlurBackgroundEntityManager
{
    // 对 UI 的 Blur 组件进行管理
    static readonly HashSet<UIBlurBGEntity> s_UIBlurEntities = new ();
    public static void AddUIBlurBGEntity(UIBlurBGEntity uiBlurEntity)
    {
        s_UIBlurEntities.Add(uiBlurEntity);
    }

    public static void RemoveUIBlurBGEntity(UIBlurBGEntity uiBlurEntity)
    {
        s_UIBlurEntities.Remove(uiBlurEntity);
    }

    public static HashSet<UIBlurBGEntity> GetUIBlurEntities()
    {
        return s_UIBlurEntities;
    }

    public static void ClearNullEntities()
    {
        int n = s_UIBlurEntities.RemoveWhere(entity => !entity);
        Debug.Log($"Removed {n} null entities in s_UIBlurEntities");
    }
}
