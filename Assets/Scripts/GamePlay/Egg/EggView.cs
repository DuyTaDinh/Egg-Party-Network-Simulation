using System;
using UnityEngine;
using Network.Client;

namespace Gameplay.Egg
{
    public class EggView : MonoBehaviour
    {
        static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        
        [Header("Visual Settings")]
        [SerializeField] private float bobSpeed = 2f;
        [SerializeField] private float bobAmount = 0.2f;
        [SerializeField] private float rotateSpeed = 90f;
        [SerializeField] private float heightOffset = 0.5f;

        [Header("Components")]
        [SerializeField] private MeshRenderer meshRenderer;

        private int eggId;
        private Color eggColor;
        private Vector3 basePosition;
        private float bobOffset;
        private bool isInitialized;
        private Material material;
        private bool isCollected;
        private float collectAnimTime;

        public int EggId => eggId;
        public bool IsCollected => isCollected;

        public void Initialize(int id, Vector3 position, Color color)
        {
            eggId = id;
            eggColor = color;
            basePosition = position + Vector3.up * heightOffset;
            transform.position = basePosition;
            bobOffset = UnityEngine.Random.Range(0f, Mathf.PI * 2f);

            if (!meshRenderer)
            {
                CreateVisual();
            }

            SetColor(color);
            isInitialized = true;
            isCollected = false;
        }

        private void CreateVisual()
        {
            var sphere = gameObject.AddComponent<MeshFilter>();
            sphere.mesh = CreateEggMesh();

            meshRenderer = gameObject.AddComponent<MeshRenderer>();
            material = new Material(Shader.Find("Standard"));
            material.EnableKeyword("_EMISSION");
            meshRenderer.material = material;

            var collider = gameObject.AddComponent<SphereCollider>();
            collider.radius = 0.3f;
            collider.isTrigger = true;
        }

        private Mesh CreateEggMesh()
        {
            GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Mesh mesh = temp.GetComponent<MeshFilter>().mesh;
            Mesh clonedMesh = UnityEngine.Object.Instantiate(mesh);
            DestroyImmediate(temp);
            
            Vector3[] vertices = clonedMesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                float y = vertices[i].y;
                float scale = 1f + (y > 0 ? -0.2f * y : 0.1f * Mathf.Abs(y));
                vertices[i] = new Vector3(vertices[i].x * scale * 0.7f, vertices[i].y * 1.2f, vertices[i].z * scale * 0.7f);
            }
            clonedMesh.vertices = vertices;
            clonedMesh.RecalculateNormals();
            clonedMesh.RecalculateBounds();

            return clonedMesh;
        }

        public void SetColor(Color color)
        {
            eggColor = color;
            if (material != null)
            {
                material.color = color;
                material.SetColor(EmissionColor, color * 0.3f);
            }
        }

        public void Collect()
        {
            if (isCollected) return;
            isCollected = true;
            collectAnimTime = 0;
        }

        private void Update()
        {
            if (!isInitialized) return;

            if (isCollected)
            {
                collectAnimTime += Time.deltaTime * 3f;
                float scale = Mathf.Lerp(1f, 0f, collectAnimTime);
                transform.localScale = Vector3.one * scale;
                transform.position = basePosition + Vector3.up * (collectAnimTime * 2f);

                if (collectAnimTime >= 1f)
                {
                    gameObject.SetActive(false);
                }
                return;
            }

            float bob = Mathf.Sin((Time.time + bobOffset) * bobSpeed) * bobAmount;
            transform.position = basePosition + Vector3.up * bob;
            transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
        }

        public void SetPosition(Vector3 position)
        {
            basePosition = position + Vector3.up * heightOffset;
        }

        private void OnDestroy()
        {
            if (material)
            {
                Destroy(material);
            }
        }
    }
}