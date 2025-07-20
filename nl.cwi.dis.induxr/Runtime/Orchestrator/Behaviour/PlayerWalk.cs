using UnityEngine;

namespace Orchestrator.Behaviour
{
	public class PlayerWalk : MonoBehaviour {
		private static readonly int Jump = Animator.StringToHash("Jump");
		private static readonly int Crouch = Animator.StringToHash("Crouch");
		private static readonly int Speed = Animator.StringToHash("Speed");
		private static readonly int Direction = Animator.StringToHash("Direction");

		private Animator _anim;

		private void Start()
		{
			_anim = GetComponent<Animator>();
		}

		private void Update()
		{
			if (!_anim) return;
			var animState = _anim.GetCurrentAnimatorStateInfo(0);

			// Jump when the SPACE key is pressed
			if (animState.IsName("Base.WalkForward"))
			{
				if (Input.GetButton("Jump")) {
					_anim.SetBool(Jump, true);
				}
			}
			else
			{
				_anim.SetBool(Jump, false);
			}

			// Crouch when the CTRL key is pressed
			if (animState.IsName("Base.Idle"))
			{
				if (Input.GetButtonDown("Fire1")) {
					_anim.SetBool(Crouch, true);
				}
			}

			// Go back to idle when the CTRL key is released
			if (animState.IsName("Base.Crouch"))
			{
				if (Input.GetButtonUp("Fire1")) {
					_anim.SetBool(Crouch, false);
				}
			}

			var h = Input.GetAxis("Horizontal");
			var v = Input.GetAxis("Vertical");

			_anim.SetFloat(Speed, v);
			_anim.SetFloat(Direction, h);
			_anim.speed = 2f;
		}
	}
}
