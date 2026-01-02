using System;
using System.Collections.Generic;
using GamePlay;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay
{
    public class GameUI : MonoBehaviour
    {
        [Header("HUD References")]
        [SerializeField] private GameObject hudPanel;
        [SerializeField] private Text timerText;
        [SerializeField] private Transform scoreContainer;
        [SerializeField] private GameObject scoreEntryPrefab;

        [Header("Game Over References")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private Text winnerText;
        [SerializeField] private Transform finalScoreContainer;
        [SerializeField] private Button restartButton;

        private readonly Dictionary<int, ScoreEntry> scoreEntries = new Dictionary<int, ScoreEntry>();
        private bool isInitialized;

        private class ScoreEntry
        {
            public GameObject Root;
            public Image ColorImage;
            public Text NameText;
            public Text ScoreText;
        }

        private void Awake()
        {
            CreateUIIfNeeded();
        }

        private void Start()
        {
            if (restartButton != null)
            {
                restartButton.onClick.AddListener(OnRestartClicked);
            }

            HideAll();
        }

        private void CreateUIIfNeeded()
        {
            if (hudPanel != null && gameOverPanel != null)
            {
                isInitialized = true;
                return;
            }

            var canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                gameObject.AddComponent<CanvasScaler>();
                gameObject.AddComponent<GraphicRaycaster>();
            }

            CreateHUDPanel();
            CreateGameOverPanel();

            isInitialized = true;
        }

        private void CreateHUDPanel()
        {
            hudPanel = CreatePanel("HUD Panel");
            
            var timerObj = new GameObject("Timer");
            timerObj.transform.SetParent(hudPanel.transform, false);
            timerText = timerObj.AddComponent<Text>();
            timerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            timerText.fontSize = 48;
            timerText.alignment = TextAnchor.UpperCenter;
            timerText.color = Color.white;
            timerText.text = "60";

            var timerRect = timerText.GetComponent<RectTransform>();
            timerRect.anchorMin = new Vector2(0.5f, 1f);
            timerRect.anchorMax = new Vector2(0.5f, 1f);
            timerRect.pivot = new Vector2(0.5f, 1f);
            timerRect.anchoredPosition = new Vector2(0, -20);
            timerRect.sizeDelta = new Vector2(200, 60);

            var scoreContainerObj = new GameObject("Score Container");
            scoreContainerObj.transform.SetParent(hudPanel.transform, false);
            scoreContainer = scoreContainerObj.transform;

            var scoreRect = scoreContainerObj.AddComponent<RectTransform>();
            scoreRect.anchorMin = new Vector2(0, 1);
            scoreRect.anchorMax = new Vector2(0, 1);
            scoreRect.pivot = new Vector2(0, 1);
            scoreRect.anchoredPosition = new Vector2(20, -20);
            scoreRect.sizeDelta = new Vector2(200, 300);

            var layout = scoreContainerObj.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        private void CreateGameOverPanel()
        {
            if(gameOverPanel)
            {
                gameOverPanel.SetActive(false);
                return;
            }
            gameOverPanel = CreatePanel("GameOver Panel");

            var bgImage = gameOverPanel.AddComponent<Image>();
            bgImage.color = new Color(0, 0, 0, 0.8f);

            winnerText = CreateText(gameOverPanel.transform, "Winner Text", "Winner!", 64);
            var winnerRect = winnerText.GetComponent<RectTransform>();
            winnerRect.anchorMin = new Vector2(0.5f, 0.7f);
            winnerRect.anchorMax = new Vector2(0.5f, 0.7f);
            winnerRect.anchoredPosition = Vector2.zero;
            winnerRect.sizeDelta = new Vector2(400, 80);

            var finalScoreObj = new GameObject("Final Scores");
            finalScoreObj.transform.SetParent(gameOverPanel.transform, false);
            finalScoreContainer = finalScoreObj.transform;

            var finalScoreRect = finalScoreObj.AddComponent<RectTransform>();
            finalScoreRect.anchorMin = new Vector2(0.5f, 0.5f);
            finalScoreRect.anchorMax = new Vector2(0.5f, 0.5f);
            finalScoreRect.pivot = new Vector2(0.5f, 0.5f);
            finalScoreRect.anchoredPosition = Vector2.zero;
            finalScoreRect.sizeDelta = new Vector2(300, 200);

            var finalLayout = finalScoreObj.AddComponent<VerticalLayoutGroup>();
            finalLayout.spacing = 5;
            finalLayout.childForceExpandWidth = true;
            finalLayout.childForceExpandHeight = false;
            finalLayout.childAlignment = TextAnchor.MiddleCenter;

            var restartObj = new GameObject("Restart Button");
            restartObj.transform.SetParent(gameOverPanel.transform, false);

            var restartRect = restartObj.AddComponent<RectTransform>();
            restartRect.anchorMin = new Vector2(0.5f, 0.2f);
            restartRect.anchorMax = new Vector2(0.5f, 0.2f);
            restartRect.sizeDelta = new Vector2(200, 50);

            var buttonImage = restartObj.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.6f, 0.2f);

            restartButton = restartObj.AddComponent<Button>();
            restartButton.targetGraphic = buttonImage;

            var buttonText = CreateText(restartObj.transform, "Button Text", "Restart", 24);
            var buttonTextRect = buttonText.GetComponent<RectTransform>();
            buttonTextRect.anchorMin = Vector2.zero;
            buttonTextRect.anchorMax = Vector2.one;
            buttonTextRect.sizeDelta = Vector2.zero;
        }
      
        private GameObject CreatePanel(string name)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(transform, false);

            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            return panel;
        }

        private Text CreateText(Transform parent, string name, string content, int fontSize)
        {
            var textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);

            var text = textObj.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = content;

            var rect = text.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, fontSize + 10);

            return text;
        }

        public void ShowGameHUD()
        {
            HideAll();
            if (hudPanel != null)
            {
                hudPanel.SetActive(true);
            }
        }

        public void ShowGameOver(List<(int playerId, int score, Color color)> scores)
        {
            if (hudPanel)
            {
                hudPanel.SetActive(false);
            }

            if (gameOverPanel)
            {
                gameOverPanel.SetActive(true);
            }

            scores.Sort((a, b) => b.score.CompareTo(a.score));

            if (scores.Count > 0 && winnerText != null)
            {
                winnerText.text = $"Player {scores[0].playerId + 1} Wins!";
                winnerText.color = scores[0].color;
            }

            if (finalScoreContainer != null)
            {
                foreach (Transform child in finalScoreContainer)
                {
                    Destroy(child.gameObject);
                }

                foreach (var score in scores)
                {
                    var text = CreateText(finalScoreContainer, $"Score_{score.playerId}", 
                        $"P{score.playerId + 1}: {score.score} eggs", 24);
                    text.color = score.color;
                }
            }
        }

        public void UpdateTimer(float remainingTime)
        {
            if (timerText != null)
            {
                int seconds = Mathf.CeilToInt(remainingTime);
                timerText.text = seconds.ToString();

                if (seconds <= 10)
                {
                    timerText.color = Color.red;
                }
                else
                {
                    timerText.color = Color.white;
                }
            }
        }

        public void UpdateScore(int playerId, int score)
        {
            if (!scoreEntries.TryGetValue(playerId, out var entry))
            {
                entry = CreateScoreEntry(playerId);
                scoreEntries[playerId] = entry;
            }

            if (entry.ScoreText != null)
            {
                entry.ScoreText.text = $"x{score}";
            }
        }

        public void SetPlayerColor(int playerId, Color color)
        {
            if (scoreEntries.TryGetValue(playerId, out var entry))
            {
                if (entry.ColorImage != null)
                {
                    entry.ColorImage.color = color;
                }
                if (entry.NameText != null)
                {
                    entry.NameText.color = color;
                }
            }
        }

        private ScoreEntry CreateScoreEntry(int playerId)
        {
            var entryObj = new GameObject($"Score_{playerId}");
            entryObj.transform.SetParent(scoreContainer, false);

            var rect = entryObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(180, 30);

            var layout = entryObj.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            var colorObj = new GameObject("Color");
            colorObj.transform.SetParent(entryObj.transform, false);
            var colorImage = colorObj.AddComponent<Image>();
            colorImage.color = Color.white;
            var colorRect = colorObj.GetComponent<RectTransform>();
            colorRect.sizeDelta = new Vector2(20, 20);
            var colorLayout = colorObj.AddComponent<LayoutElement>();
            colorLayout.minWidth = 20;
            colorLayout.preferredWidth = 20;

            var nameText = CreateText(entryObj.transform, "Name", $"P{playerId + 1}", 20);
            nameText.alignment = TextAnchor.MiddleLeft;
            var nameLayout = nameText.gameObject.AddComponent<LayoutElement>();
            nameLayout.flexibleWidth = 1;

            var scoreText = CreateText(entryObj.transform, "Score", "x0", 20);
            scoreText.alignment = TextAnchor.MiddleRight;
            scoreText.color = Color.yellow;
            var scoreLayout = scoreText.gameObject.AddComponent<LayoutElement>();
            scoreLayout.minWidth = 50;

            return new ScoreEntry
            {
                Root = entryObj,
                ColorImage = colorImage,
                NameText = nameText,
                ScoreText = scoreText
            };
        }

        public void HideAll()
        {
            if (hudPanel) hudPanel.SetActive(false);
            if (gameOverPanel) gameOverPanel.SetActive(false);
        }


        private void OnRestartClicked()
        {
            HideAll();
            GameManager.Instance?.RestartGame();
        }

    }
}