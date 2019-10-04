using System;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace SimpleBind
{
	[Serializable]
	public class DataSourceReference
	{
		[SerializeField] [HideInInspector] private ViewModel viewModel;
		[SerializeField] [HideInInspector] private string dataSourceName;

		private ViewModel cachedViewModel;
		private string cachedDataSourceName;

		public IDataSource Source
		{
			get
			{
				if (source == null || viewModel != cachedViewModel || dataSourceName != cachedDataSourceName)
				{
					cachedViewModel = viewModel;
					cachedDataSourceName = dataSourceName;
					if (viewModel == null || string.IsNullOrEmpty(dataSourceName)) return null;
					var field = viewModel.GetType().GetField(dataSourceName);
					if (field != null)
					{
						source = (IDataSource) field.GetValue(viewModel);
					}
				}
				return source;
			}
		}

		private IDataSource source;

		public bool TryGetValue<T>(out T value)
		{
			value = default;
			var maybeSource = Source;
			if (maybeSource == null) return false;
			value = maybeSource.GetValue<T>();
			return true;
		}
	}

#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(DataSourceReference))]
	public class DataSourceReferencePropertyDrawer : PropertyDrawer
	{
		private static readonly string[] BaseDataSourceArray = {"NONE"};

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (property.serializedObject.isEditingMultipleObjects) return;

			var labelWidth = EditorGUIUtility.labelWidth;
			var lineHeight = EditorGUIUtility.singleLineHeight;

			var indent = EditorGUI.indentLevel;
			EditorGUI.LabelField(new Rect(position.x, position.y, labelWidth, position.height), property.displayName);
			position.x += labelWidth;
			position.width -= labelWidth;

			var binding = property.serializedObject.targetObject as DataBinding;
			if (binding && binding.ViewModel)
			{
				var viewModelProp = property.FindPropertyRelative("viewModel");
				var dataSourceNameProp = property.FindPropertyRelative("dataSourceName");

				var viewModelType = binding.ViewModel.GetType();
				var dataSourceNames = ViewModelEditor.GetDataSourceFieldNames(viewModelType).ToArray();
				var dataSourceDescriptions = BaseDataSourceArray.Concat(ViewModelEditor.GetDataSourceDescriptions(viewModelType)).ToArray();

				var index = -1;
				if (!string.IsNullOrEmpty(dataSourceNameProp.stringValue)) index = Array.IndexOf(dataSourceNames, dataSourceNameProp.stringValue);
				if (index == -1) index = 0;
				else index++;

				EditorGUI.BeginChangeCheck();
				viewModelProp.objectReferenceValue = binding.ViewModel;
				index = EditorGUI.Popup(new Rect(position.x, position.y, position.width, lineHeight), index, dataSourceDescriptions);
				dataSourceNameProp.stringValue = index == 0 ? null : dataSourceNames[index - 1];
				if (EditorGUI.EndChangeCheck()) property.serializedObject.ApplyModifiedProperties();
			}

			EditorGUI.indentLevel = indent;
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUIUtility.singleLineHeight;
		}
	}
#endif
}