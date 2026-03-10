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
        /// 
        /// </summary>
        public Button triggerVictoryButton;

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
            simController.OnSelectedUnitChanged += HandleSelectedUnitChanged;
            endTurnButton.onClick.AddListener(HandleEndTurnButtonClick);
            triggerVictoryButton.onClick.AddListener(HandleTriggerVictoryButtonClick);

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
        private void HandleTriggerVictoryButtonClick()
        {
            simController.TestVictory();
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
            if (simEvent.NewState == TurnState.BlueVictory)
            {
                victorySplashText.gameObject.SetActive(true);
                victorySplashText.text = TurnStateExt.ToString(simEvent.NewState);
                victorySplashText.color = Color.blue;
                
                turnState.gameObject.SetActive(false);
                endTurnButton.gameObject.SetActive(false);
                triggerVictoryButton.gameObject.SetActive(false);
                return;
            }
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