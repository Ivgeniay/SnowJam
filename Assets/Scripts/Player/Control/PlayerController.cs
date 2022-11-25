using Assets.Scripts.Enemies.StateMech;
using Assets.Scripts.Game;
using Assets.Scripts.Player.Control;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace Assets.Scripts.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour, IControllable
    {
        [SerializeField] private float playerSpeed = 2.0f;
        [SerializeField] private float jumpHeight = 1.0f;
        [SerializeField] private float gravityValue = -9.81f;
        [SerializeField] private float aiminDelay = 0.3f;
        [SerializeField] private Animator animator;

        private PlayerBehavior playerBehavior;
        private PlayerControlContext playerControlContext;
        private CharacterController _controller;

        private int _aimLayerIndex;
        private Vector3 _playerVelocity;

        private static readonly int IsMoving = Animator.StringToHash("IsMoving");
        private static readonly int IsAiming = Animator.StringToHash("IsAiming");
        private static readonly int IsAttackWithoutAim = Animator.StringToHash("Attack");



        private void Awake(){
            if (playerBehavior is null) playerBehavior = GetComponent<PlayerBehavior>();
            playerControlContext = new(PlayerState.Normal);
        }

        private void Start() {
            _controller = gameObject.GetComponent<CharacterController>();
            _aimLayerIndex = animator.GetLayerIndex("UpperBody");

            playerBehavior.AmmoReplenished += OnAmmoReplenished;
            playerBehavior.AmmoIsOver += OnAmmoIsOver;
            InputManager.Instance.FastAttackPerformed += OnFastAttackPerformed;
            InputManager.Instance.JumpPerformed += OnJumpPerformed;
            InputManager.Instance.AimStarted += OnAimStarted;
            InputManager.Instance.AimCanceled += OnAimCanceled;
            InputManager.Instance.AimPerformed += OnAimPerformed;
            InputManager.Instance.AssistanControllStarted += OnAssistanControllStarted;
            InputManager.Instance.AssistanControllCanceled += OnAssistanControllCanceled;
        }

        public PlayerControlContext GetContext() => playerControlContext;
        public void Move() {
            if (_controller == null) return;
            if (_controller.isGrounded && _playerVelocity.y < 0)
                _playerVelocity.y = 0f;


            Debug.Log(InputManager.Instance.mouseMove());

            PlayerMove();
            //animator.SetBool(IsAttackWithoutAim, false);

            _playerVelocity.y += gravityValue * Time.deltaTime;
            _controller.Move(_playerVelocity * Time.deltaTime);
        }

        private void PlayerMove()
        {
            var movement = InputManager.Instance.GetPlayerMovement();
            var move = new Vector3(movement.x, 0, movement.y);
            move = Camera.main!.transform.forward * move.z + Camera.main!.transform.right * move.x;
            move.y = 0;
            _controller.Move(move * (Time.deltaTime * playerSpeed));

            if (move != Vector3.zero)
            {
                animator.SetBool(IsMoving, true);
                transform.forward = Vector3.Lerp(transform.forward, move, Time.deltaTime * 10);
            }
            else
                animator.SetBool(IsMoving, false);
        }

        private void OnJumpPerformed() {
            if (_controller.isGrounded is false) return;
            _playerVelocity.y += Mathf.Sqrt(jumpHeight * -3.0f * gravityValue);
        }
        private void OnAimStarted()
        {
            switch (playerControlContext.GetPlayerState())
            {
                case PlayerState.AssistantControl:
                    AssistantControl(typeof(AssistantDisposer));
                    break;
                default: return;
            }

        }
        private void OnAimPerformed()
        {
            if (playerBehavior.isAmmoEmpty(playerBehavior.GetCurrentWeapon()) is true) return;

            playerControlContext.SetPlayerState(PlayerState.Aim);
            animator.SetBool(IsAiming, true);
            animator.SetLayerWeight(_aimLayerIndex, 1);
        }
        private void OnAimCanceled()
        {
            animator.SetTrigger("Attack");
            playerControlContext.SetPlayerState(PlayerState.Normal);
            animator.SetBool(IsAiming, false);
            if (animator.GetCurrentAnimatorStateInfo(_aimLayerIndex).IsName("Idle"))
                animator.SetLayerWeight(_aimLayerIndex, 0);
        }
        private void OnAssistanControllStarted()
        {
            playerControlContext.SetPlayerState(PlayerState.AssistantControl);
            animator.SetBool(IsMoving, false);

            AssistantControl(typeof(AssistantDisposer));
        }
        private void OnAssistanControllCanceled()
        {
            playerControlContext.SetPlayerState(PlayerState.Normal);
        }

        private void AssistantControl(Type assistantType)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Physics.Raycast(ray, out RaycastHit hitinfo);
            if (Input.GetMouseButtonDown(0))
                Game.Game.Manager.MoveAssistant(hitinfo.point, assistantType);
        }
        private void OnFastAttackPerformed()
        {
            if (animator.GetBool("FastAttack") == true) animator.ResetTrigger("FastAttack");
            if (playerControlContext.GetPlayerState() == PlayerState.Normal &&
                animator.GetBool("CanAttack") == true)
            {
                animator.SetTrigger("FastAttack");
            }
        }
        private void OnAmmoReplenished() => animator.SetBool("CanAttack", true);
        private void OnAmmoIsOver()
        {
            animator.SetBool("CanAttack", false);
            animator.ResetTrigger("FastAttack");
        }
        private void OnDisable() {
            playerBehavior.AmmoReplenished -= OnAmmoReplenished;
            playerBehavior.AmmoIsOver -= OnAmmoIsOver;
            InputManager.Instance.FastAttackPerformed -= OnFastAttackPerformed;
            InputManager.Instance.JumpPerformed -= OnJumpPerformed;
            InputManager.Instance.AimStarted -= OnAimStarted;
            InputManager.Instance.AimCanceled -= OnAimCanceled;
            InputManager.Instance.AimPerformed -= OnAimPerformed;
            InputManager.Instance.AssistanControllStarted -= OnAssistanControllStarted;
            InputManager.Instance.AssistanControllCanceled -= OnAssistanControllCanceled;

        }
        private void OnEnable()
        {
            if (InputManager.Instance is not null) {
                InputManager.Instance.FastAttackPerformed += OnFastAttackPerformed;
                InputManager.Instance.JumpPerformed += OnJumpPerformed;
                InputManager.Instance.AimStarted += OnAimStarted;
                InputManager.Instance.AimCanceled += OnAimCanceled;
                InputManager.Instance.AimPerformed += OnAimPerformed;
                InputManager.Instance.AssistanControllStarted += OnAssistanControllStarted;
                InputManager.Instance.AssistanControllCanceled += OnAssistanControllCanceled;
            }
            if (playerBehavior is not null) {
                playerBehavior.AmmoReplenished += OnAmmoReplenished;
                playerBehavior.AmmoIsOver += OnAmmoIsOver;
            }
        }
    }
}