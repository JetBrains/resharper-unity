using MyNamespace;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class UIController : MonoBehaviour
    {
        public Button StartGameButton;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            StartGameButton = root.Q<Button>("{caret}");//start-game-button
        }
    }
}