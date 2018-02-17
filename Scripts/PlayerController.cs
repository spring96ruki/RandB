using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniRx;

[ExecuteInEditMode]
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour {

	[SerializeField, Header("移動速度")] float playerSpeed = 0.5f;
	[SerializeField, Header("空中移動の減衰値"), Range(0.0f, 1.0f)] float airWalk = 0.8f;
	[SerializeField, Header("ジャンプの強さ")] float jumpPower = 15.0f;
	[SerializeField, Header("最大入力受付時間")] float maxInputTime = 0.5f;
	[SerializeField, Header("死に戻り可能時間")] float reflashTime = 1.0f;
	float inputTime = 0.0f;
	[SerializeField, Header("飛ばし方")] ForceMode forceMode;
	//[SerializeField, Header("ジャンプの高さの遷移（テスト用）")] AnimationCurve curve;
	GameController gc;
	Animator animator;
	Rigidbody rb { get { return GetComponent<Rigidbody> (); } }
    Vector3 move;
    Vector3 startPos;
    Quaternion startRota;
    string xAxisName = "Horizontal";
	bool isGround = true;
	bool isJumped = false;
	ReactiveProperty<PlayerState> playerState = new ReactiveProperty<PlayerState> (PlayerState.idle);
    bool IsPushed { get { return (Input.GetKeyDown (KeyCode.Space) == true || Input.GetButtonDown ("Fire1") == true); } }
	bool IsHold { get { return (Input.GetKey (KeyCode.Space) == true || Input.GetButton ("Fire1") == true); } }
	bool IsRelease { get { return (Input.GetKeyUp (KeyCode.Space) == true || Input.GetButtonUp ("Fire1") == true); } }

	AudioSource audioSource {
		get { return GetComponent<AudioSource> ();}
	}

	PlayerState state { get { return playerState.Value; } set { playerState.Value = value; } }
	public bool InBlocks { get; set; }
	public bool IsDeath { get; set; }

	List<GameObject> rightHitObjs = new List<GameObject> ();
	List<GameObject> leftHitObjs = new List<GameObject> ();

	void OnEnable() {
		if (gc == null)
			return;
		ReactiveState ();
	}

	void Start() {
		gc = GameObject.Find ("GameController").GetComponent<GameController> ();
		startPos = transform.position;
		startRota = transform.rotation;
		animator = GetComponent<Animator> ();
		InBlocks = false;
		IsDeath = false;
		ReactiveState ();
		isGround = true;
	}

	void Update() {
		if (!SceneController.Instance.IsPlaying || Time.timeScale < 1)
			return;
		move = Vector3.zero;
		move.x = isGround ? Input.GetAxis (xAxisName) : Input.GetAxis (xAxisName) * airWalk;
		var absX = Mathf.Abs (move.x);
		if (absX > 0) {
			state = isGround ? 
				!InBlocks ? PlayerState.run : PlayerState.inBlock
				: PlayerState.jump;
			if (move.x > 0)
				transform.rotation = Quaternion.Euler (new Vector3 (0, 90, 0));
			else
				transform.rotation = Quaternion.Euler (new Vector3 (0, -90, 0));
		}
		else if(!isGround)
			state = PlayerState.jump;
		else
			state = PlayerState.idle;
		if (IsPushed && isGround) {
			inputTime = 0.0f;
			isJumped = false;
		}
		if (IsHold && inputTime < maxInputTime && isGround) {
			inputTime += Time.deltaTime;
		}
		if ((IsRelease || inputTime >= maxInputTime) && isGround && !isJumped)
			PlayerJump ();
		animator.SetInteger ("Speed", absX > 0 ? 2 : 0);
	}

	// Update is called once per frame
	void FixedUpdate () {
		if (!SceneController.Instance.IsPlaying)
			return;
		rb.MovePosition(transform.position + move * playerSpeed * Time.fixedDeltaTime);
    }

	void LateUpdate() {
//		Debug.Log ("right:" + rightHitObjs.Count + ",left:" + leftHitObjs.Count);
		if(rightHitObjs.Count > 0 && leftHitObjs.Count > 0) {
			EffectManager.Instance.EffectIgnition (this.transform.position, (int)EffectType.bomb);
			gameObject.SetActive (false);
			rightHitObjs.Clear ();
			leftHitObjs.Clear ();
//			Debug.Log ("SideDeath!!!");
		}
	}

	void OnCollisionEnter(Collision col) {
		for (int i = 0; i < col.contacts.Length; i++) {
			ContactPoint contactPoint = col.contacts[i];
			if (isGround && contactPoint.otherCollider.gameObject.tag == "MoveObj") /*移動床と地面に上下に挟まれた時の判定*/{
//				Debug.Log ("name:" + contactPoint.otherCollider.gameObject.name);
				var mvObjPos = contactPoint.otherCollider.gameObject.transform.position;
				var pHeadPos = this.transform.position.y + 2;
				var isDropping = contactPoint.otherCollider.gameObject.GetComponent<MoveObj> ().isDrpping;
				if (mvObjPos.y > pHeadPos && isDropping) {
					EffectManager.Instance.EffectIgnition (this.transform.position, (int)EffectType.bomb);
					gameObject.SetActive (false);
//					Debug.Log ("MoveDeath!!!");
				}
			} else if (isGround && contactPoint.otherCollider.gameObject.tag == "FallObj") /*落ちるオブジェクトの判定*/{
				var mvObjPos = contactPoint.otherCollider.gameObject.transform.position;
				var pHeadPos = this.transform.position.y + 2;
				var objvelocityY = col.gameObject.GetComponent<Rigidbody> ().velocity.y;
				if (mvObjPos.y > pHeadPos && objvelocityY > 0.1f) {
					EffectManager.Instance.EffectIgnition (this.transform.position, (int)EffectType.bomb);
					gameObject.SetActive (false);
//					Debug.Log ("FallDeath!!!");
				}
			} else if (!isGround) {
				var groundPos = contactPoint.otherCollider.gameObject.transform.position.y - (contactPoint.otherCollider.gameObject.transform.localScale.y / 2);
				var pLowPos = this.transform.position.y;
				if (groundPos < pLowPos) {
					isGround = true;
					state = PlayerState.idle;
					rb.velocity = Vector3.zero;
				}
			}
			/*-----------------------------------------------------*/
			if (contactPoint.point.y > this.transform.position.y + 0.1f && contactPoint.point.y < this.transform.position.y + 1.8f) {
				if(contactPoint.point.x > (this.transform.position.x + 0.27f) && !IsExistList(rightHitObjs, contactPoint.otherCollider.gameObject) && !IsExistList(leftHitObjs, contactPoint.otherCollider.gameObject)) /*右側*/{
					rightHitObjs.Add(contactPoint.otherCollider.gameObject);
				}
				if(contactPoint.point.x < (this.transform.position.x - 0.27f) && !IsExistList(leftHitObjs, contactPoint.otherCollider.gameObject) && !IsExistList(rightHitObjs, contactPoint.otherCollider.gameObject)) /*左側*/{
					leftHitObjs.Add(contactPoint.otherCollider.gameObject);
				}
			}
		}
	}

	void OnCollisionExit(Collision col) {
		for (int i = 0; i < rightHitObjs.Count; i++) {
			GameObject rightHitObj = rightHitObjs [i];
			if (col.gameObject == rightHitObj) {
				rightHitObjs = rightHitObjs.Where (obj => obj != rightHitObj).ToList ();
			}
		}
		for (int i = 0; i < leftHitObjs.Count; i++) {
			GameObject leftHitObj = leftHitObjs [i];
			if (col.gameObject == leftHitObj) {
				leftHitObjs = rightHitObjs.Where (obj => obj != leftHitObj).ToList ();
			}
		}
	}

	bool IsExistList(List<GameObject> targetList, GameObject targetObj) {
		for(int i = 0; i < targetList.Count; i++) {
			GameObject obj = targetList [i];
			if (obj == targetObj) {
				return true;
			}
		}
		return false;
	}

	void ReactiveState () {
		playerState.Subscribe (ps => {
			switch (ps) {
			case PlayerState.jump:
				audioSource.clip = SEManager.Instance.GetPlayerClip ((int)ps);
				audioSource.loop = false;
				audioSource.Play ();
				break;
			case PlayerState.run:
			case PlayerState.inBlock:
				audioSource.clip = SEManager.Instance.GetPlayerClip ((int)ps);
				audioSource.loop = true;
				audioSource.Play ();
				break;
			case PlayerState.idle:
				audioSource.Stop();
				break;
			default:
				break;
			}
		}).AddTo (this.gameObject);
	}

	void PlayerJump() {
		var jumpLerp = Mathf.InverseLerp (0.0f, maxInputTime, inputTime);
		state = PlayerState.jump;
		rb.AddForce (Vector3.up * jumpPower * jumpLerp, forceMode);
		isGround = false;
		isJumped = true;
		inputTime = 0.0f;
	}

	public void Eliminate() {
		if (IsDeath)
			return;
		transform.position = startPos;
		transform.rotation = startRota;
		this.gameObject.transform.parent = null;
		InBlocks = false;
		state = PlayerState.idle;
		StartCoroutine (DeathReflash ());
	}

	IEnumerator DeathReflash() {
		yield return new WaitForSecondsRealtime (reflashTime);
		IsDeath = false;
	}

	public void AnimatorReset () {
		animator.SetInteger ("Speed", 0);
		state = PlayerState.idle;
	}
}