using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TEngine.Editor.Inspector
{
    [CustomEditor(typeof(CollectionModule))]
    internal sealed class CollectionModuleInspector : GameFrameworkInspector
    {
        private SerializedProperty m_AvailableProcedureTypeNames = null;
        private SerializedProperty m_EntranceProcedureTypeName = null;
        private SerializedProperty m_AvailableShutDownProcedureTypeNames = null;
        private SerializedProperty m_ShutDownProcedureTypeName = null;

        private string[] m_ProcedureTypeNames = null;
        private List<string> m_CurrentAvailableProcedureTypeNames = null;
        private int m_EntranceProcedureIndex = -1;
        private List<string> m_CurrentAvailableShutDownProcedureTypeNames = null;
        private int m_ShutDownProcedureIndex = -1;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            CollectionModule t = (CollectionModule)target;

            GUILayout.Label("InnerGameName:" + t.InnerGameName, EditorStyles.boldLabel);
            string packageName = t.PackageInfo != null ? t.PackageInfo.PackageName : string.Empty;
            string mainDLLName = t.PackageInfo != null ? t.PackageInfo.MainDLLName : string.Empty;
            GUILayout.Label("InnerGamePackageName:" + packageName, EditorStyles.boldLabel);
            GUILayout.Label("InnerGameMainDLLName:" + mainDLLName, EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                GUILayout.Label("Available Procedures", EditorStyles.boldLabel);
                if (m_ProcedureTypeNames.Length > 0)
                {
                    EditorGUILayout.BeginVertical("box");
                    {
                        foreach (string procedureTypeName in m_ProcedureTypeNames)
                        {
                            bool selected = m_CurrentAvailableProcedureTypeNames.Contains(procedureTypeName);
                            if (selected != EditorGUILayout.ToggleLeft(procedureTypeName, selected))
                            {
                                if (!selected)
                                {
                                    m_CurrentAvailableProcedureTypeNames.Add(procedureTypeName);
                                    WriteAvailableProcedureTypeNames();
                                }
                                else if (procedureTypeName != m_EntranceProcedureTypeName.stringValue)
                                {
                                    m_CurrentAvailableProcedureTypeNames.Remove(procedureTypeName);
                                    WriteAvailableProcedureTypeNames();
                                }
                            }
                        }
                    }
                    EditorGUILayout.EndVertical();
                }
                else
                {
                    EditorGUILayout.HelpBox("There is no available procedure.", MessageType.Warning);
                }

                if (m_CurrentAvailableProcedureTypeNames.Count > 0)
                {
                    EditorGUILayout.Separator();

                    int selectedIndex = EditorGUILayout.Popup("Entrance Procedure", m_EntranceProcedureIndex, m_CurrentAvailableProcedureTypeNames.ToArray());
                    if (selectedIndex != m_EntranceProcedureIndex)
                    {
                        m_EntranceProcedureIndex = selectedIndex;
                        m_EntranceProcedureTypeName.stringValue = m_CurrentAvailableProcedureTypeNames[selectedIndex];
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("Select available procedures first.", MessageType.Info);
                }
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                GUILayout.Label("Available ShutDown Procedures", EditorStyles.boldLabel);
                if (m_ProcedureTypeNames.Length > 0)
                {
                    EditorGUILayout.BeginVertical("box");
                    {
                        foreach (string procedureTypeName in m_ProcedureTypeNames)
                        {
                            bool selected = m_CurrentAvailableShutDownProcedureTypeNames.Contains(procedureTypeName);
                            if (selected != EditorGUILayout.ToggleLeft(procedureTypeName, selected))
                            {
                                if (!selected)
                                {
                                    m_CurrentAvailableShutDownProcedureTypeNames.Add(procedureTypeName);
                                    WriteAvailableShutDownProcedureTypeNames();
                                }
                                else if (procedureTypeName != m_EntranceProcedureTypeName.stringValue)
                                {
                                    m_CurrentAvailableShutDownProcedureTypeNames.Remove(procedureTypeName);
                                    WriteAvailableShutDownProcedureTypeNames();
                                }
                            }
                        }
                    }
                    EditorGUILayout.EndVertical();
                }
                else
                {
                    EditorGUILayout.HelpBox("There is no available procedure.", MessageType.Warning);
                }

                if (m_CurrentAvailableShutDownProcedureTypeNames.Count > 0)
                {
                    EditorGUILayout.Separator();

                    int selectedIndex = EditorGUILayout.Popup("ShutDown Procedure", m_ShutDownProcedureIndex, m_CurrentAvailableShutDownProcedureTypeNames.ToArray());
                    if (selectedIndex != m_ShutDownProcedureIndex)
                    {
                        m_ShutDownProcedureIndex = selectedIndex;
                        m_ShutDownProcedureTypeName.stringValue = m_CurrentAvailableShutDownProcedureTypeNames[selectedIndex];
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("Select available procedures first.", MessageType.Info);
                }
            }
            EditorGUI.EndDisabledGroup();

            serializedObject.ApplyModifiedProperties();

            Repaint();
        }

        protected override void OnCompileComplete()
        {
            base.OnCompileComplete();

            RefreshTypeNames();
        }

        private void OnEnable()
        {
            m_AvailableProcedureTypeNames = serializedObject.FindProperty("availableProcedureTypeNames");
            m_EntranceProcedureTypeName = serializedObject.FindProperty("entranceProcedureTypeName");
            m_AvailableShutDownProcedureTypeNames = serializedObject.FindProperty("availableShutDownProcedureTypeNames");
            m_ShutDownProcedureTypeName = serializedObject.FindProperty("shutDownProcedureTypeName");

            RefreshTypeNames();
        }

        private void RefreshTypeNames()
        {
            m_ProcedureTypeNames = Type.GetRuntimeTypeNames(typeof(ProcedureBase));
            ReadAvailableProcedureTypeNames();
            int oldCount = m_CurrentAvailableProcedureTypeNames.Count;
            m_CurrentAvailableProcedureTypeNames = m_CurrentAvailableProcedureTypeNames.Where(x => m_ProcedureTypeNames.Contains(x)).ToList();
            if (m_CurrentAvailableProcedureTypeNames.Count != oldCount)
            {
                WriteAvailableProcedureTypeNames();
            }
            else if (!string.IsNullOrEmpty(m_EntranceProcedureTypeName.stringValue))
            {
                m_EntranceProcedureIndex = m_CurrentAvailableProcedureTypeNames.IndexOf(m_EntranceProcedureTypeName.stringValue);
                if (m_EntranceProcedureIndex < 0)
                {
                    m_EntranceProcedureTypeName.stringValue = null;
                }
            }

            ReadAvailableShutDownProcedureTypeNames();
            int oldShutDownCount = m_CurrentAvailableShutDownProcedureTypeNames.Count;
            m_CurrentAvailableShutDownProcedureTypeNames = m_CurrentAvailableShutDownProcedureTypeNames.Where(x => m_ProcedureTypeNames.Contains(x)).ToList();
            if (m_CurrentAvailableShutDownProcedureTypeNames.Count != oldCount)
            {
                WriteAvailableShutDownProcedureTypeNames();
            }
            else if (!string.IsNullOrEmpty(m_ShutDownProcedureTypeName.stringValue))
            {
                m_ShutDownProcedureIndex = m_CurrentAvailableShutDownProcedureTypeNames.IndexOf(m_ShutDownProcedureTypeName.stringValue);
                if (m_ShutDownProcedureIndex < 0)
                {
                    m_ShutDownProcedureTypeName.stringValue = null;
                }
            }
            serializedObject.ApplyModifiedProperties();
        }

        private void ReadAvailableProcedureTypeNames()
        {
            m_CurrentAvailableProcedureTypeNames = new List<string>();
            int count = m_AvailableProcedureTypeNames.arraySize;
            for (int i = 0; i < count; i++)
            {
                m_CurrentAvailableProcedureTypeNames.Add(m_AvailableProcedureTypeNames.GetArrayElementAtIndex(i).stringValue);
            }
        }

        private void ReadAvailableShutDownProcedureTypeNames()
        {
            m_CurrentAvailableShutDownProcedureTypeNames = new List<string>();
            int count = m_AvailableShutDownProcedureTypeNames.arraySize;
            for (int i = 0; i < count; i++)
            {
                m_CurrentAvailableShutDownProcedureTypeNames.Add(m_AvailableShutDownProcedureTypeNames.GetArrayElementAtIndex(i).stringValue);
            }
        }

        private void WriteAvailableProcedureTypeNames()
        {
            m_AvailableProcedureTypeNames.ClearArray();
            if (m_CurrentAvailableProcedureTypeNames == null)
            {
                return;
            }

            m_CurrentAvailableProcedureTypeNames.Sort();
            int count = m_CurrentAvailableProcedureTypeNames.Count;
            for (int i = 0; i < count; i++)
            {
                m_AvailableProcedureTypeNames.InsertArrayElementAtIndex(i);
                m_AvailableProcedureTypeNames.GetArrayElementAtIndex(i).stringValue = m_CurrentAvailableProcedureTypeNames[i];
            }

            if (!string.IsNullOrEmpty(m_EntranceProcedureTypeName.stringValue))
            {
                m_EntranceProcedureIndex = m_CurrentAvailableProcedureTypeNames.IndexOf(m_EntranceProcedureTypeName.stringValue);
                if (m_EntranceProcedureIndex < 0)
                {
                    m_EntranceProcedureTypeName.stringValue = null;
                }
            }
        }

        private void WriteAvailableShutDownProcedureTypeNames()
        {
            m_AvailableShutDownProcedureTypeNames.ClearArray();
            if (m_CurrentAvailableShutDownProcedureTypeNames == null)
            {
                return;
            }

            m_CurrentAvailableShutDownProcedureTypeNames.Sort();
            int count = m_CurrentAvailableShutDownProcedureTypeNames.Count;
            for (int i = 0; i < count; i++)
            {
                m_AvailableShutDownProcedureTypeNames.InsertArrayElementAtIndex(i);
                m_AvailableShutDownProcedureTypeNames.GetArrayElementAtIndex(i).stringValue = m_CurrentAvailableShutDownProcedureTypeNames[i];
            }

            if (!string.IsNullOrEmpty(m_ShutDownProcedureTypeName.stringValue))
            {
                m_ShutDownProcedureIndex = m_CurrentAvailableShutDownProcedureTypeNames.IndexOf(m_ShutDownProcedureTypeName.stringValue);
                if (m_ShutDownProcedureIndex < 0)
                {
                    m_ShutDownProcedureTypeName.stringValue = null;
                }
            }
        }
    }
}