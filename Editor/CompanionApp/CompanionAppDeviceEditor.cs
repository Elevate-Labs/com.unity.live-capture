using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Unity.LiveCapture.CompanionApp
{
    [CustomEditor(typeof(CompanionAppDevice<>), true)]
    class CompanionAppDeviceEditor<TClient> : LiveCaptureDeviceEditor where TClient : class, ICompanionAppClient
    {
        static class Contents
        {
            public static readonly GUIContent actorLabel = EditorGUIUtility.TrTextContent("Actor", "The actor currently assigned to this device.");
            public static readonly GUIContent channelsLabel = EditorGUIUtility.TrTextContent("Channels", "The channels that will be recorded in the next take.");
            public static readonly GUIContent notAssignedLabel = EditorGUIUtility.TrTextContent("None");
            public static readonly GUIContent clientAssignLabel = EditorGUIUtility.TrTextContent("Client Device", "The remote device to capture recordings from. Only compatible connected devices are shown.");
        }

        static readonly string[] s_ExcludeProperties = { "m_Script" };

        /// <inheritdoc/>
        public override void OnInspectorGUI()
        {
            using (var change = new EditorGUI.ChangeCheckScope())
            {
                base.OnInspectorGUI();

                if (change.changed)
                {
                    var device = target as CompanionAppDevice<TClient>;

                    if (device.GetClient() != null)
                    {
                        device.UpdateClient();
                    }
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnDeviceGUI()
        {
            DoClientGUI();

            serializedObject.Update();

            DrawPropertiesExcluding(serializedObject, s_ExcludeProperties);

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Draws the client selection drop-down.
        /// </summary>
        protected void DoClientGUI()
        {
            // Display a dropdown that enables users to select which client is assigned to a device.
            // The first value in the dropdown allows users to clear the device.
            var device = target as CompanionAppDevice<TClient>;
            var currentClient = device.GetClient();

            var currentOption = currentClient != null ? new GUIContent(currentClient.name) : Contents.notAssignedLabel;

            var rect = EditorGUILayout.GetControlRect();
            rect = EditorGUI.PrefixLabel(rect, Contents.clientAssignLabel);

            if (GUI.Button(rect, currentOption, EditorStyles.popup))
            {
                var clients = GetClients();
                var options = new GUIContent[clients.Length + 1];
                options[0] = Contents.notAssignedLabel;

                var formatter = new UniqueNameFormatter();

                for (var i = 0; i < clients.Length; i++)
                {
                    var client = clients[i];
                    var name = client.name;

                    if (ClientMappingDatabase.TryGetDevice(client, out var d))
                    {
                        name += client == currentClient ? $" (Current)" : $" (In Use)";
                    }

                    options[i + 1] = new GUIContent(formatter.Format(name));
                }

                OptionSelectWindow.SelectOption(rect, new Vector2(300f, 250f), options, (index, value) =>
                {
                    device.SetClient(index > 0 ? clients[index - 1] : null, true);
                });
            }
        }

        /// <summary>
        /// Draws the actor selection field.
        /// </summary>
        /// <param name="actor">The currently assigned actor.</param>
        /// <param name="actorSelected">The action taken when an actor is selected.</param>
        /// <typeparam name="T">The type of the actor field.</typeparam>
        protected void DoActorGUI<T>(T actor, Action<T> actorSelected) where T : UnityEngine.Object
        {
            using (var change = new EditorGUI.ChangeCheckScope())
            {
                var newActor = EditorGUILayout.ObjectField(Contents.actorLabel, actor, typeof(T), true) as T;

                if (change.changed)
                {
                    Undo.RegisterCompleteObjectUndo(target, "Inspector");
                    actorSelected?.Invoke(newActor);
                    EditorUtility.SetDirty(target);
                }
            }
        }

        /// <summary>
        /// Draws the live link channels GUI.
        /// </summary>
        /// <param name="channels">The live link channels property.</param>
        protected void DoLiveLinkChannelsGUI(SerializedProperty channels)
        {
            EditorGUILayout.PropertyField(channels, Contents.channelsLabel);
        }

        static TClient[] GetClients()
        {
            if (ServerManager.instance.TryGetServer<CompanionAppServer>(out var server))
            {
                return server
                    .GetClients()
                    .OfType<TClient>()
                    .ToArray();;
            }
            return new TClient[0];
        }
    }
}
