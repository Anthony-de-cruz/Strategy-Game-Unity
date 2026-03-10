using GameLogic;
using GameLogic.Events;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts
{
    /// <summary>
    /// </summary>
    public class HudController : MonoBehaviour
    {
        /// <summary>
        /// </summary>
        public TMP_Text turnState;

        /// <summary>
        /// </summary>
        public Button endTurnButton;

        /// <summary>
        /// </summary>
        public SimController simController;


        /// <summary>
        ///     Called once per frame.
        /// </summary>
        private void Update()
        {
        }

        /// <summary>
        ///     Called on game object enabled.
        /// </summary>
        private void OnEnable()
        {
            simController.OnTurnStateChanged += HandleSimTurnStateChanged;
            endTurnButton.onClick.AddListener(HandleEndTurnButtonClick);
        }

        /// <summary>
        ///     Called on game object disabled.
        /// </summary>
        private void OnDisable()
        {
            simController.OnTurnStateChanged -= HandleSimTurnStateChanged;
            endTurnButton.onClick.RemoveAllListeners();
        }

        /// <summary>
        /// </summary>
        private void HandleEndTurnButtonClick()
        {
            simController.EndTurn();
        }

        /// <summary>
        /// </summary>
        /// <param name="simEvent"></param>
        private void HandleSimTurnStateChanged(TurnStateChangeEvent simEvent)
        {
            turnState.text = $"{TurnStateExt.ToString(simEvent.NewState)} {simEvent.TurnCounter + 1}";
            turnState.color = simEvent.NewState switch
            {
                TurnState.BlueTurn => Color.blue,
                _ => Color.red
            };

            endTurnButton.interactable = simEvent.NewState switch
            {
                TurnState.BlueTurn => true,
                _ => false
            };
        }
    }
}