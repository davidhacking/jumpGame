using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Networking;
using System.Text.RegularExpressions;


public class GameCtrl : MonoBehaviour
{

	private static readonly string CONF_FILE = "config";
	private static readonly string WEB_SERVER_URL = "http://192.168.45.130:5000";
	/*
	-68 205
	527 -57
	-80 100
	590 -150
	-80 45
	590 -210
	-80 -10
	590	-270
	*/
	public float Speed;
	public GameObject Platform;
	public GameObject NextPlatform;
	public GameObject playerAsset;
	public GameObject playAgainBtn;
	public GameObject quitGameBtn;
	public GameObject historyBtn;
	public Text scoretext;
	public Rigidbody Rig;
	public AudioSource pressingAudio;
	public AudioSource deadAudio;
	public AudioSource bonusAudio;
	public AudioSource stepAudio;
	public AudioSource gameOverAudio;

	public Text firstScore;
	public Text firstName;
	public Text secondScore;
	public Text secondName;
	public Text thirdScore;
	public Text thirdName;

	public FileUtils fileUtils;

	string playerName;

	Vector3 Point1;
	Vector3 Point2;
	Vector3 PreplayerPosition;
	float Timer;
	float Power;
	float Scale;
	float VSpeed;
	bool IsPressing;
	int Bonus;
	GameStatus gameStatus;
	bool direction;
	GameObject player;
	GameObject playerPos;
	GameObject PrePlatform;
	List<GameObject> Platforms;
	CameraCtrl cameraCtrl;
	int score;
	bool sentScore;
	bool isShowRecord = false;
	int animationScore = 0;
	float playAgainTime = 1.5f;
	float playDeltaTime = 0;

	int extraBonus = 0;

	enum GameStatus {
		INIT,
		CREATE_PLATFORM,
		SHOW_PLATFORM,
		TAPING,
		REBOUND,
		PLAYER_JUMPING,
		GAME_OVER,
		PLAY_AGAIN,
	}

	int ANIMATION_HEIGHT = 9;

	float _waitTime = 2f;
    void OnGUI() {
        if (Input.GetKeyUp(KeyCode.Escape)) {
        	onBackBtnClick();
        }
    }

    public void onBackBtnClick() {
    	stopMusic(pressingAudio);
		if (gameStatus == GameStatus.PLAY_AGAIN) {
			print("game ctrl click back LoadPrevious");
        	MasterSceneManager.Instance.LoadPrevious();
        } else {
        	print("game ctrl click back pauseMain");
            MasterSceneManager.Instance.pauseMain();
        }
	}

	public void setRecondDisable() {
		firstScore.gameObject.SetActive(false);
		firstName.gameObject.SetActive(false);
		secondScore.gameObject.SetActive(false);
		secondName.gameObject.SetActive(false);
		thirdScore.gameObject.SetActive(false);
		thirdName.gameObject.SetActive(false);
	}

	public void setRecord(Text textScore, Text textName, string scoreStr, string nameStr) {
		if (!isShowRecord) {
			return;
		}
		textScore.text = scoreStr;
		textName.text = nameStr;
		textScore.gameObject.SetActive(true);
		textName.gameObject.SetActive(true);
	}

	public void playAgain() {
		playAgainBtn.SetActive(false);
		quitGameBtn.SetActive(false);
		historyBtn.SetActive(false);
		gameStatus = GameStatus.INIT;
	}

	public void quitGame() {
		Application.Quit();
	}

	Vector3 randomDelta() {
		if (direction) {
			return new Vector3(0, ANIMATION_HEIGHT, Random.Range (1.2f, 4));
		}
		return new Vector3(-Random.Range (1.2f, 4), ANIMATION_HEIGHT, 0);
	}

	void printScore() {
		scoretext.text = score.ToString();
	}

	void printScoreAnimation(float t) {
		playDeltaTime += t;
		if (animationScore < score && playDeltaTime > (playAgainTime / score)) {
			playDeltaTime = 0;
			scoretext.text = animationScore.ToString();
			animationScore++;
		}
	}

	void playMusic(AudioSource audio) {
		if (!audio.isPlaying){
			audio.Play();
		}
	}

	void stopMusic(AudioSource audio) {
		if (audio.isPlaying){
			audio.Stop();
		}
	}

	public IEnumerator topNScore(int n) {
    	// validate
        UnityWebRequest www = UnityWebRequest.Get(WEB_SERVER_URL + "/topNScore?n=" + n);
		yield return www.Send();
		if (www.isError) {
            Debug.Log(www.error);
        } else {
        	print(www.downloadHandler.text);
        	if (!string.IsNullOrEmpty(www.downloadHandler.text)) {
        		string[] data = Regex.Split(www.downloadHandler.text, "\n", RegexOptions.IgnoreCase);
        		for (int i = 0; i < data.Length; i++) {
        			print(data[i]);
        			string[] ss = Regex.Split(data[i], ",", RegexOptions.IgnoreCase);
        			if (ss.Length != 2) {
        				break;
        			}
        			if (i == 0) {
        				setRecord(firstScore, firstName, ss[1], ss[0]);
        			} else if (i == 1) {
						setRecord(secondScore, secondName, ss[1], ss[0]);
        			} else if (i == 2) {
						setRecord(thirdScore, thirdName, ss[1], ss[0]);
        			}
        		}
        	}
        }
    }

	public IEnumerator recordScore(int score) {
    	// validate
        UnityWebRequest www = UnityWebRequest.Get(WEB_SERVER_URL + "/winGame?name=" + playerName + "&score=" + score);
        isShowRecord = true;
		yield return www.Send();
		if (www.isError) {
            Debug.Log(www.error);
        } else {
        	print(www.downloadHandler.text);
        	StartCoroutine(topNScore(3));
        }
    }

	// Use this for initialization
	void Start()
	{
		ArrayList lines = fileUtils.readFileToLines(CONF_FILE);
    	if (lines == null) {
    		//Application.LoadLevel("login");
    		MasterSceneManager.Instance.LoadNext("login");
    	} else {
    		playerName = (string) lines[0];
    	}

		playAgainBtn.SetActive(false);
		quitGameBtn.SetActive(false);
		historyBtn.SetActive(false);
		cameraCtrl = GetComponent<CameraCtrl>();
		Platforms = new List<GameObject>();
		scoretext = scoretext.GetComponent<Text>();
		IsPressing = false;
		Bonus = 0;

		initGame();
	}

	void initGame() {
		isShowRecord = false;
		setRecondDisable();
		extraBonus = 0;
		score = 0;
		sentScore = false;
		animationScore = 0;
		playDeltaTime = 0;
		Bonus = 0;
		printScore();
		foreach (GameObject current in Platforms) {
			Destroy(current);
		}
		if (player != null) {
			Destroy(player);
		}
		Vector3 position = new Vector3 (0, 0.52f, 0);
		NextPlatform = Instantiate(Platform, position, Quaternion.Euler (Vector3.zero));
		Platforms.Add(NextPlatform);

		player = Instantiate(playerAsset, new Vector3 (0, 1.25f, 0), Quaternion.Euler (Vector3.zero));
		playerPos = player.transform.Find("position").gameObject;
		
		gameStatus = GameStatus.CREATE_PLATFORM;
	}

	bool nextDirection() {
		return (Random.Range (0, 10) > 5);
	}

	void createPlatform() {
		PrePlatform = NextPlatform;

		direction = nextDirection();
		NextPlatform = Instantiate (Platform, PrePlatform.transform.position + randomDelta(), Quaternion.Euler (Vector3.zero));

		// render color
		if (extraBonus > 0) {
			Material materialColored = new Material(Shader.Find("Diffuse"));
	        materialColored.color = extraBonus == 1 ? Color.green : Color.yellow;
	        NextPlatform.GetComponent<Renderer>().material = materialColored;
	    }

		Platforms.Insert (Platforms.Count, NextPlatform);

		Point1 = NextPlatform.transform.position;
		Point2 = Point1;
		Point2.y = 0.5f;

        //重新设置相机的位置
		cameraCtrl.SetPosition ((PrePlatform.transform.position + Point2) / 2);

		Timer = 0;
		gameStatus = GameStatus.SHOW_PLATFORM;
		Power = 0;
	}

	void playAgainFunc() {
		if (!sentScore) {
			StartCoroutine(recordScore(score));
			sentScore = true;
		}

		float t = Time.deltaTime;
		Timer += t;
		if (Timer < playAgainTime) playMusic(gameOverAudio);
		else stopMusic(gameOverAudio);
		// score animation
		if (Timer < playAgainTime) printScoreAnimation(t);
		else printScore();
		playAgainBtn.SetActive(true);
		quitGameBtn.SetActive(true);
		historyBtn.SetActive(true);
	}

	public void gotoHistory() {
		//Application.LoadLevel("scrollView");
		MasterSceneManager.Instance.LoadNext("scrollView");
    	print("scrollView");
	}

	/*
	https://answers.unity.com/questions/1065971/how-do-you-detect-a-mouse-button-click-on-a-game-o.html
	检测鼠标点击的gameobject避免button click事件也被检测成pressing事件
	*/
	bool isPressing(bool flag) {
		if (Input.touchCount <= 0) {
			if (flag)
				return Input.GetMouseButtonDown(0);
			else
				return !Input.GetMouseButtonUp(0);
		} else {
			if (flag) {
				for (int i = 0; i < Input.touchCount; ++i) {
	            	if (Input.GetTouch(i).phase == TouchPhase.Began)
	                	return true;
	        	}
	        	return false;
	        } else {
	        	for (int i = 0; i < Input.touchCount; ++i) {
	            	if (Input.GetTouch(i).phase == TouchPhase.Ended)
	                	return false;
	        	}
	        	return true;
	        }
		}
	}

	// Update is called once per frame
	void Update () {

		if (MasterSceneManager.Instance.mainPause) {
			return;
		}

		//print("current status: " + gameStatus);
		switch (gameStatus) {
		case GameStatus.INIT:
			initGame();
			cameraCtrl.CameraInit();
			break;

		case GameStatus.CREATE_PLATFORM:
            createPlatform();
			break;
		case GameStatus.SHOW_PLATFORM:
                
			Timer += Time.deltaTime;
			NextPlatform.transform.position = Vector3.Lerp (Point1, Point2, Timer * Speed);

			if (Timer * Speed > 1) {
				gameStatus = GameStatus.TAPING;
				Timer = 0;
			}
			break;
		case GameStatus.TAPING:
			if (isPressing(true)) {
				playMusic(pressingAudio);
				IsPressing = true;
			}
			if (IsPressing) {
				if (isPressing(false)) {
					playMusic(pressingAudio);
					Timer += Time.deltaTime;
					if (Timer < 4) {
						Power = Timer * 3;
						PrePlatform.transform.localScale = new Vector3 (1, 1 - 0.2f * Timer, 1);
						PrePlatform.transform.Translate (0, -0.1f * Time.deltaTime, 0);
						player.transform.Translate (0, -0.2f * Time.deltaTime, 0);
					}
				} else {
					stopMusic(pressingAudio);
					IsPressing = false;
					gameStatus = GameStatus.REBOUND;
					Scale = PrePlatform.transform.localScale.y;
				}
			}
			break;

		case GameStatus.REBOUND:
			Timer += Time.deltaTime;
			PrePlatform.transform.localScale = Vector3.Lerp (new Vector3 (1, Scale, 1), new Vector3 (1, 1, 1), Timer);
			PrePlatform.transform.Translate (0, 0.5f * Time.deltaTime, 0);
			player.transform.Translate (0, 1.0f * Time.deltaTime, 0);

			if (PrePlatform.transform.position.y >= 0.5) {
				gameStatus = GameStatus.PLAYER_JUMPING;
				VSpeed = 0.3f;
				PreplayerPosition = player.transform.position;
			}
			// clear gameobject
			if (Platforms.Count > 5) {
				Platforms.RemoveAt (0);
				Destroy ((GameObject) Platforms[0]);
			}
			break;

		case GameStatus.PLAYER_JUMPING:
			VSpeed -= Time.deltaTime;
			if (direction) {
				player.transform.Translate (new Vector3 ((NextPlatform.transform.position.x - PreplayerPosition.x) / 0.6f * Time.deltaTime, VSpeed / 2, Power / 0.6f * Time.deltaTime));
				playerPos.transform.Rotate (new Vector3 (600 * Time.deltaTime, 0));
			} else {
				player.transform.Translate (new Vector3 (-Power / 0.6f * Time.deltaTime, VSpeed / 2, (NextPlatform.transform.position.z - PreplayerPosition.z) / 0.6f * Time.deltaTime));
				playerPos.transform.Rotate (new Vector3 (0, 0, 600 * Time.deltaTime));
			}
			if (player.transform.position.y <= 1) {
				player.transform.position = new Vector3 (player.transform.position.x, 1.25f, player.transform.position.z);
				playerPos.transform.rotation = Quaternion.Euler (0, 0, 0);
				if (Mathf.Abs (player.transform.position.x - PrePlatform.transform.position.x) < 0.5 && Mathf.Abs (player.transform.position.z - PrePlatform.transform.position.z) < 0.5) {
					gameStatus = GameStatus.TAPING;
				} else {
					if (Mathf.Abs (player.transform.position.x - NextPlatform.transform.position.x) > 0.5 || Mathf.Abs (player.transform.position.z - NextPlatform.transform.position.z) > 0.5) {
						playMusic(deadAudio);
						gameStatus = GameStatus.GAME_OVER;
						Timer = 0;
					} else {
						if (Mathf.Abs (player.transform.position.x - NextPlatform.transform.position.x) < 0.2 && Mathf.Abs (player.transform.position.z - NextPlatform.transform.position.z) < 0.2) {
							playMusic(bonusAudio);
							Bonus++;
							score += Bonus * 2 + extraBonus;
							// white yellow purple
							if (extraBonus < 2)
								extraBonus++;
							printScore();
						} else {
							playMusic(stepAudio);
							Bonus = 0;
							score = score + 1 + extraBonus;
							extraBonus = 0;
							printScore();
						}
						gameStatus = GameStatus.CREATE_PLATFORM;
					}
				}
			}
			break;

		case GameStatus.GAME_OVER:
			if (Timer == 0) {
				Rig = playerPos.AddComponent<Rigidbody> ();
			}
			Timer += Time.deltaTime;
			if (Timer >= 1) {
				Timer = 0;
				gameStatus = GameStatus.PLAY_AGAIN;
			}
			break;

		case GameStatus.PLAY_AGAIN:
			playAgainFunc();
			break;
		default:
			break;
		}

	}
	
}
