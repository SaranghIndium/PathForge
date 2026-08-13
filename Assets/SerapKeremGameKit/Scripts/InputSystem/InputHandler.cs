using SerapKeremGameKit._Singletons;
using UnityEngine;
using SerapKeremGameKit._InputSystem.Data;

namespace SerapKeremGameKit._InputSystem
{
    public class InputHandler : MonoSingleton<InputHandler>
    {
        [Header("Input Settings")]
        [SerializeField, Tooltip("Scriptable object for managing player input.")]
        private PlayerInputSO _playerInput;

        private bool _isInputLocked = false; // Indicates whether input is currently locked

        public bool IsInputLocked { get => _isInputLocked; }

        protected override void Awake()
        {
            base.Awake();
            Input.simulateMouseWithTouches = false;

            //if (LoadingPanelController.Instance)
            //{
            //    LockInput();
            //    LoadingPanelController.Instance.OnLoadingFinished += UnlockInput;
            //}
        }

        private void Update()
        {
            if (_isInputLocked) return; // Skip processing if input is locked
            _playerInput.ResetFrame();

            if (Input.touchCount > 0)
            {
                ProcessTouchInput();
            }
            else
            {
                ProcessMouseInput();
            }
        }

        private void ProcessTouchInput()
        {
            Touch touch = Input.GetTouch(0);
            Vector3 touchPosition = touch.position;

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    HandleMouseDown(touchPosition);
                    break;
                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    if (_playerInput.Held)
                    {
                        HandleMouseHeld(touchPosition);
                    }
                    else
                    {
                        HandleMouseDown(touchPosition);
                    }
                    break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    HandleMouseUp(touchPosition);
                    break;
            }
        }

        private void ProcessMouseInput()
        {
            Vector3 mousePosition = Input.mousePosition;

            if (Input.GetMouseButtonDown(0))
            {
                HandleMouseDown(mousePosition);
            }
            else if (Input.GetMouseButton(0))
            {
                HandleMouseHeld(mousePosition);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                HandleMouseUp(mousePosition);
            }
        }

        private void HandleMouseDown(Vector3 position)
        {
            _playerInput.SetMouseDown(position);
        }

        private void HandleMouseHeld(Vector3 position)
        {
            _playerInput.SetMouseHeld(position);
        }

        private void HandleMouseUp(Vector3 position)
        {
            _playerInput.SetMouseUp(position);
        }

        public void UnlockInput()
        {
            _isInputLocked = false;
        }

        public void LockInput()
        {
            _isInputLocked = true;
        }
    }
}