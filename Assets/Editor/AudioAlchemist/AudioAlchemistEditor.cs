using System;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(AudioAlchemist))]
public class AudioAlchemistEditor : Editor
{
    SerializedProperty destroyOnLoadProperty;
    SerializedProperty soundSubjectsProperty;
    bool[] foldouts;
    bool[][] soundFoldouts; // Array de arrays para controlar el colapsado de cada sonido en cada grupo
    AudioSource previewAudioSource;
    string searchQuery = ""; // Campo de búsqueda
    float globalVolume = 1f; // Volumen global
    float[] groupVolumes; // Volumen por grupo
    string currentClipName = ""; // Nombre del clip actual en reproducción
    string currentGroupName = ""; // Nombre del grupo actual
    Texture2D headerImage; // Imagen del header

    void OnEnable()
    {
        destroyOnLoadProperty = serializedObject.FindProperty("destroyOnLoad");
        soundSubjectsProperty = serializedObject.FindProperty("soundSubjects");
        foldouts = new bool[soundSubjectsProperty.arraySize];
        soundFoldouts = new bool[soundSubjectsProperty.arraySize][]; // Inicializar arreglo para el control de colapsado de sonidos
        groupVolumes = new float[soundSubjectsProperty.arraySize];

        // Inicializar el volumen de cada grupo a 0.5 (volumen medio)
        for (int i = 0; i < groupVolumes.Length; i++)
        {
            groupVolumes[i] = 0.5f;
        }

        // Cargar la imagen del header
        headerImage = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/AudioAlchemist/sprites/header.png");

        // Crear el AudioSource de preview si no existe
        if (previewAudioSource == null)
        {
            previewAudioSource = EditorUtility.CreateGameObjectWithHideFlags("AudioSource", HideFlags.HideAndDontSave, typeof(AudioSource)).GetComponent<AudioSource>();
        }

        // Registrar evento para detener reproducción si el usuario cambia de GameObject
        Selection.selectionChanged += OnSelectionChanged;
    }

    void OnDisable()
    {
        // Detener la reproducción al deshabilitar el inspector
        StopPreviewClip();
        Selection.selectionChanged -= OnSelectionChanged;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        var audioAlchemist = (AudioAlchemist)target;

        // Dibujar el header en la parte superior
        if (headerImage != null)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
        
            GUILayout.Label(headerImage, GUILayout.MaxHeight(150), GUILayout.ExpandWidth(true)); // Expande el ancho completo
        
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }


        EditorGUILayout.Space();
        // Dibujar el campo destroyOnLoad
        EditorGUILayout.PropertyField(destroyOnLoadProperty, new GUIContent("Destroy On Load"));

        // Campo de búsqueda
        EditorGUILayout.Space();
        searchQuery = EditorGUILayout.TextField("Search Sounds", searchQuery);

        // Slider de volumen global y botón en la misma línea
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Global Volume Control", GUILayout.Width(130));
        globalVolume = EditorGUILayout.Slider(globalVolume, 0f, 1f);
        if (GUILayout.Button("Apply Global Volume", GUILayout.Width(150)))
        {
            ApplyGlobalVolume(audioAlchemist);
        }
        EditorGUILayout.EndHorizontal();

        // Mini reproductor global sin barra de longitud, solo botón Stop y formato de tiempo
        EditorGUILayout.Space();
        GUI.backgroundColor = new Color(1f, 0.9f, 0.6f); // Color amarillo tenue
        EditorGUILayout.BeginVertical("box");

        // Mostrar "Now Playing: <nombre del clip>" y el nombre del grupo en la misma línea
        string nowPlayingText = string.IsNullOrEmpty(currentClipName) ? "No sound currently playing." : $"Now Playing: \"{currentClipName}\" (Group: {currentGroupName})";
        EditorGUILayout.LabelField(nowPlayingText, EditorStyles.boldLabel);

        // Botón de Stop y formato de tiempo sin la barra de longitud
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Stop ■", GUILayout.Width(70)))
        {
            StopPreviewClip();
        }

        // Mostrar el tiempo transcurrido y la duración total en formato 00:00 - 03:00 al lado del botón Stop
        if (previewAudioSource != null && previewAudioSource.clip != null)
        {
            var elapsedTime = FormatTime(previewAudioSource.time);
            var totalTime = FormatTime(previewAudioSource.clip.length);
            GUILayout.Label($"{elapsedTime} - {totalTime}", GUILayout.Width(100));
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
        GUI.backgroundColor = Color.white;

        // Actualizar tamaño de los foldouts y los volúmenes si cambió el array
        if (foldouts.Length != soundSubjectsProperty.arraySize)
        {
            Array.Resize(ref foldouts, soundSubjectsProperty.arraySize);
            Array.Resize(ref soundFoldouts, soundSubjectsProperty.arraySize);
            Array.Resize(ref groupVolumes, soundSubjectsProperty.arraySize);
        }

        // Dibujar cada grupo de sonidos con opciones expandibles
        for (int i = 0; i < soundSubjectsProperty.arraySize; i++)
        {
            var soundSubjectProperty = soundSubjectsProperty.GetArrayElementAtIndex(i);
            var groupName = soundSubjectProperty.FindPropertyRelative("groupName");
            var soundsArray = soundSubjectProperty.FindPropertyRelative("sounds");

            // Inicializar el array de foldouts para cada sonido en el grupo
            if (soundFoldouts[i] == null || soundFoldouts[i].Length != soundsArray.arraySize)
            {
                soundFoldouts[i] = new bool[soundsArray.arraySize];
            }

            // Caja para el grupo de sonidos y colapsable en color azul
            GUI.backgroundColor = new Color(0.6f, 0.8f, 1f); // Color azul
            EditorGUILayout.BeginVertical("box");

            // Sound Group título y botón Remove Sound Group en la misma línea
            EditorGUILayout.BeginHorizontal();
            foldouts[i] = EditorGUILayout.Foldout(foldouts[i], $"Sound Group: {groupName.stringValue}");

            // Botón Remove Sound Group estilo
            var removeGroupButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 14,
                normal = { textColor = Color.red, background = Texture2D.blackTexture },
                hover = { textColor = Color.white, background = Texture2D.grayTexture }
            };

            // Botón Remove Group con una "X" y tooltip
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

                // Control de volumen por grupo en una línea
                EditorGUILayout.BeginHorizontal();
                groupVolumes[i] = EditorGUILayout.Slider("Group Volume", groupVolumes[i], 0f, 1f);
                if (GUILayout.Button("Apply Group Volume", GUILayout.Width(150)))
                {
                    ApplyGroupVolume(audioAlchemist, i, groupVolumes[i]);
                }
                EditorGUILayout.EndHorizontal();

                // Dibujar cada sonido dentro del grupo que coincide con el criterio de búsqueda
                for (int j = 0; j < soundsArray.arraySize; j++)
                {
                    var soundProperty = soundsArray.GetArrayElementAtIndex(j);
                    var clipName = soundProperty.FindPropertyRelative("clipName");

                    // Verificar si el sonido coincide con el criterio de búsqueda
                    if (!string.IsNullOrEmpty(searchQuery) && !clipName.stringValue.ToLower().Contains(searchQuery.ToLower()))
                    {
                        continue; // Saltar sonidos que no coinciden
                    }

                    var clip = soundProperty.FindPropertyRelative("clip");
                    var volume = soundProperty.FindPropertyRelative("volume");
                    var pitch = soundProperty.FindPropertyRelative("pitch");
                    var loop = soundProperty.FindPropertyRelative("loop");
                    var fadeIn = soundProperty.FindPropertyRelative("fadeIn");
                    var fadeOut = soundProperty.FindPropertyRelative("fadeOut");
                    var fadeDuration = soundProperty.FindPropertyRelative("fadeDuration");

                    // Colapsador para cada sonido con fondo rojo tenue
                    soundFoldouts[i][j] = EditorGUILayout.Foldout(soundFoldouts[i][j], $"Sound: {clipName.stringValue}");
                    GUI.backgroundColor = new Color(1f, 0.6f, 0.6f); // Color rojo tenue

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

                        // Crear estilos para los botones con fondo visible y hover
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

                        // Controles de Play, Stop y Remove con estilo
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

                // Caja para el botón + centrado y en negrita
                GUI.backgroundColor = new Color(0.8f, 0.6f, 1f); // Color morado tenue
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

                    // Inicializar el nuevo sonido con valores predeterminados y expandido
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

        // Configurar el color de fondo para el botón "++" en verde tenue
        GUI.backgroundColor = new Color(0.6f, 1f, 0.6f); // Fondo verde tenue

        // Crear el estilo del botón para aplicar el hover y otros ajustes
        var addGroupButtonStyle = new GUIStyle(GUI.skin.button)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            fontSize = 14,
            fixedHeight = 40 // Asegurar que el botón ocupe todo el alto del renglón
        };

        // Configurar el color de hover en amarillo
        addGroupButtonStyle.normal.textColor = Color.white;
        addGroupButtonStyle.hover.textColor = Color.yellow;

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button(new GUIContent("Add + Groups", "Add Sound Group"), addGroupButtonStyle, GUILayout.ExpandWidth(true)))
        {
            var newGroupIndex = soundSubjectsProperty.arraySize;
            soundSubjectsProperty.arraySize++;
            var newGroup = soundSubjectsProperty.GetArrayElementAtIndex(newGroupIndex);

            // Inicializar el nuevo grupo de sonidos vacío
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

        // Resetear color de fondo a blanco
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

            currentClipName = sound.clip.name; // Actualizar el nombre del clip en reproducción
            currentGroupName = groupName; // Actualizar el nombre del grupo
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
        currentClipName = ""; // Limpiar el nombre del clip actual en reproducción
        currentGroupName = ""; // Limpiar el nombre del grupo actual
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
        // Detener la reproducción al cambiar de GameObject
        StopPreviewClip();
    }

    string FormatTime(float time)
    {
        var minutes = Mathf.FloorToInt(time / 60F);
        var seconds = Mathf.FloorToInt(time % 60F);
        return $"{minutes:00}:{seconds:00}";
    }
}
