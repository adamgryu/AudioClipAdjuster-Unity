using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System;
using System.Linq;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace AudioClipAdjuster.Editor {

    public class AudioClipAdjuster : EditorWindow {

        private const string DEFAULT_executablePath = "ffmpeg";
        private const string DEFAULT_argumentString = "-y -i \"{0}\" -filter:a \"volume={2}\" \"{1}\"";
        private const string DEFAULT_advancedArgString = "-y -i \"{0}\" -filter:a \"volume={2}, rubberband=tempo={4}:pitch={3}\" \"{1}\"";
        private const string UXML_GUID = "d246ffddb41c134419f4d0ec756f2e96";

        public static string executablePath = DEFAULT_executablePath;
        public static string argumentString = DEFAULT_argumentString;
        public static string advancedArgString = DEFAULT_advancedArgString;
        public HashSet<string> audioExtensions = new HashSet<string> { ".wav", ".ogg", ".mp3", ".aiff", ".aif" };

        private Slider volumeSlider;
        private Slider pitchSlider;
        private Slider tempoSlider;
        private Label clipsText;
        private Button adjustButton;
        private Button clearButton;
        private Button revertButton;

        private string projectFolder => Application.dataPath.Substring(0, Application.dataPath.Length - 7);

        [MenuItem("Window/Audio/Audio Clip Adjuster")]
        public static void OpenFromAudio() {
            OpenWindow();
        }

        [MenuItem("CONTEXT/AudioImporter/Edit Volume")]
        public static void OpenFromImporter() {
            OpenWindow();
        }

        [MenuItem("CONTEXT/AudioClip/Edit Volume")]
        public static void OpenFromClip() {
            OpenWindow();
        }

        private static void OpenWindow() {
            AudioClipAdjuster wnd = GetWindow<AudioClipAdjuster>();
            wnd.titleContent = new GUIContent("Audio Clip Adjuster");
        }

        private void OnSelectionChange() {
            UpdateClipsText();
        }

        public void CreateGUI() {
            // Each editor window contains a root VisualElement object.
            VisualElement root = rootVisualElement;

            // Import UXML.
            var uxmlPath = AssetDatabase.GUIDToAssetPath(UXML_GUID);
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
            VisualElement labelFromUXML = visualTree.Instantiate();
            root.Add(labelFromUXML);

            adjustButton = root.Query<Button>("Adjust").First();
            adjustButton.RegisterCallback<ClickEvent>(AdjustClip);

            clearButton = root.Query<Button>("Clear").First();
            clearButton.RegisterCallback<ClickEvent>(ClearClipCache);
            revertButton = root.Query<Button>("Revert").First();
            revertButton.RegisterCallback<ClickEvent>(RevertClip);
            root.Query<Button>("DefaultSettings").First().RegisterCallback<ClickEvent>(RestoreDefaultSettings);

            volumeSlider = root.Query<Slider>("VolumeSlider").First();
            pitchSlider = root.Query<Slider>("PitchSlider").First();
            tempoSlider = root.Query<Slider>("TempoSlider").First();
            clipsText = root.Query<Label>("Clips").First();

            SetupEditorPrefField("PathField", () => executablePath, value => executablePath = value);
            SetupEditorPrefField("ArgumentField", () => argumentString, value => argumentString = value);
            SetupEditorPrefField("AdvancedArgumentField", () => advancedArgString, value => advancedArgString = value);

            UpdateClipsText();
        }

        private void UpdateClipsText() {
            int count = GetSelectedClips(false).Count();
            adjustButton.text = count > 1 ? "Adjust Clips" : "Adjust Clip";
            adjustButton.SetEnabled(count > 0);
            revertButton.SetEnabled(count > 0);
            clearButton.SetEnabled(count > 0);
            clipsText.text = count == 0
                ? "No clips selected."
                : GetSelectedClips().Aggregate("", (sum, next) => sum + next.name + "\n").TrimEnd();
        }

        /// <summary>
        /// Adjusts each selected clip.
        /// </summary>
        private void AdjustClip(ClickEvent clickEvent) {
            foreach (Object clip in GetSelectedClips()) {
                AdjustClip(clip);
            }
        }

        /// <summary>
        /// Calls FFMPEG to apply the changes.
        /// </summary>
        private void AdjustClip(Object clip) {
            // Build input and output paths.
            var originPath = GetClipPath(clip);
            if (originPath == null) {
                return;
            }

            // Move the original file to the temp folder.
            var tempPath = GetClipTempPath(originPath);
            if (!File.Exists(tempPath)) {
                File.Move(originPath, tempPath);
            }

            // Set up process info.
            ProcessStartInfo startInfo = new ProcessStartInfo(executablePath);
            startInfo.WindowStyle = ProcessWindowStyle.Normal;
            startInfo.UseShellExecute = true;

            bool notAdvanced = Mathf.Approximately(pitchSlider.value, 1) && Mathf.Approximately(tempoSlider.value, 1);
            string argStr = notAdvanced ? argumentString : advancedArgString;
            startInfo.Arguments = string.Format(argStr, tempPath, originPath, volumeSlider.value, pitchSlider.value, tempoSlider.value);

            // Launch process.
            var command = executablePath + " " + startInfo.Arguments;
            Debug.Log(command);

            var process = new Process();
            process.StartInfo = startInfo;
            process.EnableRaisingEvents = true;
            process.Exited += (sender, e) => OnClipProcessFinish(originPath);
            process.Start();
        }

        /// <summary>
        /// Clears the clip from the Temp folder, removing your ability to undo.
        /// </summary>
        private void ClearClipCache(ClickEvent evt) {
            if (EditorUtility.DisplayDialog("Overwrite the cache for this sound?", "This will erase the original clip and permanently apply your changes.", "OK", "Cancel")) {
                foreach (Object clip in GetSelectedClips()) {
                    string tempPath = GetClipTempPath(GetClipPath(clip));
                    if (tempPath != null && File.Exists(tempPath)) {
                        File.Delete(tempPath);
                    }
                }
            }
        }

        /// <summary>
        /// Restores the original clip.
        /// </summary>
        private void RevertClip(ClickEvent evt) {
            foreach (Object clip in GetSelectedClips()) {
                string originPath = GetClipPath(clip);
                if (originPath == null) {
                    continue;
                }
                string tempPath = GetClipTempPath(originPath);
                if (File.Exists(tempPath) && File.Exists(originPath)) {
                    File.Delete(originPath);
                    File.Move(tempPath, originPath);
                }
            }
            AssetDatabase.Refresh();
            volumeSlider.value = 1;
            pitchSlider.value = 1;
            tempoSlider.value = 1;
        }

        /// <summary>
        /// Resets editor preferences.
        /// </summary>
        private void RestoreDefaultSettings(ClickEvent evt) {
            rootVisualElement.Query<TextField>("PathField").First().value = DEFAULT_executablePath;
            rootVisualElement.Query<TextField>("ArgumentField").First().value = DEFAULT_argumentString;
            rootVisualElement.Query<TextField>("AdvancedArgumentField").First().value = DEFAULT_advancedArgString;
        }

        /// <summary>
        /// Gets the location of a clip in the project folder.
        /// Returns null if the object doesn't represent an audio file.
        /// </summary>
        private string GetClipPath(Object clip) {
            if (clip == null) {
                return null;
            }

            string originPath = projectFolder + "/" + AssetDatabase.GetAssetPath(clip);
            originPath = Path.GetFullPath(originPath); // Correct the slash direction.
            if (!audioExtensions.Contains(Path.GetExtension(originPath))) {
                return null;
            }

            return originPath;
        }

        /// <summary>
        /// Gets the location of the original clip in the Temp folder.
        /// </summary>
        private string GetClipTempPath(string originalPath) {
            if (originalPath == null) {
                return null;
            }

            var tempPath = projectFolder + "/Temp/" + Path.GetFileName(originalPath);
            return Path.GetFullPath(tempPath);
        }

        /// <summary>
        /// Refresh the database once the clip has been processed by FFMPEG.
        /// </summary>
        private void OnClipProcessFinish(string clipPath) {
            EditorApplication.delayCall += () => {
                AssetDatabase.Refresh();
                clipPath = clipPath.Replace(Path.GetFullPath(Application.dataPath), "Assets/");
                if (AssetDatabase.LoadAssetAtPath<Object>(clipPath) is DefaultAsset) {
                    Debug.LogError("The audio clip became corrupt. This is probably because your version of FFMPEG is out of date and doesn't include the \"rubberband\" filter. Without this filter, you can only adjust the volume. Press \"Restore Cached\" to restore the original clip.");
                }
            };
        }

        /// <summary>
        /// Returns valid clip objects, including corrupted clips.
        /// </summary>
        private static IEnumerable<Object> GetSelectedClips(bool warnWhenEmpty = true) {
            var clips = Selection.objects.Where(obj => obj is AudioClip || obj is DefaultAsset);
            if (!clips.Any() && warnWhenEmpty) {
                Debug.Log("No clip selected.");
            }
            return clips;
        }

        /// <summary>
        /// Sets up a text field to read and write editor preferences.
        /// </summary>
        private void SetupEditorPrefField(string fieldName, Func<string> get, Action<string> set) {
            var pathField = rootVisualElement.Query<TextField>(fieldName).First();
            string key = "AudioClipAdjuster_" + fieldName;
            if (EditorPrefs.HasKey(key)) {
                set(EditorPrefs.GetString(key));
            }
            pathField.SetValueWithoutNotify(get());
            pathField.RegisterValueChangedCallback(evt => {
                set(evt.newValue);
                EditorPrefs.SetString(key, get());
            });
        }
    }
}