using System;
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
        /// 
        /// </summary>
        public TMP_Text selectedUnit;

        /// <summary>
        /// </summary>
        public Button endTurnButton;

        /// <summary>
        /// 
        /// </summary>
        public TMP_Text victorySplashText;

        /// <summary>
        /// </summary>
        public SimController simController;

        /// <summary>
        ///     Called on game object enabled.
        /// </summary>
        private void OnEnable()
        {
            simController.OnTurnStateChanged += HandleSimTurnStateChanged;
            simController.OnSelectedUnitChanged += HandleSelectedUnitChanged;
            endTurnButton.onClick.AddListener(HandleEndTurnButtonClick);

            // Initial state.
            selectedUnit.text = "";
            turnState.text = $"{TurnStateExt.ToString(simController.TurnState)} 1";
            turnState.color = simController.TurnState switch
            {
                TurnState.BlueTurn => Color.blue,
                _ => Color.red
            };

            endTurnButton.interactable = simController.TurnState switch
            {
                TurnState.BlueTurn => true,
                _ => false
            };

            victorySplashText.gameObject.SetActive(false);
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
        /// 
        /// </summary>
        /// <param name="id"></param>
        private void HandleSelectedUnitChanged(uint id)
        {
            selectedUnit.text = id switch
            {
                0 => "",
                _ => $"Selected unit: {id}"
            };
        }

        /// <summary>
        /// </summary>
        /// <param name="simEvent"></param>
        private void HandleSimTurnStateChanged(TurnStateChangeEvent simEvent)
        {
            Color color = simEvent.NewState switch
            {
                TurnState.BlueTurn => Color.blue,
                TurnState.BlueAction => Color.blue,
                TurnState.BlueVictory => Color.blue,
                TurnState.RedTurn => Color.red,
                TurnState.RedAction => Color.red,
                TurnState.RedVictory => Color.red,
                _ => throw new ArgumentOutOfRangeException()
            };

            if (simEvent.NewState is TurnState.BlueVictory or TurnState.RedVictory)
            {
                victorySplashText.gameObject.SetActive(true);
                victorySplashText.text = TurnStateExt.ToString(simEvent.NewState);
                victorySplashText.color = color;

                turnState.gameObject.SetActive(false);
                endTurnButton.gameObject.SetActive(false);
                return;
            }

            turnState.text =
                $"{TurnStateExt.ToString(simEvent.NewState)}{Environment.NewLine}{simEvent.TurnCounter + 1}";
            turnState.color = color;
            endTurnButton.interactable = simEvent.NewState switch
            {
                TurnState.BlueTurn => true,
                _ => false
            };
        }
    }
}