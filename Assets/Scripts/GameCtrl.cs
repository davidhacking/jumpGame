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
	float Timer;
	float Scale;
	bool IsPressing;
	int Bonus;
	GameStatus gameStatus;
	// all players data struct
	Player[] playerList;
	// 当前玩家的索引号
	int THIS_PLAYER_INDEX = 0;
	List<GameObject> platforms;
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

    public struct Player {
		public GameObject player;
		public GameObject playerLabel;
		public TextMesh playerLabelTextMesh;
		public GameObject playerPos;
		public Vector3 prePlayerPosition;
		public float VSpeed;
		public float power;
		public int currentPlatformIndex;
		// 决定跳的方向
		public bool direction;
		// 动画播放队列
		public List<string> animationQueue;
	};

	public void clearPlayer(Player[] playerList) {
		if (playerList == null) {
			return;
		}
		for (int i = 0; i < playerList.Length; i++) {
			destroyPlayer(ref playerList[i]);
		}
	}

	public void destroyPlayer(ref Player p) {
		if (p.player != null) {
			Destroy(p.player);
		}
		if (p.player != null) {
			Destroy(p.playerLabel);
		}
	}

	public Player[] newPlayerList(int len) {
		Player[] playerList = new Player[len];
		for (int i = 0; i < len; i++) {
			playerList[i].player = Instantiate(playerAsset, new Vector3 (0, 1.25f, randomPos(0, i)), Quaternion.Euler (Vector3.zero));
			playerList[i].playerPos = playerList[i].player.transform.Find("position").gameObject;
			playerList[i].playerLabel = new GameObject();
			playerList[i].playerLabel.transform.position = playerList[i].player.transform.position + new Vector3(0, playerList[i].player.transform.position.y + 0.4f * i, 0);
			playerList[i].playerLabelTextMesh = playerList[i].playerLabel.AddComponent<TextMesh>() as TextMesh;
			playerList[i].playerLabel.AddComponent<MeshRenderer>();
			playerList[i].playerLabel.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
			playerList[i].playerLabelTextMesh.text = "Id: " + i;
			playerList[i].currentPlatformIndex = 0;
			playerList[i].animationQueue = new List<string>();
		}
		return playerList;
	}

	void displayPlayerLabel(int index, string text) {
		setEnable(index);
		playerList[index].playerLabel.transform.position = playerList[index].player.transform.position + new Vector3(0, playerList[index].player.transform.position.y + 0.4f * index, 0);
		playerList[index].playerLabelTextMesh.text = text;
	}

	void setDisable(int index) {
		playerList[index].playerLabel.SetActive(false);
	}

	void setEnable(int index) {
		playerList[index].playerLabel.SetActive(true);
	}

	GameObject nextPlatform(int index) {
		return platforms[playerList[index].currentPlatformIndex + 1];
	}

	GameObject currPlatform(int index) {
		return platforms[playerList[index].currentPlatformIndex];
	}

	void displayOtherPlayer(int index) {
		print("index: " + index + "; playerList[index].direction: " + playerList[index].direction + "; playerList[index].direction: " + playerList[index].currentPlatformIndex);
		if (!playerList[index].direction) {
			playerList[index].player.transform.position = new Vector3(currPlatform(index).transform.position.x, 1.25f, randomPos(currPlatform(index).transform.position.z, index));
		} else {
			playerList[index].player.transform.position = new Vector3(randomPos(currPlatform(index).transform.position.x, index), 1.25f, currPlatform(index).transform.position.z);
		}
		playerList[index].playerPos.transform.rotation = Quaternion.Euler(0, 0, 0);
	}

	void displayPlayerInNextPlatform(int index) {
		if (!playerList[index].direction) {
			playerList[index].player.transform.position = new Vector3(playerList[index].player.transform.position.x, 1.25f, randomPos(nextPlatform(index).transform.position.z, index));
		} else {
			playerList[index].player.transform.position = new Vector3(randomPos(nextPlatform(index).transform.position.x, index), 1.25f, playerList[index].player.transform.position.z);
		}
		playerList[index].playerPos.transform.rotation = Quaternion.Euler(0, 0, 0);
		// for test
		if (index == THIS_PLAYER_INDEX) {
			playerList[1].VSpeed = 0.3f;
			playerList[1].direction = playerList[THIS_PLAYER_INDEX].direction;
			playerList[1].animationQueue.Insert(playerList[1].animationQueue.Count, "jump");
		}
	}
	bool playerJump(int index) {
		setDisable(index);
		playerList[index].VSpeed -= Time.deltaTime;
		print("index: " + index + "; playerList[index].direction: " + playerList[index].direction);
		if (playerList[index].direction) {
			//playerList[index].player.transform.Translate(new Vector3((nextPlatform(index).transform.position.x - playerList[index].prePlayerPosition.x) / 0.6f * Time.deltaTime, playerList[index].VSpeed / 2, playerList[index].power / 0.6f * Time.deltaTime));
			playerList[index].player.transform.Translate(new Vector3(0, playerList[index].VSpeed / 2, playerList[index].power / 0.6f * Time.deltaTime));
			playerList[index].playerPos.transform.Rotate(new Vector3(720 * Time.deltaTime, 0));
		} else {
			//playerList[index].player.transform.Translate(new Vector3(-playerList[index].power / 0.6f * Time.deltaTime, playerList[index].VSpeed / 2, (nextPlatform(index).transform.position.z - playerList[index].prePlayerPosition.z) / 0.6f * Time.deltaTime));
			playerList[index].player.transform.Translate(new Vector3(-playerList[index].power / 0.6f * Time.deltaTime, playerList[index].VSpeed / 2, 0));
			playerList[index].playerPos.transform.Rotate(new Vector3(0, 0, 720 * Time.deltaTime));
		}
		if (playerList[index].player.transform.position.y <= 1) {
			displayPlayerInNextPlatform(index);
			if (Mathf.Abs(playerList[index].player.transform.position.x - currPlatform(index).transform.position.x) < 0.5 && Mathf.Abs (playerList[index].player.transform.position.z - currPlatform(index).transform.position.z) < 0.5) {
				gameStatus = GameStatus.TAPING;
			} else {
				if (Mathf.Abs(playerList[index].player.transform.position.x - nextPlatform(index).transform.position.x) > 0.5 || Mathf.Abs (playerList[index].player.transform.position.z - nextPlatform(index).transform.position.z) > 0.5) {
					if (index == THIS_PLAYER_INDEX) {
						playMusic(deadAudio);
						gameStatus = GameStatus.GAME_OVER;
						Timer = 0;
					}
					// for test
					playerList[index].animationQueue.Insert(playerList[index].animationQueue.Count, "game_over");
				} else {
					if (index == THIS_PLAYER_INDEX) {
						if (Mathf.Abs(playerList[index].player.transform.position.x - nextPlatform(index).transform.position.x) < 0.2 && Mathf.Abs(playerList[index].player.transform.position.z - nextPlatform(index).transform.position.z) < 0.2) {
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
					// 当前platform+1
					playerList[index].currentPlatformIndex++;
				}
			}
			displayPlayerLabel(index, "jump");
			return true;
		}
		return false;
	}

	public void syncAnimation() {
		// other player status
		int t = playerList[THIS_PLAYER_INDEX].currentPlatformIndex;
		for (int i = 0; i < playerList.Length; i++) {
			if (i == THIS_PLAYER_INDEX) {
				continue;
			}
			if (playerList[i].animationQueue.Count > 0) {
				// play jump animation
				if (playerList[i].animationQueue[0] == "jump") {
					bool endFlag = playerJump(i);
					if (endFlag) {
						playerList[i].animationQueue.RemoveAt(0);
					}
				} else if (playerList[i].animationQueue[0] == "game_over") { // game over animation
					playerList[i].playerPos.AddComponent<Rigidbody> ();
					playerList[i].animationQueue.RemoveAt(0);
				}
				continue;
			}
			if (playerList[i].currentPlatformIndex == t || playerList[i].currentPlatformIndex == (t + 1)) {
				//displayOtherPlayer(i);
				//displayPlayerLabel(i, "sync");
			}
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
		if (playerList[THIS_PLAYER_INDEX].direction) {
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
		platforms = new List<GameObject>();
		scoretext = scoretext.GetComponent<Text>();
		IsPressing = false;
		Bonus = 0;

		initGame();
	}

	private GameObject playerLabel;
	private TextMesh playerLabelTextMesh;

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
		if (platforms != null) {
			foreach (GameObject current in platforms) {
				Destroy(current);
			}
		}
		platforms = new List<GameObject>();
		clearPlayer(playerList);
		playerList = newPlayerList(2);
		Vector3 position = new Vector3 (0, 0.52f, 0);
		platforms.Add(Instantiate(Platform, position, Quaternion.Euler (Vector3.zero)));
		gameStatus = GameStatus.CREATE_PLATFORM;
	}

	bool nextDirection() {
		return (Random.Range (0, 10) > 5);
		//return true;
	}

	float randomPos(float pos, int index) {
		float[] posList = {pos - 0.4f, pos - 0.2f, pos, pos + 0.2f, pos + 0.4f};
		if (index >= 0 && index <= 4) {
			return posList[index];
		}
		return pos;
	}

	void createPlatform() {
		playerList[THIS_PLAYER_INDEX].direction = nextDirection();
		platforms.Insert(platforms.Count, Instantiate(Platform, currPlatform(THIS_PLAYER_INDEX).transform.position + randomDelta(), Quaternion.Euler (Vector3.zero)));
		// render color
		if (extraBonus > 0) {
			Material materialColored = new Material(Shader.Find("Diffuse"));
	        materialColored.color = extraBonus == 1 ? Color.green : Color.yellow;
	        nextPlatform(THIS_PLAYER_INDEX).GetComponent<Renderer>().material = materialColored;
	    }

		Point1 = nextPlatform(THIS_PLAYER_INDEX).transform.position;
		Point2 = Point1;
		Point2.y = 0.5f;

        //重新设置相机的位置
		cameraCtrl.SetPosition ((currPlatform(THIS_PLAYER_INDEX).transform.position + Point2) / 2);

		Timer = 0;
		gameStatus = GameStatus.SHOW_PLATFORM;
		playerList[THIS_PLAYER_INDEX].power = 0;
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
	void Update() {

		if (MasterSceneManager.Instance.mainPause) {
			return;
		}

		syncAnimation();

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
			nextPlatform(THIS_PLAYER_INDEX).transform.position = Vector3.Lerp(Point1, Point2, Timer * Speed);

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
						playerList[THIS_PLAYER_INDEX].power = Timer * 3;
						// for test 
						playerList[1].power = Timer * 3;
						currPlatform(THIS_PLAYER_INDEX).transform.localScale = new Vector3 (1, 1 - 0.2f * Timer, 1);
						currPlatform(THIS_PLAYER_INDEX).transform.Translate (0, -0.1f * Time.deltaTime, 0);
						playerList[THIS_PLAYER_INDEX].player.transform.Translate (0, -0.2f * Time.deltaTime, 0);
					}
				} else {
					stopMusic(pressingAudio);
					IsPressing = false;
					gameStatus = GameStatus.REBOUND;
					Scale = currPlatform(THIS_PLAYER_INDEX).transform.localScale.y;
				}
			}
			break;

		case GameStatus.REBOUND:
			Timer += Time.deltaTime;
			currPlatform(THIS_PLAYER_INDEX).transform.localScale = Vector3.Lerp (new Vector3 (1, Scale, 1), new Vector3 (1, 1, 1), Timer);
			currPlatform(THIS_PLAYER_INDEX).transform.Translate (0, 0.5f * Time.deltaTime, 0);
			playerList[THIS_PLAYER_INDEX].player.transform.Translate (0, 1.0f * Time.deltaTime, 0);

			if (currPlatform(THIS_PLAYER_INDEX).transform.position.y >= 0.5) {
				gameStatus = GameStatus.PLAYER_JUMPING;
				playerList[THIS_PLAYER_INDEX].VSpeed = 0.3f;
				playerList[THIS_PLAYER_INDEX].prePlayerPosition = playerList[THIS_PLAYER_INDEX].player.transform.position;
				// for test
				playerList[1].prePlayerPosition = playerList[THIS_PLAYER_INDEX].prePlayerPosition;
			}
			// clear gameobject
			//if (platforms.Count > 5) {
			//	platforms.RemoveAt (0);
			//	Destroy ((GameObject) platforms[0]);
			//}
			break;

		case GameStatus.PLAYER_JUMPING:
			playerJump(0);
			break;

		case GameStatus.GAME_OVER:
			if (Timer == 0) {
				Rig = playerList[THIS_PLAYER_INDEX].playerPos.AddComponent<Rigidbody> ();
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
