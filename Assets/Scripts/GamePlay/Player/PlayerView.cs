using System;
using UnityEngine;

namespace Gameplay.Player
{
    [RequireComponent(typeof(CapsuleCollider))]
    public class PlayerView : MonoBehaviour
    {
        [Header("Visual Settings")]
        [SerializeField] private float moveSmoothing = 15f;
        [SerializeField] private float heightOffset = 0.5f;

        [Header("Components")]
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private TextMesh nameLabel;
        [SerializeField] private TextMesh scoreLabel;

        private int playerId;
        private Color playerColor;
        private Vector3 targetPosition;
        private bool isInitialized;
        private Material material;

        public int PlayerId => playerId;
        public bool IsLocal { get; private set; }

        public void Initialize(int id, Color color, bool isLocal)
        {
            playerId = id;
            playerColor = color;
            IsLocal = isLocal;

            if (!meshRenderer)
            {
                CreateVisual();
            }
            else
            {
                material = meshRenderer.material;
            }

            SetColor(color);
            UpdateNameLabel($"P{id + 1}");
            UpdateScoreLabel(0);
            isInitialized = true;
        }

        private void CreateVisual()
        {
            var capsule = gameObject.AddComponent<MeshFilter>();
            capsule.mesh = CreateCapsuleMesh();

            meshRenderer = gameObject.AddComponent<MeshRenderer>();
            material = new Material(Shader.Find("Standard"));
            meshRenderer.material = material;

            var nameObj = new GameObject("NameLabel");
            nameObj.transform.SetParent(transform);
            nameObj.transform.localPosition = new Vector3(0, 1.5f, 0);
            nameLabel = nameObj.AddComponent<TextMesh>();
            nameLabel.alignment = TextAlignment.Center;
            nameLabel.anchor = TextAnchor.MiddleCenter;
            nameLabel.fontSize = 32;
            nameLabel.characterSize = 0.1f;

            var scoreObj = new GameObject("ScoreLabel");
            scoreObj.transform.SetParent(transform);
            scoreObj.transform.localPosition = new Vector3(0, 1.8f, 0);
            scoreLabel = scoreObj.AddComponent<TextMesh>();
            scoreLabel.alignment = TextAlignment.Center;
            scoreLabel.anchor = TextAnchor.MiddleCenter;
            scoreLabel.fontSize = 28;
            scoreLabel.characterSize = 0.08f;
            scoreLabel.color = Color.yellow;
        }

        private Mesh CreateCapsuleMesh()
        {
            GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Mesh mesh = temp.GetComponent<MeshFilter>().mesh;
            Mesh clonedMesh = UnityEngine.Object.Instantiate(mesh);
            DestroyImmediate(temp);
            return clonedMesh;
        }

        public void SetColor(Color color)
        {
            playerColor = color;
            if (material)
            {
                material.color = color;
            }
            if (nameLabel)
            {
                nameLabel.color = color;
            }
        }

        public void UpdatePosition(Vector3 position)
        {
            targetPosition = position + Vector3.up * heightOffset;
        }

        public void SetPositionImmediate(Vector3 position)
        {
            targetPosition = position + Vector3.up * heightOffset;
            transform.position = targetPosition;
        }

        public void UpdateNameLabel(string name)
        {
            if (nameLabel != null)
            {
                nameLabel.text = name;
            }
        }

        public void UpdateScoreLabel(int score)
        {
            if (scoreLabel != null)
            {
                scoreLabel.text = $"x{score}";
            }
        }

        private void Update()
        {
            if (!isInitialized) return;

            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * moveSmoothing);

            if (nameLabel)
            {
                nameLabel.transform.forward = Camera.main ? Camera.main.transform.forward : Vector3.forward;
            }
            if (scoreLabel)
            {
                scoreLabel.transform.forward = Camera.main ? Camera.main.transform.forward : Vector3.forward;
            }
        }

        private void OnDestroy()
        {
            if (material != null)
            {
                Destroy(material);
            }
        }
    }
}