using System;
using DataClasses.Enums;
using UnityEngine;
using UnityEngine.UI;

namespace DataViews
{
    /// <summary>
    /// Modal panel that asks the player to pick one of the four normal suits
    /// after playing a wild card.
    ///
    /// Scene setup:
    ///   This MonoBehaviour lives on a Screen Space Overlay Canvas child.
    ///   'panel'         - root GameObject that is toggled on/off
    ///   'redButton'     - Button for Suit.Red
    ///   'yellowButton'  - Button for Suit.Yellow
    ///   'blueButton'    - Button for Suit.Blue
    ///   'greenButton'   - Button for Suit.Green
    ///
    /// Wire each button's OnClick in the Inspector to the corresponding
    /// ChooseRed / ChooseYellow / ChooseBlue / ChooseGreen method, or leave
    /// them unwired and let Bind() add listeners at runtime (both work; runtime
    /// listeners are removed on Hide() to avoid stale callbacks across calls).
    /// </summary>
    public class SuitPickerView : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Button redButton;
        [SerializeField] private Button yellowButton;
        [SerializeField] private Button blueButton;
        [SerializeField] private Button greenButton;

        private Action<Suit> _onChosen;

        private void Awake()
        {
            Hide();
        }

        /// <summary>
        /// Shows the panel and registers the callback that will fire when the
        /// player clicks a suit button. The callback is cleared on Hide() so
        /// there is no risk of it firing twice across separate wild plays.
        /// </summary>
        public void Show(Action<Suit> onChosen)
        {
            _onChosen = onChosen;

            redButton.onClick.AddListener(ChooseRed);
            yellowButton.onClick.AddListener(ChooseYellow);
            blueButton.onClick.AddListener(ChooseBlue);
            greenButton.onClick.AddListener(ChooseGreen);

            panel.SetActive(true);
        }

        public void Hide()
        {
            panel.SetActive(false);

            redButton.onClick.RemoveListener(ChooseRed);
            yellowButton.onClick.RemoveListener(ChooseYellow);
            blueButton.onClick.RemoveListener(ChooseBlue);
            greenButton.onClick.RemoveListener(ChooseGreen);

            _onChosen = null;
        }

        // These four methods are the button targets; they can also be wired
        // directly in the Inspector if preferred.

        public void ChooseRed()
        {
            Commit(Suit.Red);
        }

        public void ChooseYellow()
        {
            Commit(Suit.Yellow);
        }

        public void ChooseBlue()
        {
            Commit(Suit.Blue);
        }

        public void ChooseGreen()
        {
            Commit(Suit.Green);
        }

        private void Commit(Suit suit)
        {
            // Capture and null-out before invoking so Hide() is safe to call inside
            // the callback if the caller wants to chain behaviour.
            var callback = _onChosen;
            Hide();
            callback?.Invoke(suit);
        }
    }
}
