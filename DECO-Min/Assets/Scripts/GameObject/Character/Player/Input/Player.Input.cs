using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows;

public class PlayerInputData
{
    public Vector2 Move;

    public bool JumpHeld;

    public bool JumpPressed;
    public float JumpPressedTime;

    public bool AttackPressed;

    public bool InteractPressed;

    public bool PeelScrollPressed;


    public void ClearFrameInput()
    {
        JumpPressed = false;
    }

    public bool HasJumpBuffered(float bufferTime)
    {
        return (Time.time - JumpPressedTime <= bufferTime);
    }
}

public partial class Player : Character
{
    private PlayerInput _playerInput { get; set; }

    private void InitializeInput()
    {
        // PlayerInputのインスタンスを生成
        _playerInput = new PlayerInput();
        playerInputData = new PlayerInputData();
        playerInputData.JumpPressedTime = -Mathf.Infinity;
        // PlayerInputのアクションにコールバックを登録
        _playerInput.PlayerControll.XYAxis.started += SetPlayerInputDataAxis;
        _playerInput.PlayerControll.XYAxis.performed += SetPlayerInputDataAxis;
        _playerInput.PlayerControll.XYAxis.canceled += SetPlayerInputDataAxis;
        _playerInput.PlayerControll.Jump.started += _ =>
        {
            playerInputData.JumpHeld = true;

            playerInputData.JumpPressed = true;
            playerInputData.JumpPressedTime = Time.time;
        };
        _playerInput.PlayerControll.Jump.canceled += _ =>
        {
            playerInputData.JumpHeld = false;
        };
        _playerInput.PlayerControll.Attack.started += _ =>
        {
            playerInputData.AttackPressed = true;
        };
        _playerInput.PlayerControll.Attack.canceled += _ =>
        {
            playerInputData.AttackPressed = false;
        };
        _playerInput.PlayerControll.Interact.started += _ =>
        {
            playerInputData.InteractPressed = true;
        };
        _playerInput.PlayerControll.Interact.canceled += _ =>
        {
            playerInputData.InteractPressed = false;
        };
        _playerInput.PlayerControll.Peel.started += _ =>
        {
            playerInputData.PeelScrollPressed = true;
        };
        _playerInput.PlayerControll.Peel.canceled += _ =>
        {
            playerInputData.PeelScrollPressed = false;
        };
        // PlayerInputを有効化
        _playerInput.Enable();

        // 初期化
        playerInputData.Move = Vector2.zero;
        playerInputData.JumpHeld = false;
        playerInputData.JumpPressed = false;
        playerInputData.AttackPressed = false;
        playerInputData.InteractPressed = false;
        playerInputData.PeelScrollPressed = false;
    }

    private void FinalizeInput()
    {
        // PlayerInputを無効化
        _playerInput?.Dispose();
    }

    private void SetPlayerInputDataAxis(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        playerInputData.Move = context.ReadValue<Vector2>();
    }
}