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
        public TMP_Text selectedUnitText;

        /// <summary>
        /// </summary>
        public Button endTurnButton;

        public Button resetStateButton;

        public Button nextMapButton;

        /// <summary>
        /// 
        /// </summary>
        public TMP_Text victorySplashText;

        public TMP_Text remainingEnemiesText;

        /// <summary>
        /// </summary>
        public SimController simController;

        private uint _remainingEnemies;

        /// <summary>
        ///     Called on game object enabled.
        /// </summary>
        private void OnEnable()
        {
            simController.OnTurnStateChanged += HandleSimTurnStateChanged;
            simController.OnSelectedUnitChanged += HandleSelectedUnitChanged;
            simController.OnUnitDamaged += HandleUnitAttacked;
            simController.OnStateReset +=  HandleResetState;
            endTurnButton.onClick.AddListener(HandleEndTurnButtonClick);
            resetStateButton.onClick.AddListener(HandleReset);
            nextMapButton.onClick.AddListener(HandleLoadNew);

            Setup();
        }

        /// <summary>
        ///     Called on game object disabled.
        /// </summary>
        private void OnDisable()
        {
            simController.OnTurnStateChanged -= HandleSimTurnStateChanged;
            simController.OnSelectedUnitChanged -= HandleSelectedUnitChanged;
            simController.OnUnitDamaged -= HandleUnitAttacked;
            simController.OnStateReset -=  HandleResetState;
            endTurnButton.onClick.RemoveAllListeners();
            resetStateButton.onClick.RemoveAllListeners();
            nextMapButton.onClick.RemoveAllListeners();
        }

        private void Setup()
        {
            // Initial state.
            selectedUnitText.text = "";
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
            remainingEnemiesText.text = $"{simController.GetUnitsByTeam(UnitTeam.Red).Length} ENEMY UNITS REMAINING";


            turnState.gameObject.SetActive(true);
            endTurnButton.gameObject.SetActive(true);
            remainingEnemiesText.gameObject.SetActive(true);
            selectedUnitText.gameObject.SetActive(true);
            victorySplashText.gameObject.SetActive(false);
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
            if (id == 0)
            {
                selectedUnitText.text = "";
                return;
            }

            if (!simController.TryGetUnitById(id, out UnitView unit)) throw new InvalidConfigException();
            selectedUnitText.text = $"Selected unit: {unit.Type} ({unit.X},{unit.Y})";
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
                remainingEnemiesText.gameObject.SetActive(false);
                selectedUnitText.gameObject.SetActive(false);
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

        private void HandleUnitAttacked(UnitAttackedEvent simEvent)
        {
            if (simEvent.NewStrength != 0) return;
            // Todo - Minus 1 because the event is triggered before the unit is removed properly.
            remainingEnemiesText.text =
                $"{simController.GetUnitsByTeam(UnitTeam.Red).Length - 1} ENEMY UNITS REMAINING";
        }

        private void HandleReset()
        {
            simController.ResetLevel();
        }

        private void HandleLoadNew()
        {
            simController.LoadLevel(1);
        }

        private void HandleResetState()
        {
            Setup();
        }
    }
}