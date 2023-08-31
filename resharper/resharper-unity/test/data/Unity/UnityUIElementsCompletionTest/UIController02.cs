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
            StartGameButton = root.Q<Button>("");//start-game-button

            var t = root.Q("{caret}", "");

            var t2 = root.Query<Button>("show-message-button");

            var t3 = root.Query("", "");

            var custom = root.Query<MyElement>("MyElement");

            var custom2 = root.Q<MyElement2>("MyElement2");

            custom2.click+= () =>
            {
                Debug.Log("custom2.click");
                return new ClickEvent();
            };
            
            StartGameButton.clicked += () =>
            { 
                Debug.Log("StartGameButton.clicked");
            };
        }
    }
}