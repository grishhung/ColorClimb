using System;
using System.Collections.Generic;
using DataClasses.BusinessLayer;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DataViews
{
    /// <summary>
    /// Modal panel that asks the player to pick another player to swap hands with
    /// after playing a 7.
    ///
    /// Scene setup:
    ///   'panel'              - root GameObject toggled on/off
    ///   'buttonPrefab'       - Button prefab with a TMP_Text child for the label;
    ///                          one instance is spawned per valid target at Show() time
    ///                          and destroyed on Hide()
    ///   'buttonContainer'    - Transform (e.g. a Vertical Layout Group) that
    ///                          receives the spawned buttons
    /// </summary>
    public class PlayerPickerView : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Button buttonPrefab;
        [SerializeField] private Transform buttonContainer;

        private readonly List<Button> _spawnedButtons = new();
        private Action<int> _onChosen;

        private void Awake()
        {
            Hide();
        }

        /// <summary>
        /// Shows the panel with one button per valid swap target.
        /// The source player is excluded; all others are listed.
        /// </summary>
        public void Show(GameState state, int sourcePlayerIndex, Action<int> onChosen)
        {
            _onChosen = onChosen;

            for (var i = 0; i < state.Players.Count; i++)
            {
                // The player who played the 7 cannot swap with themselves.
                if (i == sourcePlayerIndex)
                {
                    continue;
                }

                var targetIndex = i; // Capture for lambda; loop variable would be stale.
                var button = Instantiate(buttonPrefab, buttonContainer);

                // Label the button with the player's position in the turn order.
                // TODO: Replace "Player N" with real player names once those exist.
                var label = button.GetComponentInChildren<TMP_Text>();
                if (label != null)
                {
                    label.text = $"Player {i + 1}";
                }

                button.onClick.AddListener(() => Commit(targetIndex));
                _spawnedButtons.Add(button);
            }

            panel.SetActive(true);
        }

        public void Hide()
        {
            panel.SetActive(false);

            foreach (var button in _spawnedButtons)
            {
                Destroy(button.gameObject);
            }

            _spawnedButtons.Clear();
            _onChosen = null;
        }

        private void Commit(int targetIndex)
        {
            var callback = _onChosen;
            Hide();
            callback?.Invoke(targetIndex);
        }
    }
}
