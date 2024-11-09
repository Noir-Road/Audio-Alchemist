using System;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(AudioAlchemist))]
public class AudioAlchemistEditor : Editor
{
    SerializedProperty destroyOnLoadProperty;
    SerializedProperty soundSubjectsProperty;
    bool[] foldouts;
    bool[][] soundFoldouts;
    AudioSource previewAudioSource;
    string searchQuery = ""; 
    float globalVolume = 1f; 
    float[] groupVolumes; 
    string currentClipName = ""; 
    string currentGroupName = "";
    Texture2D headerImage;

    void OnEnable()
    {
        destroyOnLoadProperty = serializedObject.FindProperty("destroyOnLoad");
        soundSubjectsProperty = serializedObject.FindProperty("soundSubjects");
        foldouts = new bool[soundSubjectsProperty.arraySize];
        soundFoldouts = new bool[soundSubjectsProperty.arraySize][];
        groupVolumes = new float[soundSubjectsProperty.arraySize];

        for (int i = 0; i < groupVolumes.Length; i++)
        {
            groupVolumes[i] = 0.5f;
        }

        headerImage = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/AudioAlchemist/sprites/header.png");

        if (previewAudioSource == null)
        {
            previewAudioSource = EditorUtility.CreateGameObjectWithHideFlags("AudioSource", HideFlags.HideAndDontSave, typeof(AudioSource)).GetComponent<AudioSource>();
        }

        Selection.selectionChanged += OnSelectionChanged;
    }

    void OnDisable()
    {
        StopPreviewClip();
        Selection.selectionChanged -= OnSelectionChanged;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        var audioAlchemist = (AudioAlchemist)target;

        if (headerImage != null)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
        
            GUILayout.Label(headerImage, GUILayout.MaxHeight(150), GUILayout.ExpandWidth(true)); // Expande el ancho completo
        
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }


         EditorGUILayout.Space(); // Space
        
        EditorGUILayout.PropertyField(destroyOnLoadProperty, new GUIContent("Destroy On Load"));

        EditorGUILayout.Space();
        searchQuery = EditorGUILayout.TextField("Search Sounds", searchQuery);

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Global Volume Control", GUILayout.Width(130));
        globalVolume = EditorGUILayout.Slider(globalVolume, 0f, 1f);
        if (GUILayout.Button("Apply Global Volume", GUILayout.Width(150)))
        {
            ApplyGlobalVolume(audioAlchemist);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        GUI.backgroundColor = new Color(1f, 0.9f, 0.6f);
        EditorGUILayout.BeginVertical("box");

        string nowPlayingText = string.IsNullOrEmpty(currentClipName) ? "No sound currently playing." : $"Now Playing: \"{currentClipName}\" (Group: {currentGroupName})";
        EditorGUILayout.LabelField(nowPlayingText, EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Stop ■", GUILayout.Width(70)))
        {
            StopPreviewClip();
        }

        if (previewAudioSource != null && previewAudioSource.clip != null)
        {
            var elapsedTime = FormatTime(previewAudioSource.time);
            var totalTime = FormatTime(previewAudioSource.clip.length);
            GUILayout.Label($"{elapsedTime} - {totalTime}", GUILayout.Width(100));
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
        GUI.backgroundColor = Color.white;

        if (foldouts.Length != soundSubjectsProperty.arraySize)
        {
            Array.Resize(ref foldouts, soundSubjectsProperty.arraySize);
            Array.Resize(ref soundFoldouts, soundSubjectsProperty.arraySize);
            Array.Resize(ref groupVolumes, soundSubjectsProperty.arraySize);
        }

        for (int i = 0; i < soundSubjectsProperty.arraySize; i++)
        {
            var soundSubjectProperty = soundSubjectsProperty.GetArrayElementAtIndex(i);
            var groupName = soundSubjectProperty.FindPropertyRelative("groupName");
            var soundsArray = soundSubjectProperty.FindPropertyRelative("sounds");

            if (soundFoldouts[i] == null || soundFoldouts[i].Length != soundsArray.arraySize)
            {
                soundFoldouts[i] = new bool[soundsArray.arraySize];
            }

            GUI.backgroundColor = new Color(0.6f, 0.8f, 1f); // Color azul
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();
            foldouts[i] = EditorGUILayout.Foldout(foldouts[i], $"Sound Group: {groupName.stringValue}");

            var removeGroupButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 14,
                normal = { textColor = Color.red, background = Texture2D.blackTexture },
                hover = { textColor = Color.white, background = Texture2D.grayTexture }
            };

            if (GUILayout.Button(new GUIContent("✖", "Remove Sound Group"), removeGroupButtonStyle, GUILayout.Width(30)))
            {
                soundSubjectsProperty.DeleteArrayElementAtIndex(i);
                Array.Resize(ref foldouts, soundSubjectsProperty.arraySize);
                Array.Resize(ref soundFoldouts, soundSubjectsProperty.arraySize);
                serializedObject.ApplyModifiedProperties();
                return;
            }
            EditorGUILayout.EndHorizontal();

            if (foldouts[i])
            {
                EditorGUILayout.PropertyField(groupName, new GUIContent("Group Name"));

                EditorGUILayout.BeginHorizontal();
                groupVolumes[i] = EditorGUILayout.Slider("Group Volume", groupVolumes[i], 0f, 1f);
                if (GUILayout.Button("Apply Group Volume", GUILayout.Width(150)))
                {
                    ApplyGroupVolume(audioAlchemist, i, groupVolumes[i]);
                }
                EditorGUILayout.EndHorizontal();

                for (int j = 0; j < soundsArray.arraySize; j++)
                {
                    var soundProperty = soundsArray.GetArrayElementAtIndex(j);
                    var clipName = soundProperty.FindPropertyRelative("clipName");

                    if (!string.IsNullOrEmpty(searchQuery) && !clipName.stringValue.ToLower().Contains(searchQuery.ToLower()))
                    {
                        continue; 
                    }

                    var clip = soundProperty.FindPropertyRelative("clip");
                    var volume = soundProperty.FindPropertyRelative("volume");
                    var pitch = soundProperty.FindPropertyRelative("pitch");
                    var loop = soundProperty.FindPropertyRelative("loop");
                    var fadeIn = soundProperty.FindPropertyRelative("fadeIn");
                    var fadeOut = soundProperty.FindPropertyRelative("fadeOut");
                    var fadeDuration = soundProperty.FindPropertyRelative("fadeDuration");

                    soundFoldouts[i][j] = EditorGUILayout.Foldout(soundFoldouts[i][j], $"Sound: {clipName.stringValue}");
                    GUI.backgroundColor = new Color(1f, 0.6f, 0.6f); 

                    if (soundFoldouts[i][j])
                    {
                        EditorGUILayout.BeginVertical("box");
                        EditorGUILayout.PropertyField(clipName, new GUIContent("Clip Name"));
                        EditorGUILayout.PropertyField(clip, new GUIContent("Audio Clip"));
                        EditorGUILayout.Slider(volume, 0f, 1f, "Volume");
                        EditorGUILayout.Slider(pitch, 0.1f, 2f, "Pitch");
                        EditorGUILayout.PropertyField(loop, new GUIContent("Loop"));
                        EditorGUILayout.PropertyField(fadeIn, new GUIContent("Fade In"));
                        EditorGUILayout.PropertyField(fadeOut, new GUIContent("Fade Out"));
                        EditorGUILayout.PropertyField(fadeDuration, new GUIContent("Fade Duration"));

                        var playButtonStyle = new GUIStyle(GUI.skin.button)
                        {
                            fontStyle = FontStyle.Bold,
                            fontSize = 14,
                            normal = { textColor = Color.white, background = Texture2D.blackTexture },
                            hover = { textColor = Color.green, background = Texture2D.blackTexture }
                        };
                        var stopButtonStyle = new GUIStyle(GUI.skin.button)
                        {
                            fontStyle = FontStyle.Bold,
                            fontSize = 14,
                            normal = { textColor = Color.white, background = Texture2D.blackTexture },
                            hover = { textColor = new Color(1f, 0.5f, 0f), background = Texture2D.blackTexture }
                        };
                        var removeButtonStyle = new GUIStyle(GUI.skin.button)
                        {
                            fontStyle = FontStyle.Bold,
                            fontSize = 14,
                            normal = { textColor = Color.white, background = Texture2D.blackTexture },
                            hover = { textColor = Color.red, background = Texture2D.blackTexture }
                        };

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        
                        if (GUILayout.Button(new GUIContent("▶", "Play"), playButtonStyle, GUILayout.Width(50)))
                        {
                            PlayClipInEditor(audioAlchemist.soundSubjects[i].sounds[j], groupName.stringValue);
                        }

                        if (GUILayout.Button(new GUIContent("■", "Stop"), stopButtonStyle, GUILayout.Width(50)))
                        {
                            StopPreviewClip();
                        }

                        if (GUILayout.Button(new GUIContent("✖", "Remove Sound"), removeButtonStyle, GUILayout.Width(50)))
                        {
                            soundsArray.DeleteArrayElementAtIndex(j);
                            Array.Resize(ref soundFoldouts[i], soundsArray.arraySize);
                            serializedObject.ApplyModifiedProperties();
                            return;
                        }

                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndVertical();
                        EditorGUILayout.Space();
                    }
                    GUI.backgroundColor = Color.white; // Reset color
                }

                GUI.backgroundColor = new Color(0.8f, 0.6f, 1f);
                EditorGUILayout.BeginVertical("box");

                var addButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontStyle = FontStyle.Bold,
                    fontSize = 16,
                    normal = { textColor = Color.white, background = Texture2D.blackTexture },
                    hover = { textColor = Color.green, background = Texture2D.blackTexture }
                };

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                
                if (GUILayout.Button(new GUIContent("+", "Add Sound to current group"), addButtonStyle, GUILayout.Width(50)))
                {
                    soundsArray.arraySize++;
                    var newSound = soundsArray.GetArrayElementAtIndex(soundsArray.arraySize - 1);

                    newSound.FindPropertyRelative("clipName").stringValue = string.Empty;
                    newSound.FindPropertyRelative("clip").objectReferenceValue = null;
                    newSound.FindPropertyRelative("volume").floatValue = 0.5f;
                    newSound.FindPropertyRelative("pitch").floatValue = 1f;
                    newSound.FindPropertyRelative("loop").boolValue = false;
                    newSound.FindPropertyRelative("fadeIn").boolValue = false;
                    newSound.FindPropertyRelative("fadeOut").boolValue = false;
                    newSound.FindPropertyRelative("fadeDuration").floatValue = 1f;

                    bool[] newSoundFoldouts = new bool[soundsArray.arraySize];
                    Array.Copy(soundFoldouts[i], newSoundFoldouts, soundsArray.arraySize - 1);
                    newSoundFoldouts[soundsArray.arraySize - 1] = true;
                    soundFoldouts[i] = newSoundFoldouts;
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                GUI.backgroundColor = Color.white; // Reset color
            }

            EditorGUILayout.EndVertical();
            GUI.backgroundColor = Color.white;
            EditorGUILayout.Space();
        }

        GUI.backgroundColor = new Color(0.6f, 1f, 0.6f); 
        
        var addGroupButtonStyle = new GUIStyle(GUI.skin.button)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            fontSize = 14,
            fixedHeight = 40 
        };

        addGroupButtonStyle.normal.textColor = Color.white;
        addGroupButtonStyle.hover.textColor = Color.yellow;

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button(new GUIContent("Add + Groups", "Add Sound Group"), addGroupButtonStyle, GUILayout.ExpandWidth(true)))
        {
            var newGroupIndex = soundSubjectsProperty.arraySize;
            soundSubjectsProperty.arraySize++;
            var newGroup = soundSubjectsProperty.GetArrayElementAtIndex(newGroupIndex);

            newGroup.FindPropertyRelative("groupName").stringValue = "";
            newGroup.FindPropertyRelative("sounds").arraySize = 0;

            Array.Resize(ref foldouts, soundSubjectsProperty.arraySize);
            Array.Resize(ref soundFoldouts, soundSubjectsProperty.arraySize);
            Array.Resize(ref groupVolumes, soundSubjectsProperty.arraySize);
            groupVolumes[newGroupIndex] = 0.5f;
            foldouts[newGroupIndex] = true;
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUI.backgroundColor = Color.white;

        serializedObject.ApplyModifiedProperties();
    }
    
    void PlayClipInEditor(Sounds sound, string groupName)
    {
        if (sound.clip != null)
        {
            if (previewAudioSource == null)
            {
                previewAudioSource = EditorUtility.CreateGameObjectWithHideFlags("AudioSource", HideFlags.HideAndDontSave, typeof(AudioSource)).GetComponent<AudioSource>();
            }

            previewAudioSource.clip = sound.clip;
            previewAudioSource.volume = sound.volume;
            previewAudioSource.pitch = sound.pitch;
            previewAudioSource.loop = sound.loop;
            previewAudioSource.Play();

            currentClipName = sound.clip.name; 
            currentGroupName = groupName; 
        }
    }

    void StopPreviewClip()
    {
        if (previewAudioSource != null)
        {
            previewAudioSource.Stop();
            DestroyImmediate(previewAudioSource.gameObject);
            previewAudioSource = null;
        }
        currentClipName = ""; 
        currentGroupName = ""; 
    }

    void ApplyGlobalVolume(AudioAlchemist audioAlchemist)
    {
        foreach (var soundSubject in audioAlchemist.soundSubjects)
        {
            foreach (var sound in soundSubject.sounds)
            {
                sound.volume = globalVolume;
                if (sound.source != null)
                {
                    sound.source.volume = globalVolume;
                }
            }
        }
    }

    void ApplyGroupVolume(AudioAlchemist audioAlchemist, int groupIndex, float groupVolume)
    {
        foreach (var sound in audioAlchemist.soundSubjects[groupIndex].sounds)
        {
            sound.volume = groupVolume;
            if (sound.source != null)
            {
                sound.source.volume = groupVolume;
            }
        }
    }

    void OnSelectionChanged()
    {
        StopPreviewClip();
    }

    string FormatTime(float time)
    {
        var minutes = Mathf.FloorToInt(time / 60F);
        var seconds = Mathf.FloorToInt(time % 60F);
        return $"{minutes:00}:{seconds:00}";
    }
}
