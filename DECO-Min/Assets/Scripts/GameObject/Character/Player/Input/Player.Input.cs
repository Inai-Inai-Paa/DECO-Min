using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows;

public class PlayerInputData
{
    public Vector2 Move;

    public bool JumpHeld;

    public bool JumpPressed;
    public float JumpPressedTime;

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
        // PlayerInputを有効化
        _playerInput.Enable();

        // 初期化
        playerInputData.Move = Vector2.zero;
        playerInputData.JumpHeld = false;
        playerInputData.JumpPressed = false;
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