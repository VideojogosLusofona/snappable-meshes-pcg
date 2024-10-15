﻿/// Credit Ziboo
/// Sourced from - http://forum.unity3d.com/threads/free-reorderable-list.364600/

namespace UnityEngine.UI.Extensions
{
    public class ReorderableListDebug : MonoBehaviour
    {
#if UNITY_2022_1_OR_NEWER
        public TMPro.TMP_Text DebugLabel;
#else
        public Text DebugLabel;
#endif

        void Awake()
        {
#if UNITY_2023_1_OR_NEWER
            foreach (var list in FindObjectsByType<ReorderableList>(FindObjectsSortMode.None))
#else
            foreach (var list in FindObjectsOfType<ReorderableList>())
#endif
            {
                list.OnElementDropped.AddListener(ElementDropped);
            }
        }

        private void ElementDropped(ReorderableList.ReorderableListEventStruct droppedStruct)
        {
            DebugLabel.text = "";
            DebugLabel.text += "Dropped Object: " + droppedStruct.DroppedObject.name + "\n";
            DebugLabel.text += "Is Clone ?: " + droppedStruct.IsAClone + "\n";
            if (droppedStruct.IsAClone)
                DebugLabel.text += "Source Object: " + droppedStruct.SourceObject.name + "\n";
            DebugLabel.text += string.Format("From {0} at Index {1} \n", droppedStruct.FromList.name, droppedStruct.FromIndex);
            DebugLabel.text += string.Format("To {0} at Index {1} \n", droppedStruct.ToList == null ? "Empty space" : droppedStruct.ToList.name, droppedStruct.ToIndex);
        }
    }
}