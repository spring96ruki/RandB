using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Text;

[System.Serializable]
public static class SceneName
{
	public const string TITLE = "Title";
	public const string SCENE1 = "Scene1";
	public const string SCENE2 = "Scene2";
	public const string SCENE3 = "Scene3";
	public const string STAGESELECT = "StageSelect";
	public const string STAGE01 = "Stage01";
	public const string STAGE02 = "Stage02";
	public const string STAGE03 = "Stage03";
	public const string STAGE04 = "Stage04";
	public const string STAGE05 = "Stage05";
	public const string STAGE06 = "Stage06";
	public const string STAGE07 = "Stage07";
}

public class SceneController : SingletonMono<SceneController>
{
	public GameObject m_loadScreen;
	[SerializeField]
	TextAsset jsonAssets;
	static string m_json = "";

	AsyncOperation m_async;
	StringBuilder m_sceneName;

	ClearTime m_clearTime;
	public ClearTime clearTime { get { return m_clearTime; } }
	TimeInfo m_timeInfo = null;
	public TimeInfo timeInfo { get { return m_timeInfo; } }

	Material fadeMaterial {
		get { return m_loadScreen.GetComponent<Renderer> ().sharedMaterial;}
	}
	Color fadeInColor;
	Color fadeOutColor;
	public string activeSceneName { get { return SceneManager.GetActiveScene ().name; } }

	public bool IsPlaying { get; set; }
	public bool IsPlayScene { get { return activeSceneName.IndexOf ("Stage") >= 0; } }
	public bool NotSelectScene { get { return activeSceneName != "StageSelect"; } }

	public int stageNum{ get; set; }
	public float clearRecord { get; set; }
	public string viewTime { get; set; }

	StageNumber stageNumber = StageNumber.Stage01;

	private void Start ()
	{
		m_sceneName = new StringBuilder ();
		fadeInColor = new Color (fadeMaterial.color.r, fadeMaterial.color.g, fadeMaterial.color.b, 0);
		fadeOutColor = new Color (fadeMaterial.color.r, fadeMaterial.color.g, fadeMaterial.color.b, 1);
		m_json = jsonAssets.text;
		try{
			m_timeInfo = m_json.LoadFromJson<TimeInfo>();
		}catch{
			m_timeInfo = new TimeInfo ();
			m_timeInfo.clearTimes.Clear ();
			m_timeInfo.clearTimes = new List<ClearTime> ();
			for (int i = 0; i < stageNumber.GetLength () - 1; i++) {
				var clearTime = new ClearTime ();
				clearTime.bestTime = 10000.0f;
				clearTime.viewTime = "";
				m_timeInfo.clearTimes.Add (clearTime);
			}
		}
	}

	// Scene.Instanse.LoadSceneTest(SceneName.****)
	public void LoadScene (string sceneName, bool interruption = false)
	{
		m_sceneName.Length = 0;
		m_sceneName.Append (sceneName);
		if(IsPlayScene && NotSelectScene && !interruption)
			BreakRecordCheck ();
		StartCoroutine (LoadStart ());
	}

	public void ReloadScene() {
		m_sceneName.Length = 0;
		m_sceneName.Append (activeSceneName);
		BreakRecordCheck ();
		StartCoroutine (LoadStart ());
	}

	IEnumerator LoadStart ()
	{
		IsPlaying = false;
		m_async = SceneManager.LoadSceneAsync (m_sceneName.ToString ());
		m_async.allowSceneActivation = false;
		BGMManager.Instance.audioSource.Stop ();
		yield return FadeOut ();
		ScreenController.Instance.ScreenClear ();
		m_async.allowSceneActivation = true;
	}

	public IEnumerator FadeOut() {
		fadeMaterial.color = fadeOutColor;
		for(int i = 1; i <= 64; i++) {
			var a = 0.015625f * i;
			fadeMaterial.color = new Color (fadeInColor.r, fadeInColor.g, fadeInColor.b, a);
			yield return null;
		}
	}

	public IEnumerator FadeIn() {
		fadeMaterial.color = fadeInColor;
		for(int i = 64; i >= 1; i--) {
			var a = 0.015625f * i;
			fadeMaterial.color = new Color (fadeInColor.r, fadeInColor.g, fadeInColor.b, a);
			yield return null;
		}
	}

	public void StageInit(){
		IsPlaying = true;
		clearRecord = 0;
		var activeScene = SceneManager.GetActiveScene ();
		stageNum = activeScene.buildIndex - 2;
		viewTime = "";
	}

	void BreakRecordCheck() {
		m_timeInfo = m_json.LoadFromJson<TimeInfo> ();
		if (m_timeInfo.clearTimes [stageNum].bestTime > clearRecord) {
			m_timeInfo.clearTimes [stageNum].bestTime = clearRecord;
			m_timeInfo.clearTimes [stageNum].viewTime = viewTime;
			m_timeInfo.SaveToJson (ref m_json);
		}
	}
}
