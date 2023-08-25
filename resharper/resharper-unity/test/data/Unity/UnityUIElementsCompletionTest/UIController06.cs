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

            var t = root.Q("", "");

            var t2 = root.Query<Button>("");

            var t3 = root.Query("", "");

            var custom = root.Query<MyElement>("");

            var custom2 = root.Q<MyElement2>("{caret}");

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