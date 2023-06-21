using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(AudioAlchemist))]
public class SoundManagerEditor : Editor
{
    SerializedProperty soundSubjectsProperty;
    Texture2D headerImage;
    Color arrayColor; // Color for the array background
    Color[] randomColors; // Array to store the fixed random colors for each array element
    float spacing = 10f; // Spacing between array elements

    void OnEnable()
    {
        soundSubjectsProperty = serializedObject.FindProperty("soundSubjects");
        headerImage = EditorGUIUtility.Load("Assets/Editor/Audioalchemist/headerImage.png") as Texture2D;
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
        EditorGUILayout.LabelField("Sound Subjects", EditorStyles.boldLabel);

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
        }

        EditorGUI.indentLevel--;

        EditorGUILayout.Space();

        // Add a button to add new sound subjects
        if (GUILayout.Button("Add Sound Subject"))
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

        serializedObject.ApplyModifiedProperties();
    }
}