using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Rendering.Universal;
using System;
using UnityEditor.Rendering;
using UnityEngine;

namespace Kimi
{
    [CustomEditor(typeof(KForwardRenderRenderData), true)]
    public class KForwardRenderRenderDataEditor : Editor
    {
        class Styles
        {
            public static readonly GUIContent RenderPasses =
                new GUIContent("Renderer Passes",
                    "添加或者删除Render Pass.");

            public static readonly GUIContent PassNameField =
                new GUIContent("Name", "Render pass name. This name is the name displayed in Frame Debugger.");

            public static readonly GUIContent MissingFeature = new GUIContent("Missing RendererFeature",
                "Missing reference, due to compilation issues or missing files. you can attempt auto fix or choose to remove the feature.");

            public static GUIStyle BoldLabelSimple;

            static Styles()
            {
                BoldLabelSimple = new GUIStyle(EditorStyles.label);
                BoldLabelSimple.fontStyle = FontStyle.Bold;
            }
        }
        

        private SerializedProperty m_RendererPasses;
        private SerializedProperty m_RendererPassMap;
        
        
        private SerializedProperty m_PassFalseBool;
        List<Editor> m_PassEditors = new List<Editor>();
    
        private void OnEnable()
        {
            m_RendererPasses = serializedObject.FindProperty("m_BaseRenderPasses");
            m_RendererPassMap = serializedObject.FindProperty("m_BaseRenderPassMap");
            var editorObj = new SerializedObject(this);
            m_PassFalseBool = editorObj.FindProperty(nameof(m_PassFalseBool));
            UpdateEditorList();
        }
        
        private void OnDisable()
        {
            ClearEditorsList();
        }
        

        public override void OnInspectorGUI()
        {
            if (m_RendererPasses == null)
                OnEnable();
            else if (m_RendererPasses.arraySize != m_PassEditors.Count)
                UpdateEditorList();
            
            serializedObject.Update();
            DrawRendererPassList();
        }

        private void DrawRendererPassList()
        {
            EditorGUILayout.LabelField(Styles.RenderPasses, EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            if (m_RendererPasses.arraySize == 0)
            {
                EditorGUILayout.HelpBox("No Renderer Passes added", MessageType.Info);
            }
            else
            {
                //Draw List
                CoreEditorUtils.DrawSplitter();
                for (int i = 0; i < m_RendererPasses.arraySize; i++)
                {
                    SerializedProperty renderPassesProperty = m_RendererPasses.GetArrayElementAtIndex(i);
                    DrawRendererPass(i, ref renderPassesProperty);
                    CoreEditorUtils.DrawSplitter();
                }
            }
            
            EditorGUILayout.Space();
            
            //Add renderer
            if (GUILayout.Button("Add Renderer Pass", EditorStyles.miniButton))
            {
                AddPassMenu();
            }
            
            if (GUILayout.Button("Refresh Renderer Pass", EditorStyles.miniButton))
            {
                var data = target as KForwardRenderRenderData;
                if (data)
                    data.RefreshRenderPasses();
            }
        }

        private void DrawRendererPass(int index, ref SerializedProperty renderPassProperty)
        {
            var rendererPassObjRef = renderPassProperty.objectReferenceValue;
            if (rendererPassObjRef != null)
            {
                bool hasChangedProperties = false;
                string title = ObjectNames.GetInspectorTitle(rendererPassObjRef);
                
                // Get the serialized object for the editor script & update it
                var rendererPassEditor = m_PassEditors[index];
                SerializedObject serializedRendererPassEditor = rendererPassEditor.serializedObject;
                serializedRendererPassEditor.Update();
                // Foldout header
                EditorGUI.BeginChangeCheck();
                SerializedProperty activeProperty = serializedRendererPassEditor.FindProperty("m_Active");
                bool displayContent = CoreEditorUtils.DrawHeaderToggle(title, renderPassProperty, activeProperty, pos => OnContextClick(pos, index));
                hasChangedProperties |= EditorGUI.EndChangeCheck();
                
                // ObjectEditor
                if (displayContent)
                {
                    EditorGUI.BeginChangeCheck();
                    SerializedProperty nameProperty = serializedRendererPassEditor.FindProperty("m_Name");
                    nameProperty.stringValue = ValidateName(EditorGUILayout.DelayedTextField(Styles.PassNameField, nameProperty.stringValue));
                    if (EditorGUI.EndChangeCheck())
                    {
                        hasChangedProperties = true;

                        // We need to update sub-asset name
                        rendererPassObjRef.name = nameProperty.stringValue;

                        AssetDatabase.SaveAssets();

                        // Triggers update for sub-asset name change
                        ProjectWindowUtil.ShowCreatedAsset(target);
                    }

                    EditorGUI.BeginChangeCheck();
                    rendererPassEditor.OnInspectorGUI();
                    hasChangedProperties |= EditorGUI.EndChangeCheck();

                    EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
                }

                // Apply changes and save if the user has modified any settings
                if (hasChangedProperties)
                {
                    serializedRendererPassEditor.ApplyModifiedProperties();
                    serializedObject.ApplyModifiedProperties();


                    var pass = rendererPassEditor.target as BaseRenderPass;
                    if (pass is not null) pass.Refresh();
                    ForceSave();
                }
            }
            else
            {
                // CoreEditorUtils.DrawHeaderToggle(Styles.MissingFeature,renderPassProperty, m_PassFalseBool,pos => OnContextClick(pos, index));
                // m_PassFalseBool.boolValue = false; // always make sure false bool is false
                EditorGUILayout.HelpBox(Styles.MissingFeature.tooltip, MessageType.Error);
                if (GUILayout.Button("Attempt Fix", EditorStyles.miniButton))
                {
                    RemoveComponent(index);
                    // var data = target as CustomRendererData;
                    // // if (data) data.ValidateRendererPasses();
                    // if (data)
                }
            }
        }
        
        private void OnContextClick(Vector2 position, int id)
        {
            var menu = new GenericMenu();

            if (id == 0)
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Move Up"));
            else
                menu.AddItem(EditorGUIUtility.TrTextContent("Move Up"), false, () => MoveComponent(id, -1));

            if (id == m_RendererPasses.arraySize - 1)
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Move Down"));
            else
                menu.AddItem(EditorGUIUtility.TrTextContent("Move Down"), false, () => MoveComponent(id, 1));

            menu.AddSeparator(string.Empty);
            menu.AddItem(EditorGUIUtility.TrTextContent("Remove"), false, () => RemoveComponent(id));

            menu.DropDown(new Rect(position, Vector2.zero));
        }

        
        private void AddPassMenu()
        {
            GenericMenu menu = new GenericMenu();
            TypeCache.TypeCollection types = TypeCache.GetTypesDerivedFrom<BaseRenderPass>();
            foreach (Type type in types)
            {
                var data = target as BaseRenderPass;
                // if (data.DuplicateFeatureCheck(type))
                // {
                //     continue;
                // }

                string path = GetMenuNameFromType(type);
                menu.AddItem(new GUIContent(path), false, AddComponent, type.Name);
            }
            menu.ShowAsContext();
        }
        
        private void AddComponent(object type)
        {
            serializedObject.Update();

            var component = CreateInstance((string)type);
            component.name = $"New{(string)type}";
            Undo.RegisterCreatedObjectUndo(component, "Add Renderer Feature");

            // Store this new effect as a sub-asset so we can reference it safely afterwards
            // Only when we're not dealing with an instantiated asset
            if (EditorUtility.IsPersistent(target))
            {
                AssetDatabase.AddObjectToAsset(component, target);
            }
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(component, out var guid, out long localId);

            // Grow the list first, then add - that's how serialized lists work in Unity
            m_RendererPasses.arraySize++;
            SerializedProperty componentProp = m_RendererPasses.GetArrayElementAtIndex(m_RendererPasses.arraySize - 1);
            componentProp.objectReferenceValue = component;

            // Update GUID Map
            m_RendererPassMap.arraySize++;
            SerializedProperty guidProp = m_RendererPassMap.GetArrayElementAtIndex(m_RendererPassMap.arraySize - 1);
            guidProp.longValue = localId;
            UpdateEditorList();
            serializedObject.ApplyModifiedProperties();

            // Force save / refresh
            if (EditorUtility.IsPersistent(target))
            {
                ForceSave();
            }
            serializedObject.ApplyModifiedProperties();
        }
        
        private void RemoveComponent(int id)
        {
            SerializedProperty property = m_RendererPasses.GetArrayElementAtIndex(id);
            var component = property.objectReferenceValue;
            property.objectReferenceValue = null;

            Undo.SetCurrentGroupName(component == null ? "Remove Renderer Feature" : $"Remove {component.name}");

            // remove the array index itself from the list
            m_RendererPasses.DeleteArrayElementAtIndex(id);
            m_RendererPassMap.DeleteArrayElementAtIndex(id);
            UpdateEditorList();
            serializedObject.ApplyModifiedProperties();

            // Destroy the setting object after ApplyModifiedProperties(). If we do it before, redo
            // actions will be in the wrong order and the reference to the setting object in the
            // list will be lost.
            if (component != null)
            {
                Undo.DestroyObjectImmediate(component);
            }

            // Force save / refresh
            ForceSave();
        }

        private void MoveComponent(int id, int offset)
        {
            Undo.SetCurrentGroupName("Move Render Pass");
            serializedObject.Update();
            m_RendererPasses.MoveArrayElement(id, id + offset);
            m_RendererPassMap.MoveArrayElement(id, id + offset);
            UpdateEditorList();
            serializedObject.ApplyModifiedProperties();

            // Force save / refresh
            ForceSave();
        }
        
        
        private string GetMenuNameFromType(Type type)
        {
            var path = type.Name;
            if (type.Namespace != null)
            {
                if (type.Namespace.Contains("Experimental"))
                    path += " (Experimental)";
            }

            // Inserts blank space in between camel case strings
            return Regex.Replace(Regex.Replace(path, "([a-z])([A-Z])", "$1 $2", RegexOptions.Compiled),
                "([A-Z])([A-Z][a-z])", "$1 $2", RegexOptions.Compiled);
        }
        
        private string ValidateName(string name)
        {
            name = Regex.Replace(name, @"[^a-zA-Z0-9 ]", "");
            return name;
        }

        
        private void UpdateEditorList()
        {
            ClearEditorsList();
            for (int i = 0; i < m_RendererPasses.arraySize; i++)
            {
                m_PassEditors.Add(CreateEditor(m_RendererPasses.GetArrayElementAtIndex(i).objectReferenceValue));
            }
        }
        
        private void ClearEditorsList()
        {
            for (int i = m_PassEditors.Count - 1; i >= 0; --i)
            {
                DestroyImmediate(m_PassEditors[i]);
            }
            m_PassEditors.Clear();
        }
        
        private void ForceSave()
        {
            EditorUtility.SetDirty(target);
        }
        
        
    }
}
