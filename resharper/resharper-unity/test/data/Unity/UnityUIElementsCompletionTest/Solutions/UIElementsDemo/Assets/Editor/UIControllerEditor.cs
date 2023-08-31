using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class UIControllerEditor : VisualElement
    {
        public Button StartGameButton;

        private void OnEnable()
        {
            var root = this;
            StartGameButton = root.Q<Button>("");

            var t = root.Q("", "");

            var t2 = root.Query<Button>("show-message-button");

            var t3 = root.Query("", "");

            StartGameButton.clicked += () =>
            {

            };

            Animation a;
        }
    }
}