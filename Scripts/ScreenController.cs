using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;

public enum ScreenState
{
    unknown,
    play,
    pause,
    clear
}

public static class ScreenName
{
    public const string GAMECLEAR = "GameClear";
    public const string RETURN_START = "Start画面に戻る";
    public const string REPLAY = "もう一度プレイ";
    public const string PAUSE = "Pause画面";
    public const string EXPLOSION = "自爆する";
    public const string TO_SELECT = "セレクト画面へ";
}

public class ScreenController : SingletonMono<ScreenController> {

    [SerializeField] GameObject m_screenObj;
//    [SerializeField] GameObject m_textObject;
//    [SerializeField] GameObject m_buttonLeftObj;
//    [SerializeField] GameObject m_buttonRightObj;
	[SerializeField] Text m_screenTitle;
	[SerializeField] Text m_buttonTextLeft;
	[SerializeField] Text m_buttonTextRight;
	[SerializeField] Button m_buttonLeft;
	[SerializeField] Button m_buttonRight;

    ScreenSelections screenSelections;

	PlayerController playerController;
	GameController gameController;

    public ScreenState m_screenState = ScreenState.unknown;

    void Start() {
//		screenSelections = m_screenObj.GetComponent<ScreenSelections>();
//		playerController = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
//		gameController = GameObject.Find("GameController").GetComponent<GameController>();
    }

	// 初期化メソッド
	public void Init() {
		screenSelections = m_screenObj.GetComponent<ScreenSelections>();
		playerController = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
		gameController = GameObject.Find("GameController").GetComponent<GameController>();
		ScreenClear ();
	}

    public void ScreenInfo(ScreenState state)
    {
//        if (state == ScreenState.pause)
//        {
//			m_screenTitle.text = ScreenName.PAUSE;
//            addButtonComponent(ScreenName.EXPLOSION, ScreenName.TO_SELECT, state);
//        }
//        if (state == ScreenState.clear)
//        {
//			m_screenTitle.text = ScreenName.GAMECLEAR;
//            addButtonComponent(ScreenName.RETURN_START, ScreenName.REPLAY, state);
//        }
    }

	public void ScreenSet(ScreenState state) {
		m_screenObj.gameObject.SetActive (!m_screenObj.gameObject.activeSelf);
		if (this.gameObject.activeSelf == false) {
			return;
		}
		RemoveEvent ();
		switch(state) {
		case ScreenState.pause:
			m_screenTitle.text = ScreenName.PAUSE;
			m_buttonTextLeft.text = ScreenName.EXPLOSION;
			m_buttonTextRight.text = ScreenName.TO_SELECT;
			PauseSelection ();
			break;
		case ScreenState.clear:
			m_screenTitle.text = ScreenName.GAMECLEAR;
			m_buttonTextLeft.text = ScreenName.RETURN_START;
			m_buttonTextRight.text = ScreenName.REPLAY;
			GameClearSelect();
			break;
		default:
			break;
		}
	}

	void RemoveEvent() {
		m_buttonLeft.onClick.RemoveAllListeners ();
		m_buttonRight.onClick.RemoveAllListeners ();
		m_buttonTextLeft.text = "";
		m_buttonTextRight.text = "";
	}

//    void addButtonComponent( string buttonTextLeft , string buttonTextRight, ScreenState state)
//    {
//        m_buttonTextLeft.text = buttonTextLeft;
//        m_buttonTextRight.text = buttonTextRight;
//        if (state == ScreenState.pause) {
//        	m_buttonLeft.onClick.AddListener(screenSelections._pauseSelection);
//        	m_buttonRight.onClick.AddListener(screenSelections._pauseSelection);
//        }
//        if (state == ScreenState.clear) {
//        	m_buttonLeft.onClick.AddListener(screenSelections._gameClearSelect);
//        	m_buttonRight.onClick.AddListener(screenSelections._gameClearSelect);
//        }
//    }

	void PauseSelection()
	{
		// 自爆
		m_buttonLeft.Select();
		m_buttonLeft.OnClickAsObservable()
			.FirstOrDefault()
			.Subscribe(_ => {
				Time.timeScale = 1;
				EffectManager.Instance.EffectIgnition(playerController.transform.position, (int)EffectType.bomb);
				playerController.gameObject.SetActive(false);
				m_screenObj.gameObject.SetActive(false);
				Debug.Log("自爆");
			})
			.AddTo(this.gameObject);
		// セレクトシーンへ
		m_buttonRight.OnClickAsObservable()
			.FirstOrDefault()
			.Subscribe(_ => {
				Time.timeScale = 1;
				SceneController.Instance.LoadScene(SceneName.STAGESELECT, true);
				Debug.Log("シーンロード");
			})
			.AddTo(this.gameObject);
	}

	void GameClearSelect()
	{
		//  セレクトシーンへ
		m_buttonLeft.Select();
		m_buttonLeft.OnClickAsObservable()
			.FirstOrDefault()
			.Subscribe(_ => {
				SceneController.Instance.LoadScene(SceneName.STAGESELECT);
				Debug.Log("ステージセレクトへ");
			})
			.AddTo(this.gameObject);
		// 再スタート
		m_buttonRight.OnClickAsObservable()
			.FirstOrDefault()
			.Subscribe(_ => {
				Time.timeScale = 1;
				SceneController.Instance.ReloadScene();
				Debug.Log("リスタート");
			})
			.AddTo(this.gameObject);	
	}

	public void ScreenClear() {
		m_screenObj.SetActive (false);
		RemoveEvent ();
	}
}
