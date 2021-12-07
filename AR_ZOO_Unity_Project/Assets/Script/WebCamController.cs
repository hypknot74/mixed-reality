using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Unity.Barracuda;

public class WebCamController : MonoBehaviour
 {
    int width = 640;
    int height = 480;
    int fps = 30;
    WebCamTexture webcamTexture;

    // MobileNetモデル関連
    public NNModel modelAsset;    
    private MobileNet mobileNet;
    
    Texture2D texture;
    Color32[] color32;

    // GameObject を定義
    private GameObject[] ARObjects;

    // Startから5秒経っているかのFlag
    private bool IsProcessingFlag;
    private float elapsedTime;
    private float[] scoreSums;
    private int cnt;

    // 推論結果描画用テキスト
    public Text text;
    private readonly FPSCounter fpsCounter = new FPSCounter();

    void Start() 
    {   

        // ARObject の初期化
        ARObjects = getARObjects();
        InitializeARObjects();

        // Webカメラ準備
        WebCamDevice[] devices = WebCamTexture.devices;
        webcamTexture = new WebCamTexture(devices[0].name, this.width, this.height, this.fps);
        webcamTexture.Play();
        
        // MobileNetV2推論用クラス
        mobileNet = new MobileNet(modelAsset);
        StartCoroutine(InitializeWebCamTexture());
    }

    IEnumerator InitializeWebCamTexture()
    {
        while (true) {
            Debug.Log(webcamTexture.width);
            Debug.Log(webcamTexture.height);
            if (webcamTexture.width > 16 && webcamTexture.height > 16) {
                GetComponent<Renderer>().material.mainTexture = webcamTexture;
                color32 = new Color32[webcamTexture.width * webcamTexture.height];
                texture = new Texture2D(webcamTexture.width, webcamTexture.height);
                break;
            }
            yield return null;
        }
    }

    void InitializeARObjects(){
        foreach (GameObject ARObject in ARObjects) {
            ARObject.SetActive(false);
        }
    }

    void Update()
    {   
        if (color32 != null && texture != null){

            if (Input.GetKey(KeyCode.Space)){
                elapsedTime = 0;
                IsProcessingFlag = StartProcess();
            }

            if (IsProcessingFlag){
                elapsedTime += Time.deltaTime;
                Process();

                if (elapsedTime > 5f){
                    IsProcessingFlag = EndProcess();
                }
            }
        }

    }

    private bool StartProcess(){
        InitializeARObjects();
        scoreSums = new float[ARObjects.Length];
        cnt = 0;
        return true;
    }

    private bool EndProcess(){
        var maxScore = float.MinValue;
        int classId = -1;
        for (int i = 0; i < scoreSums.Length; i++) {
            if (maxScore < scoreSums[i]) {
                maxScore = scoreSums[i];
                classId = i;
            }
        }
        // 推論の最終結果テキスト構築とARObjectの表示
        float maxScoreRate = maxScore/cnt;
        string resultText = "";
        resultText += "--- Result ---\n";
        resultText += "Class ID: " + classId.ToString() + "\n";
        resultText += "Score: " + maxScoreRate.ToString("F3") + "\n";

        if (classId >= 0) {

            ARObjects[classId].SetActive(true);
            
            resultText += "Name: " + mobileNet.getClassName(classId) + "\n";
        } else {
            resultText += "Name:????\n";
        }

        // テキスト画面反映
        text.text = resultText;

        return false;
    }

    private void Process(){
        
        // Inference 実行回数
        cnt++;

        // fps のアップデート
        fpsCounter.Update();

        // 入力用テクスチャ準備
        webcamTexture.GetPixels32(color32);
        texture.SetPixels32(color32);
        texture.Apply();
        
        // 推論
        var scores = mobileNet.Inference(texture);
        
        // 推論結果とテキスト構築
        string resultText = "";
        resultText += "FPS:" + fpsCounter.FPS.ToString("F2") + "\n" + "\n";  

        // score各クラスのスコアを加算する
        for (int i = 0; i < scores.Length; i++) {
            scoreSums[i] += scores[i];
            resultText += scores[i].ToString("F3") + " : " + mobileNet.getClassName(i) + "\n";
        }
        #if UNITY_IOS || UNITY_ANDROID
                resultText += SystemInfo.graphicsDeviceType;
        #endif

        // テキスト画面反映
        text.text = resultText;
    }

// ==========ARObjects==========

    public GameObject Buildings;
    public GameObject Forests;
    public GameObject Glacier;
    public GameObject Mountains;
    public GameObject Sea;
    public GameObject Street;

    private GameObject[] getARObjects()
    {
        var ARObjects = new GameObject[]
        {
          Buildings,
          Forests,
          Glacier,
          Mountains,
          Sea,
          Street
        };
        
        return ARObjects;
    }

}