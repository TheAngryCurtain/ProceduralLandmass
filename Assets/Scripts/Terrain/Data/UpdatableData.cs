using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdatableData : ScriptableObject
{
    public event System.Action OnValuesUpdated;
    public bool autoUpdate;

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        if (autoUpdate)
        {
            //UnityEditor.EditorApplication.update -= NotifyOfValueUpdate;
            //UnityEditor.EditorApplication.update += NotifyOfValueUpdate;
            NotifyOfValueUpdate();
        }
    }

    public void NotifyOfValueUpdate()
    {
        if (OnValuesUpdated != null)
        {
            OnValuesUpdated();
        }
    }
#endif
}
