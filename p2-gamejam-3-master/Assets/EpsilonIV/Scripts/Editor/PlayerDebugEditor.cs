using UnityEngine;
using UnityEditor;

namespace Unity.FPS.Gameplay
{
    [CustomEditor(typeof(PlayerDebug))]
    public class PlayerDebugEditor : Editor
    {
        private void OnSceneGUI()
        {
            PlayerDebug playerDebug = (PlayerDebug)target;

            if (!Application.isPlaying || !playerDebug.ShowDebugInfo)
                return;

            Handles.BeginGUI();

            GUILayout.BeginArea(new Rect(10, 10, 150, 60));

            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };

            // Button color
            Color prevBg = GUI.backgroundColor;
            GUI.backgroundColor = playerDebug.SpawnPoint != null ? Color.cyan : Color.gray;

            if (GUILayout.Button("Reset to Spawn", buttonStyle, GUILayout.Height(30)))
            {
                playerDebug.ResetToSpawnPoint();
            }

            GUI.backgroundColor = prevBg;

            // Label
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 9,
                normal = { textColor = playerDebug.SpawnPoint != null ? Color.green : Color.red }
            };

            GUILayout.Label(playerDebug.SpawnPoint != null ? $"Spawn: {playerDebug.SpawnPoint.name}" : "No spawn point!", labelStyle);

            GUILayout.EndArea();
            Handles.EndGUI();
        }
    }
}
