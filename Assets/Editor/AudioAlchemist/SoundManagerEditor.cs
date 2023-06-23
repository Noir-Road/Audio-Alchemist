using UnityEngine;
using UnityEditor;
using System.Linq;

[CustomEditor(typeof(AudioAlchemist))]
public class SoundManagerEditor : Editor
{
    SerializedProperty destroyOnLoadProperty;
    SerializedProperty soundSubjectsProperty;
    Texture2D headerImage;
    Texture2D addImage;
    Texture2D removeImage;
    Color arrayColor; // Color for the array background
    Color[] randomColors; // Array to store the fixed random colors for each array element
    float spacing = 10f; // Spacing between array elements

    void OnEnable()
    {
        destroyOnLoadProperty = serializedObject.FindProperty("destroyOnLoad");
        soundSubjectsProperty = serializedObject.FindProperty("soundSubjects");
        headerImage = EditorGUIUtility.Load("Assets/Editor/Audioalchemist/headerImage.png") as Texture2D;
        addImage = EditorGUIUtility.Load("Assets/Editor/Icons/add.png") as Texture2D;
        removeImage = EditorGUIUtility.Load("Assets/Editor/Icons/cancel.png") as Texture2D;
        arrayColor = new Color(0.9f, 0.9f, 0.9f); // Set the array background color

        // Generate fixed random colors for each array element
        randomColors = new Color[soundSubjectsProperty.arraySize];
        for (int i = 0; i < soundSubjectsProperty.arraySize; i++)
        {
            randomColors[i] = Random.ColorHSV();
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var headerRect = GUILayoutUtility.GetRect(0, int.MaxValue, 150, 150);

        GUI.DrawTexture(headerRect, headerImage, ScaleMode.ScaleToFit);

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(destroyOnLoadProperty);

        EditorGUI.indentLevel++;

        for (int i = 0; i < soundSubjectsProperty.arraySize; i++)
        {
            SerializedProperty soundSubjectProperty = soundSubjectsProperty.GetArrayElementAtIndex(i);

            // Draw the array background color
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 0f), arrayColor);

            // Draw the colored separator line
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 2f), randomColors[i]);

            EditorGUILayout.PropertyField(soundSubjectProperty);

            // Add spacing between array elements
            GUILayout.Space(spacing);

            // Add a remove button for each sound subject
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(removeImage, GUILayout.Height(35), GUILayout.Width(35)))
            {
                soundSubjectsProperty.DeleteArrayElementAtIndex(i);
                randomColors = randomColors.Where((_, index) => index != i).ToArray();
            }
            GUILayout.EndHorizontal();
        }

        EditorGUI.indentLevel--;

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUIStyle centeredLabelStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
        GUILayout.Label("Add Sounds Group", centeredLabelStyle);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button(addImage, GUILayout.Height(40), GUILayout.Width(40)))
        {
            soundSubjectsProperty.arraySize++;

            // Generate a new fixed random color for the new array element
            Color[] newRandomColors = new Color[soundSubjectsProperty.arraySize];
            for (int i = 0; i < soundSubjectsProperty.arraySize; i++)
            {
                if (i < soundSubjectsProperty.arraySize - 1)
                {
                    newRandomColors[i] = randomColors[i]; // Preserve the existing colors for existing array elements
                }
                else
                {
                    newRandomColors[i] = Random.ColorHSV(); // Generate a new random color for the new array element
                }
            }
            randomColors = newRandomColors; // Assign the new colors to the randomColors array
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        serializedObject.ApplyModifiedProperties();
    }
}
