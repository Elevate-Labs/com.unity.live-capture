using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.LiveCapture
{
    /// <summary>
    /// A property drawer for enums. Does not block the main thread.
    /// </summary>
    [CustomPropertyDrawer(typeof(EnumButtonGroupAttribute))]
    class EnumButtonGroupAttributeDrawer : PropertyDrawer
    {
        // An interface allowing us to mock GUI calls in tests.
        internal interface IGui
        {
            bool Toggle(Rect position, bool value, GUIContent content);
            void LabelField(Rect position, GUIContent label);
            float GetSegmentWidth(PropertyDrawer drawer);

            float singleLineHeight { get; }
            float standardVerticalSpacing { get; }
            float currentViewWidth { get; }
            float labelWidth { get; }
        }

        class DefaultGUI : IGui
        {
            public bool Toggle(Rect position, bool value, GUIContent content)
            {
                return GUI.Toggle(position, value, content, EditorStyles.miniButton);
            }

            public void LabelField(Rect position, GUIContent label)
            {
                EditorGUI.LabelField(new Rect(position.x, position.y, EditorGUIUtility.labelWidth, position.height), label);
            }

            public float GetSegmentWidth(PropertyDrawer drawer)
            {
                return (drawer.attribute as EnumButtonGroupAttribute).segmentWidth;
            }

            public float singleLineHeight => EditorGUIUtility.singleLineHeight;
            public float standardVerticalSpacing => EditorGUIUtility.standardVerticalSpacing;
            public float currentViewWidth => EditorGUIUtility.currentViewWidth;
            public float labelWidth => EditorGUIUtility.labelWidth;
        }

        protected struct EnumValue
        {
            public string name;
            public string displayName;
            public int index;
            public bool selected;
            public bool newSelected;
        }

        // Bindings flags used to access field properties.
        const BindingFlags k_BindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        // Stores all descriptions for encountered enum types.
        static readonly Dictionary<Type, Dictionary<string, string>> k_DescriptionsStorage = new Dictionary<Type, Dictionary<string, string>>();

        // Store enum types without descriptions to avoid trying to read those descriptions repeatedly.
        static readonly HashSet<Type> k_TypesWithoutDescription = new HashSet<Type>();

        // Recycled temporary description storage
        static readonly Dictionary<string, string> k_TmpDescriptions = new Dictionary<string, string>();

        /// <summary>
        /// Fetches enum values descriptions. Those descriptions are implemented as <see cref="DescriptionAttribute" /> attributes.
        /// </summary>
        /// <param name="enumType">The type of the enum.</param>
        /// <param name="descriptions">The enum values descriptions.</param>
        /// <exception cref="InvalidOperationException">Thrown if the enum values information cannot be determined.</exception>
        static void FetchDescriptions(Type enumType, Dictionary<string, string> descriptions)
        {
            Assert.IsTrue(enumType.IsEnum);

            foreach (var enumValue in Enum.GetValues(enumType))
            {
                var memberInfos = enumType.GetMember(enumValue.ToString());

                if (memberInfos.Length == 0)
                {
                    throw new InvalidOperationException($"No {nameof(MemberInfo)} found for value {enumValue}.");
                }

                foreach (var description in memberInfos[0].GetCustomAttributes().OfType<DescriptionAttribute>())
                {
                    descriptions[enumValue.ToString()] = description.description;
                }
            }
        }

        /// <summary>
        /// Try to get the descriptions associated with enum values.
        /// </summary>
        /// <param name="property">The serialized property corresponding to the enum.</param>
        /// <param name="descriptions">The descriptions of the enum values.</param>
        /// <returns>True if there are descriptions associated to the enum values.</returns>
        static bool TryGetDescriptions(FieldInfo fieldInfo, out Dictionary<string, string> descriptions)
        {
            var enumType = fieldInfo.FieldType;

            if (!enumType.IsEnum)
            {
                throw new InvalidOperationException($"{enumType.FullName} is not an Enum.");
            }

            // The type was already encountered and we know it has no descriptions.
            if (k_TypesWithoutDescription.Contains(enumType))
            {
                descriptions = null;
                return false;
            }

            // Have we already fetched the descriptions for this enum type?
            if (k_DescriptionsStorage.TryGetValue(enumType, out descriptions))
            {
                return true;
            }

            // Try to fetch the descriptions for this enum type.
            k_TmpDescriptions.Clear();
            FetchDescriptions(enumType, k_TmpDescriptions);

            // The enum has descriptions, store them.
            if (k_TmpDescriptions.Count > 0)
            {
                var descriptionsCopy = new Dictionary<string, string>();
                foreach (var item in k_TmpDescriptions)
                {
                    descriptionsCopy.Add(item.Key, item.Value);
                }

                k_DescriptionsStorage.Add(enumType, descriptionsCopy);
                descriptions = descriptionsCopy;
                return true;
            }

            // The enum has no descriptions.
            k_TypesWithoutDescription.Add(enumType);
            return false;
        }

        // Can be exposed to subclasses for use in error messages.
        protected const string k_None = "None";
        protected const string k_All = "All";
        protected const string k_Everything = "Everything";

        static readonly string[] k_HiddenEntryNames = { k_None, k_All, k_Everything };

        // No concurrent access expected.
        protected static readonly List<EnumValue> k_TmpDisplayedEnumValues = new List<EnumValue>();

        // We reuse the same GUIContent to avoid allocations.
        static readonly GUIContent k_TmpGUIContent = new GUIContent();

        static readonly IGui k_Gui = new DefaultGUI();

        IGui m_Gui = k_Gui;

        // Meant to allow tests to override the GUI API.
        internal IGui gui
        {
            set { m_Gui = value; }
        }

        // Returns the list of enum values visible in the UI.
        // We filter out hidden entries such as *None* or *All*.
        protected virtual List<EnumValue> GetDisplayedEnumValues(SerializedProperty property)
        {
            k_TmpDisplayedEnumValues.Clear();

            var enumNames = property.enumNames;
            var enumDisplayNames = property.enumDisplayNames;
            var entryCount = enumDisplayNames.Length;

            for (var i = 0; i != entryCount; ++i)
            {
                var displayName = enumDisplayNames[i];
                if (ArrayUtility.Contains(k_HiddenEntryNames, displayName))
                {
                    continue;
                }

                k_TmpDisplayedEnumValues.Add(new EnumValue
                {
                    name = enumNames[i],
                    displayName = displayName,
                    index = i,
                    selected = IsSelected(property.intValue, i)
                });
            }

            return k_TmpDisplayedEnumValues;
        }

        /// <summary>
        /// Determines if a given enum value is selected.
        /// </summary>
        /// <param name="intValue">The enum as an integer.</param>
        /// <param name="index">The index of the enum value.</param>
        /// <returns>True if the enum value is selected.</returns>
        protected virtual bool IsSelected(int intValue, int index)
        {
            return intValue == index;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var enumValues = GetDisplayedEnumValues(property);
            UpdateLayout(enumValues.Count, out _, out _, out var rows);
            return rows * m_Gui.singleLineHeight + (rows - 1) * m_Gui.standardVerticalSpacing;
        }

        // Update layout values describing the grid of displayed enum values.
        void UpdateLayout(int entryCount, out float rowWidth, out int columns, out int rows)
        {
            var segmentWidth = m_Gui.GetSegmentWidth(this);
            rowWidth = m_Gui.currentViewWidth - m_Gui.labelWidth - 40;
            columns = Mathf.FloorToInt(rowWidth / segmentWidth);
            rows = Mathf.CeilToInt((float)entryCount / columns);
        }

        /// <summary>
        /// Update the serialized property value based on user-edited (displayed) values.
        /// </summary>
        /// <param name="property">The serialized property.</param>
        /// <param name="enumValues">The list of user-edited enum values.</param>
        protected virtual void UpdatePropertyValue(SerializedProperty property, List<EnumValue> enumValues)
        {
            var selected = -1;
            var newSelected = -1;
            var entryCount = enumValues.Count;

            for (var i = 0; i != entryCount; ++i)
            {
                var entry = enumValues[i];
                if (entry.selected)
                {
                    selected = entry.index;
                }
                else if (entry.newSelected)
                {
                    newSelected = entry.index;
                }
            }

            if (newSelected != -1)
            {
                property.intValue = newSelected;
                return;
            }

            // If there is a None entry, select it.
            var noneIndex = Array.IndexOf(property.enumNames, k_None);
            if (noneIndex != -1)
            {
                property.intValue = noneIndex;
                return;
            }

            if (selected != -1)
            {
                property.intValue = selected;
                return;
            }

            // If we made it here, no value is selected and we could not find a None value on the enum.
            throw new InvalidOperationException($"Could not find {k_None} value on enum {property.name}");
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var hasDescriptions = TryGetDescriptions(fieldInfo, out var descriptions);

            m_Gui.LabelField(new Rect(position.x, position.y, EditorGUIUtility.labelWidth, position.height), label);

            var enumValues = GetDisplayedEnumValues(property);

            UpdateLayout(enumValues.Count, out var rowWidth, out var columns, out _);

            var entryWidth = rowWidth / Mathf.Min(columns, enumValues.Count);

            for (var i = 0; i != enumValues.Count; ++i)
            {
                var entry = enumValues[i];
                var column = i % columns;
                var row = i / columns;

                var entryRect = GetEntryRect(entryWidth, row, column);
                entryRect.position += position.position;

                k_TmpGUIContent.text = entry.displayName;
                if (hasDescriptions && descriptions.TryGetValue(entry.name, out var tooltip))
                {
                    k_TmpGUIContent.tooltip = tooltip;
                }
                else
                {
                    k_TmpGUIContent.tooltip = null;
                }

                entry.newSelected = m_Gui.Toggle(entryRect, entry.selected, k_TmpGUIContent);
                enumValues[i] = entry;
            }

            UpdatePropertyValue(property, enumValues);
        }

        // Returns the local rect to draw an enum value.
        Rect GetEntryRect(float segmentWidth, int row, int column)
        {
            // Hardcoded 3px to fix right padding.
            return new Rect(
                m_Gui.labelWidth + 3 + segmentWidth * column,
                row * (m_Gui.singleLineHeight + m_Gui.standardVerticalSpacing),
                segmentWidth, m_Gui.singleLineHeight);
        }
    }
}
