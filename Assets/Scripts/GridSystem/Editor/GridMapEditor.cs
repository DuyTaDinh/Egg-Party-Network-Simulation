using System.Collections.Generic;
using GridSystem.Core;
using UnityEditor;
using UnityEngine;

namespace GridSystem.Editor
{
    public class GridMapEditor : EditorWindow
    {
        // References
        private MapGridData mapData;
        private CellDatabase cellDatabase;

        // Paint Settings
        private CellType selectedCellType = CellType.Ground;
        private int brushSize = 1;
        private bool isPainting = false;
        private bool isDraggingView = false;

        // Tool Modes
        private enum ToolMode
        {
            Paint,
            Erase,
            BucketFill,
            Eyedropper
        }

        private ToolMode currentTool = ToolMode.Paint;

        // View Settings
        private Vector2 scrollPosition;
        private float zoomLevel = 1.0f;
        private Vector2 gridOffset = Vector2.zero;
        private bool showGrid = true;
        private bool showCoordinates = false;

        // Undo System
        private Stack<CellType[]> undoStack = new Stack<CellType[]>();
        private Stack<CellType[]> redoStack = new Stack<CellType[]>();
        private const int maxUndoSteps = 20;

        // UI Settings
        private Vector2 paletteScroll;
        private Color gridColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);
        private GUIStyle selectedCellStyle;
        private GUIStyle cellButtonStyle;
        private GUIStyle toolbarStyle;

        // Grid rendering
        private Rect gridViewRect;
        private Vector2Int hoveredCell = new Vector2Int(-1, -1);

        [MenuItem("Tools/Grid Map Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<GridMapEditor>("Grid Map Editor");
            window.minSize = new Vector2(1000, 700);
            window.Show();
        }

        void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            InitializeStyles();
        }

        void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        void InitializeStyles()
        {
            selectedCellStyle = null; // Will be initialized in OnGUI
            cellButtonStyle = null;
        }

        void OnGUI()
        {
            InitializeStylesIfNeeded();

            DrawToolbar();

            EditorGUILayout.BeginHorizontal();
            {
                DrawLeftPanel();
                DrawGridViewPanel();
                DrawRightPanel();
            }
            EditorGUILayout.EndHorizontal();

            HandleKeyboardShortcuts();

            if (Event.current.type == EventType.MouseMove || Event.current.type == EventType.MouseDrag)
            {
                Repaint();
            }
        }

        void InitializeStylesIfNeeded()
        {
            if (selectedCellStyle == null)
            {
                selectedCellStyle = new GUIStyle(GUI.skin.box);
                selectedCellStyle.normal.background = MakeTex(2, 2, new Color(0.3f, 0.6f, 1f, 0.5f));
                selectedCellStyle.border = new RectOffset(2, 2, 2, 2);
            }

            if (cellButtonStyle == null)
            {
                cellButtonStyle = new GUIStyle(GUI.skin.button);
                cellButtonStyle.alignment = TextAnchor.MiddleLeft;
                cellButtonStyle.padding = new RectOffset(8, 8, 6, 6);
            }
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;

            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        // ========================================================================
        // TOOLBAR
        // ========================================================================

        void DrawToolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.ExpandWidth(true));
            {
                // Map Data Reference
                EditorGUI.BeginChangeCheck();
                mapData = (MapGridData)EditorGUILayout.ObjectField(
                    mapData, typeof(MapGridData), false, GUILayout.Width(200)
                );
                if (EditorGUI.EndChangeCheck() && mapData != null)
                {
                    cellDatabase = mapData.cellDatabase;
                }

                GUILayout.FlexibleSpace();

                // Tools with icons
                Color originalColor = GUI.backgroundColor;

                GUI.backgroundColor = currentTool == ToolMode.Paint ? Color.green : originalColor;
                if (GUILayout.Button(new GUIContent("✏️ Paint (B)", "Paint cells"), EditorStyles.toolbarButton,
                        GUILayout.Width(100)))
                    currentTool = ToolMode.Paint;

                GUI.backgroundColor = currentTool == ToolMode.Erase ? Color.red : originalColor;
                if (GUILayout.Button(new GUIContent("🗑️ Erase (E)", "Erase cells"), EditorStyles.toolbarButton,
                        GUILayout.Width(100)))
                    currentTool = ToolMode.Erase;

                GUI.backgroundColor = currentTool == ToolMode.BucketFill ? Color.cyan : originalColor;
                if (GUILayout.Button(new GUIContent("🪣 Fill (F)", "Bucket fill"), EditorStyles.toolbarButton,
                        GUILayout.Width(100)))
                    currentTool = ToolMode.BucketFill;

                GUI.backgroundColor = currentTool == ToolMode.Eyedropper ? Color.yellow : originalColor;
                if (GUILayout.Button(new GUIContent("💧 Pick (I)", "Pick cell type"), EditorStyles.toolbarButton,
                        GUILayout.Width(100)))
                    currentTool = ToolMode.Eyedropper;

                GUI.backgroundColor = originalColor;

                GUILayout.FlexibleSpace();

                // Actions
                GUI.enabled = undoStack.Count > 0;
                if (GUILayout.Button("↶ Undo", EditorStyles.toolbarButton, GUILayout.Width(80)))
                    PerformUndo();

                GUI.enabled = redoStack.Count > 0;
                if (GUILayout.Button("↷ Redo", EditorStyles.toolbarButton, GUILayout.Width(80)))
                    PerformRedo();

                GUI.enabled = true;

                if (GUILayout.Button("🗑️ Clear", EditorStyles.toolbarButton, GUILayout.Width(80)))
                {
                    if (EditorUtility.DisplayDialog("Clear Map", "Clear all cells?", "Yes", "No"))
                    {
                        RecordUndo();
                        mapData.Clear();
                        EditorUtility.SetDirty(mapData);
                    }
                }

                if (GUILayout.Button("💾 Save", EditorStyles.toolbarButton, GUILayout.Width(80)))
                {
                    EditorUtility.SetDirty(mapData);
                    AssetDatabase.SaveAssets();
                    ShowNotification(new GUIContent("✓ Map Saved!"));
                }
            }
            GUILayout.EndHorizontal();
        }

        // ========================================================================
        // LEFT PANEL (MAP SETTINGS & PALETTE)
        // ========================================================================

        void DrawLeftPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(280));
            {
                DrawMapSettings();
                EditorGUILayout.Space(10);
                DrawPalette();
            }
            EditorGUILayout.EndVertical();
        }

        void DrawMapSettings()
        {
            GUILayout.Label("📋 Map Settings", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical("box");
            {
                if (mapData == null)
                {
                    EditorGUILayout.HelpBox("⚠️ Select or create a MapGridData asset", MessageType.Warning);
                    if (GUILayout.Button("Create New Map Data", GUILayout.Height(30)))
                    {
                        CreateNewMapData();
                    }

                    EditorGUILayout.EndVertical();
                    return;
                }

                EditorGUI.BeginChangeCheck();

                int newWidth = EditorGUILayout.IntField("Width", mapData.width);
                int newHeight = EditorGUILayout.IntField("Height", mapData.height);

                if (EditorGUI.EndChangeCheck() && (newWidth != mapData.width || newHeight != mapData.height))
                {
                    if (EditorUtility.DisplayDialog("Resize Map",
                            $"Resize map to {newWidth}x{newHeight}?\nThis may lose data outside new bounds.", "Resize",
                            "Cancel"))
                    {
                        RecordUndo();
                        mapData.Resize(Mathf.Max(1, newWidth), Mathf.Max(1, newHeight));
                        EditorUtility.SetDirty(mapData);
                    }
                }

                mapData.cellSize = EditorGUILayout.FloatField("Cell Size", mapData.cellSize);
                mapData.worldOffset = EditorGUILayout.Vector3Field("World Offset", mapData.worldOffset);

                EditorGUI.BeginChangeCheck();
                var newDatabase = (CellDatabase)EditorGUILayout.ObjectField(
                    "Cell Database", mapData.cellDatabase, typeof(CellDatabase), false
                );
                if (EditorGUI.EndChangeCheck())
                {
                    mapData.cellDatabase = newDatabase;
                    cellDatabase = newDatabase;
                    EditorUtility.SetDirty(mapData);
                }

                if (mapData.cellDatabase == null)
                {
                    EditorGUILayout.HelpBox("⚠️ Assign a CellDatabase!", MessageType.Error);
                }

                GUILayout.Space(5);

                if (GUILayout.Button("Initialize Map", GUILayout.Height(25)))
                {
                    mapData.Initialize();
                    EditorUtility.SetDirty(mapData);
                    ShowNotification(new GUIContent("✓ Map Initialized!"));
                }
            }
            EditorGUILayout.EndVertical();
        }

        void DrawPalette()
        {
            GUILayout.Label("🎨 Cell Palette", EditorStyles.boldLabel);

            if (mapData == null || mapData.cellDatabase == null)
            {
                EditorGUILayout.HelpBox("⚠️ Database not assigned", MessageType.Warning);
                return;
            }

            EditorGUILayout.BeginVertical("box");
            {
                paletteScroll = EditorGUILayout.BeginScrollView(paletteScroll, GUILayout.Height(400));
                {
                    if (mapData.cellDatabase.cells == null || mapData.cellDatabase.cells.Count == 0)
                    {
                        EditorGUILayout.HelpBox("No cells in database. Add cells in the CellDatabase asset.",
                            MessageType.Info);
                    }
                    else
                    {
                        foreach (var cellData in mapData.cellDatabase.cells)
                        {
                            if (cellData != null)
                            {
                                DrawPaletteItem(cellData);
                            }
                        }
                    }
                }
                EditorGUILayout.EndScrollView();
            }
            EditorGUILayout.EndVertical();
        }

        void DrawPaletteItem(CellData cellData)
        {
            bool isSelected = selectedCellType == cellData.cellType;

            GUIStyle style = isSelected ? selectedCellStyle : cellButtonStyle;

            EditorGUILayout.BeginHorizontal(style, GUILayout.Height(50));
            {
                // Color indicator
                Rect colorRect = GUILayoutUtility.GetRect(40, 40);
                EditorGUI.DrawRect(colorRect, cellData.editorColor);

                if (cellData.editorIcon)
                {
                    GUI.DrawTexture(colorRect, cellData.editorIcon.texture, ScaleMode.ScaleToFit);
                }
                else
                {
                    EditorGUI.DrawRect(colorRect, cellData.editorColor);
                }

                // Info section
                EditorGUILayout.BeginVertical();
                {
                    GUILayout.Label(cellData.cellName, EditorStyles.boldLabel);
                    GUILayout.Label($"Type: {cellData.cellType}", EditorStyles.miniLabel);
                    GUILayout.Label($"Walkable: {(cellData.isWalkable ? "✓" : "✗")}", EditorStyles.miniLabel);
                }
                EditorGUILayout.EndVertical();

                GUILayout.FlexibleSpace();

                // Select button
                if (isSelected)
                {
                    GUI.backgroundColor = Color.green;
                    GUILayout.Button("✓ Selected", GUILayout.Width(80), GUILayout.Height(40));
                    GUI.backgroundColor = Color.white;
                }
            }
            EditorGUILayout.EndHorizontal();

            // Handle click on entire item
            Rect itemRect = GUILayoutUtility.GetLastRect();
            if (Event.current.type == EventType.MouseDown && itemRect.Contains(Event.current.mousePosition))
            {
                selectedCellType = cellData.cellType;
                Event.current.Use();
                Repaint();
            }
        }

        // ========================================================================
        // RIGHT PANEL (BRUSH & VIEW SETTINGS)
        // ========================================================================

        void DrawRightPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(250));
            {
                DrawBrushSettings();
                EditorGUILayout.Space(10);
                DrawViewSettings();
                EditorGUILayout.Space(10);
                DrawInfoPanel();
            }
            EditorGUILayout.EndVertical();
        }

        void DrawBrushSettings()
        {
            GUILayout.Label("🖌️ Brush Settings", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical("box");
            {
                GUI.enabled = currentTool == ToolMode.Paint || currentTool == ToolMode.Erase;
                brushSize = EditorGUILayout.IntSlider("Brush Size", brushSize, 1, 10);
                GUI.enabled = true;

                // Visual brush preview
                GUILayout.Space(5);
                GUILayout.Label("Preview:", EditorStyles.miniLabel);

                Rect previewRect = GUILayoutUtility.GetRect(100, 100);
                EditorGUI.DrawRect(previewRect, new Color(0.2f, 0.2f, 0.2f));

                int gridSize = 10;
                float cellSize = previewRect.width / gridSize;
                int center = gridSize / 2;
                int radius = brushSize / 2;

                for (int y = 0; y < gridSize; y++)
                {
                    for (int x = 0; x < gridSize; x++)
                    {
                        Rect cellRect = new Rect(
                            previewRect.x + x * cellSize,
                            previewRect.y + y * cellSize,
                            cellSize - 1,
                            cellSize - 1
                        );

                        float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));

                        if (dist <= radius)
                        {
                            Color brushColor = currentTool == ToolMode.Erase ? Color.red : Color.green;
                            brushColor.a = 0.7f;
                            EditorGUI.DrawRect(cellRect, brushColor);
                        }
                        else
                        {
                            EditorGUI.DrawRect(cellRect, new Color(0.3f, 0.3f, 0.3f));
                        }
                    }
                }
            }
            EditorGUILayout.EndVertical();
        }

        void DrawViewSettings()
        {
            GUILayout.Label("👁️ View Settings", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical("box");
            {
                showGrid = EditorGUILayout.Toggle("Show Grid", showGrid);
                showCoordinates = EditorGUILayout.Toggle("Show Coordinates", showCoordinates);

                GUILayout.Space(5);

                EditorGUILayout.LabelField("Zoom Level", $"{zoomLevel:F2}x");
                zoomLevel = EditorGUILayout.Slider(zoomLevel, 0.3f, 3f);

                gridColor = EditorGUILayout.ColorField("Grid Color", gridColor);

                GUILayout.Space(5);

                if (GUILayout.Button("Reset View", GUILayout.Height(25)))
                {
                    zoomLevel = 1.0f;
                    gridOffset = Vector2.zero;
                }
            }
            EditorGUILayout.EndVertical();
        }

        void DrawInfoPanel()
        {
            GUILayout.Label("ℹ️ Info", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical("box");
            {
                if (mapData != null)
                {
                    EditorGUILayout.LabelField("Map Size:", $"{mapData.width} x {mapData.height}");
                    EditorGUILayout.LabelField("Total Cells:", $"{mapData.width * mapData.height}");

                    if (hoveredCell.x >= 0 && hoveredCell.y >= 0)
                    {
                        EditorGUILayout.LabelField("Hovered Cell:", $"({hoveredCell.x}, {hoveredCell.y})");
                        var cellType = mapData.GetCell(hoveredCell.x, hoveredCell.y);
                        EditorGUILayout.LabelField("Cell Type:", cellType.ToString());
                    }
                }

                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Undo Stack:", $"{undoStack.Count}/{maxUndoSteps}");
                EditorGUILayout.LabelField("Redo Stack:", $"{redoStack.Count}");

                GUILayout.Space(10);

                EditorGUILayout.HelpBox(
                    "💡 Tips:\n" +
                    "• Middle Mouse: Pan view\n" +
                    "• Scroll Wheel: Zoom\n" +
                    "• B: Paint tool\n" +
                    "• E: Erase tool\n" +
                    "• F: Fill tool\n" +
                    "• I: Pick color\n" +
                    "• Ctrl+Z: Undo\n" +
                    "• Ctrl+Y: Redo",
                    MessageType.Info
                );
            }
            EditorGUILayout.EndVertical();
        }

        // ========================================================================
        // GRID VIEW PANEL
        // ========================================================================

        void DrawGridViewPanel()
        {
            EditorGUILayout.BeginVertical();
            {
                GUILayout.Label("🗺️ Map Editor", EditorStyles.boldLabel);

                if (mapData == null)
                {
                    EditorGUILayout.HelpBox("⚠️ No map data loaded. Select a MapGridData asset.", MessageType.Warning);
                    EditorGUILayout.EndVertical();
                    return;
                }

                // Get the rect for grid view 
                Rect viewArea = GUILayoutUtility.GetRect(800,400,
                    GUILayout.MinWidth(400),
                    GUILayout.ExpandWidth(true),
                    GUILayout.MinHeight(400),
                    GUILayout.ExpandHeight(true)
                );

                gridViewRect = viewArea;

                // Background
                EditorGUI.DrawRect(viewArea, new Color(0.15f, 0.15f, 0.15f));

                // Handle input first
                HandleGridInput(viewArea);

                // Draw grid
                DrawGrid(viewArea);

                // Draw cursor preview
                DrawCursorPreview(viewArea);
            }
            EditorGUILayout.EndVertical();
        }

        void DrawGrid(Rect viewRect)
        {
            if (!mapData) return;

            float cellSize = 32 * zoomLevel;

            // Calculate visible range
            int startX = Mathf.Max(0, Mathf.FloorToInt(-gridOffset.x / cellSize));
            int endX = Mathf.Min(mapData.width, Mathf.CeilToInt((viewRect.width - gridOffset.x) / cellSize) + 1);
            int startY = Mathf.Max(0, Mathf.FloorToInt(-gridOffset.y / cellSize));
            int endY = Mathf.Min(mapData.height, Mathf.CeilToInt((viewRect.height - gridOffset.y) / cellSize) + 1);

            Handles.BeginGUI();

            // Draw cells
            for (int y = startY; y < endY; y++)
            {
                for (int x = startX; x < endX; x++)
                {
                    Rect cellRect = new Rect(
                        viewRect.x + x * cellSize + gridOffset.x,
                        viewRect.y + y * cellSize + gridOffset.y,
                        cellSize - 1,
                        cellSize - 1
                    );

                    DrawCell(cellRect, x, y, cellSize);
                }
            }

            // Draw grid lines
            if (showGrid)
            {
                Handles.color = gridColor;

                for (int y = startY; y <= endY; y++)
                {
                    float yPos = viewRect.y + y * cellSize + gridOffset.y;
                    if (yPos >= viewRect.y && yPos <= viewRect.y + viewRect.height)
                    {
                        Handles.DrawLine(
                            new Vector3(viewRect.x, yPos),
                            new Vector3(viewRect.x + viewRect.width, yPos)
                        );
                    }
                }

                for (int x = startX; x <= endX; x++)
                {
                    float xPos = viewRect.x + x * cellSize + gridOffset.x;
                    if (xPos >= viewRect.x && xPos <= viewRect.x + viewRect.width)
                    {
                        Handles.DrawLine(
                            new Vector3(xPos, viewRect.y),
                            new Vector3(xPos, viewRect.y + viewRect.height)
                        );
                    }
                }
            }

            Handles.EndGUI();
        }

        void DrawCell(Rect rect, int x, int y, float cellSize)
        {
            var cellType = mapData.GetCell(x, y);

            // Draw cell background
            if (cellType != CellType.Empty && mapData.cellDatabase != null)
            {
                var cellData = mapData.cellDatabase.GetCellData(cellType);
                if (cellData != null)
                {
                    EditorGUI.DrawRect(rect, cellData.editorColor);

                    // Draw icon if enabled and available
                    if (cellData.editorIcon && cellSize > 16)
                    {
                        Rect iconRect = new Rect(rect.x + 2, rect.y + 2, rect.width - 4, rect.height - 4);
                        GUI.DrawTexture(iconRect, cellData.editorIcon.texture, ScaleMode.ScaleToFit);
                    }
                    else
                    {
                        Rect innerRect = new Rect(
                            rect.x + 3,
                            rect.y + 3,
                            rect.width - 6,
                            rect.height - 6
                        );
                        EditorGUI.DrawRect(innerRect, cellData.editorColor);
                    }
                }
            }
            else
            {
                // Empty cell - checkerboard pattern
                Color col = ((x + y) % 2 == 0) ? new Color(0.25f, 0.25f, 0.25f) : new Color(0.2f, 0.2f, 0.2f);
                EditorGUI.DrawRect(rect, col);
            }

            // Draw coordinates if enabled
            if (showCoordinates && cellSize > 30)
            {
                GUIStyle coordStyle = new GUIStyle(EditorStyles.miniLabel);
                coordStyle.normal.textColor = Color.white;
                coordStyle.fontSize = Mathf.Max(8, Mathf.FloorToInt(cellSize / 4));
                coordStyle.alignment = TextAnchor.MiddleCenter;

                GUI.Label(rect, $"{x},{y}", coordStyle);
            }
        }

        void DrawCursorPreview(Rect viewRect)
        {
            if (hoveredCell.x < 0 || hoveredCell.y < 0) return;
            if (!mapData.IsValidPosition(hoveredCell.x, hoveredCell.y)) return;

            float cellSize = 32 * zoomLevel;
            int radius = brushSize / 2;

            Color previewColor = currentTool switch
            {
                ToolMode.Paint => new Color(0, 1, 0, 0.3f),
                ToolMode.Erase => new Color(1, 0, 0, 0.3f),
                ToolMode.BucketFill => new Color(0, 1, 1, 0.3f),
                ToolMode.Eyedropper => new Color(1, 1, 0, 0.3f),
                _ => new Color(1, 1, 1, 0.3f)
            };

            Handles.BeginGUI();
            Handles.color = previewColor;

            if (currentTool == ToolMode.Paint || currentTool == ToolMode.Erase)
            {
                // Draw brush preview
                for (int dy = -radius; dy <= radius; dy++)
                {
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        int cellX = hoveredCell.x + dx;
                        int cellY = hoveredCell.y + dy;

                        if (!mapData.IsValidPosition(cellX, cellY)) continue;

                        if (Vector2.Distance(new Vector2(dx, dy), Vector2.zero) <= radius)
                        {
                            Rect cellRect = new Rect(
                                viewRect.x + cellX * cellSize + gridOffset.x,
                                viewRect.y + cellY * cellSize + gridOffset.y,
                                cellSize - 1,
                                cellSize - 1
                            );

                            EditorGUI.DrawRect(cellRect, previewColor);
                        }
                    }
                }
            }
            else
            {
                // Single cell preview
                Rect cellRect = new Rect(
                    viewRect.x + hoveredCell.x * cellSize + gridOffset.x,
                    viewRect.y + hoveredCell.y * cellSize + gridOffset.y,
                    cellSize - 1,
                    cellSize - 1
                );

                EditorGUI.DrawRect(cellRect, previewColor);
            }

            Handles.EndGUI();
        }

        void HandleGridInput(Rect rect)
        {
            Event e = Event.current;

            if (!rect.Contains(e.mousePosition))
            {
                hoveredCell = new Vector2Int(-1, -1);
                return;
            }

            float cellSize = 32 * zoomLevel;

            int gridX = Mathf.FloorToInt((e.mousePosition.x - rect.x - gridOffset.x) / cellSize);
            int gridY = Mathf.FloorToInt((e.mousePosition.y - rect.y - gridOffset.y) / cellSize);

            hoveredCell = new Vector2Int(gridX, gridY);

            // Pan view with middle mouse
            if (e.type == EventType.MouseDown && e.button == 2)
            {
                isDraggingView = true;
                e.Use();
            }

            if (e.type == EventType.MouseDrag && e.button == 2)
            {
                gridOffset += e.delta;
                e.Use();
                Repaint();
            }

            if (e.type == EventType.MouseUp && e.button == 2)
            {
                isDraggingView = false;
                e.Use();
            }

            // Zoom with scroll wheel
            if (e.type == EventType.ScrollWheel && !e.alt)
            {
                float oldZoom = zoomLevel;
                zoomLevel -= e.delta.y * 0.05f;
                zoomLevel = Mathf.Clamp(zoomLevel, 0.3f, 3f);

                // Zoom towards mouse position
                Vector2 mouseGridPos = new Vector2(
                    (e.mousePosition.x - rect.x - gridOffset.x) / (cellSize),
                    (e.mousePosition.y - rect.y - gridOffset.y) / (cellSize)
                );

                float newCellSize = 32 * zoomLevel;
                gridOffset.x = e.mousePosition.x - rect.x - mouseGridPos.x * newCellSize;
                gridOffset.y = e.mousePosition.y - rect.y - mouseGridPos.y * newCellSize;

                e.Use();
                Repaint();
            }

            // Paint
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                isPainting = true;
                RecordUndo();
                ApplyTool(gridX, gridY);
                e.Use();
                Repaint();
            }

            if (e.type == EventType.MouseDrag && e.button == 0 && isPainting)
            {
                ApplyTool(gridX, gridY);
                e.Use();
                Repaint();
            }

            if (e.type == EventType.MouseUp && e.button == 0)
            {
                isPainting = false;
                e.Use();
            }

            // Eyedropper
            if (e.type == EventType.MouseDown && e.button == 0 && currentTool == ToolMode.Eyedropper)
            {
                if (mapData.IsValidPosition(gridX, gridY))
                {
                    selectedCellType = mapData.GetCell(gridX, gridY);
                    currentTool = ToolMode.Paint;
                    ShowNotification(new GUIContent($"✓ Picked: {selectedCellType}"));
                }

                e.Use();
            }

            if (e.type == EventType.MouseMove)
            {
                Repaint();
            }
        }

        void ApplyTool(int x, int y)
        {
            if (!mapData.IsValidPosition(x, y)) return;

            switch (currentTool)
            {
                case ToolMode.Paint:
                    PaintCells(x, y);
                    break;
                case ToolMode.Erase:
                    EraseCells(x, y);
                    break;
                case ToolMode.BucketFill:
                    BucketFill(x, y);
                    isPainting = false; // Don't drag fill
                    break;
            }

            EditorUtility.SetDirty(mapData);
        }

        void PaintCells(int centerX, int centerY)
        {
            int radius = brushSize / 2;

            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    int x = centerX + dx;
                    int y = centerY + dy;

                    if (Vector2.Distance(new Vector2(dx, dy), Vector2.zero) <= radius)
                    {
                        mapData.SetCell(x, y, selectedCellType);
                    }
                }
            }
        }

        void EraseCells(int centerX, int centerY)
        {
            int radius = brushSize / 2;

            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    int x = centerX + dx;
                    int y = centerY + dy;

                    if (Vector2.Distance(new Vector2(dx, dy), Vector2.zero) <= radius)
                    {
                        mapData.SetCell(x, y, CellType.Empty);
                    }
                }
            }
        }

        void BucketFill(int startX, int startY)
        {
            var targetType = mapData.GetCell(startX, startY);
            if (targetType == selectedCellType) return;

            var toFill = new Queue<Vector2Int>();
            var filled = new HashSet<Vector2Int>();

            toFill.Enqueue(new Vector2Int(startX, startY));

            int fillCount = 0;
            int maxFill = 10000; // Prevent infinite loop

            while (toFill.Count > 0 && fillCount < maxFill)
            {
                var pos = toFill.Dequeue();

                if (!mapData.IsValidPosition(pos.x, pos.y)) continue;
                if (filled.Contains(pos)) continue;
                if (mapData.GetCell(pos.x, pos.y) != targetType) continue;

                mapData.SetCell(pos.x, pos.y, selectedCellType);
                filled.Add(pos);
                fillCount++;

                toFill.Enqueue(new Vector2Int(pos.x + 1, pos.y));
                toFill.Enqueue(new Vector2Int(pos.x - 1, pos.y));
                toFill.Enqueue(new Vector2Int(pos.x, pos.y + 1));
                toFill.Enqueue(new Vector2Int(pos.x, pos.y - 1));
            }

            if (fillCount >= maxFill)
            {
                Debug.LogWarning("Bucket fill stopped at max limit to prevent freeze");
            }
        }

        // ========================================================================
        // UNDO/REDO SYSTEM
        // ========================================================================

        void RecordUndo()
        {
            if (mapData == null) return;

            var snapshot = new CellType[mapData.width * mapData.height];
            for (int i = 0; i < snapshot.Length; i++)
            {
                int x = i % mapData.width;
                int y = i / mapData.width;
                snapshot[i] = mapData.GetCell(x, y);
            }

            undoStack.Push(snapshot);
            redoStack.Clear();

            while (undoStack.Count > maxUndoSteps)
            {
                var temp = undoStack.ToArray();
                undoStack.Clear();
                for (int i = 1; i < temp.Length; i++)
                {
                    undoStack.Push(temp[i]);
                }
            }
        }

        void PerformUndo()
        {
            if (undoStack.Count == 0 || mapData == null) return;

            var currentState = new CellType[mapData.width * mapData.height];
            for (int i = 0; i < currentState.Length; i++)
            {
                int x = i % mapData.width;
                int y = i / mapData.width;
                currentState[i] = mapData.GetCell(x, y);
            }

            redoStack.Push(currentState);

            var previousState = undoStack.Pop();
            RestoreState(previousState);

            ShowNotification(new GUIContent("↶ Undo"));
        }

        void PerformRedo()
        {
            if (redoStack.Count == 0 || mapData == null) return;

            var currentState = new CellType[mapData.width * mapData.height];
            for (int i = 0; i < currentState.Length; i++)
            {
                int x = i % mapData.width;
                int y = i / mapData.width;
                currentState[i] = mapData.GetCell(x, y);
            }

            undoStack.Push(currentState);

            var nextState = redoStack.Pop();
            RestoreState(nextState);

            ShowNotification(new GUIContent("↷ Redo"));
        }

        void RestoreState(CellType[] state)
        {
            for (int i = 0; i < state.Length; i++)
            {
                int x = i % mapData.width;
                int y = i / mapData.width;
                mapData.SetCell(x, y, state[i]);
            }

            EditorUtility.SetDirty(mapData);
            Repaint();
        }

        void HandleKeyboardShortcuts()
        {
            Event e = Event.current;

            if (e.type == EventType.KeyDown)
            {
                bool used = true;

                if (e.control && e.keyCode == KeyCode.Z)
                    PerformUndo();
                else if (e.control && e.keyCode == KeyCode.Y)
                    PerformRedo();
                else if (e.keyCode == KeyCode.B)
                    currentTool = ToolMode.Paint;
                else if (e.keyCode == KeyCode.E)
                    currentTool = ToolMode.Erase;
                else if (e.keyCode == KeyCode.F)
                    currentTool = ToolMode.BucketFill;
                else if (e.keyCode == KeyCode.I)
                    currentTool = ToolMode.Eyedropper;
                else if (e.keyCode == KeyCode.LeftBracket)
                    brushSize = Mathf.Max(1, brushSize - 1);
                else if (e.keyCode == KeyCode.RightBracket)
                    brushSize = Mathf.Min(10, brushSize + 1);
                else if (e.keyCode == KeyCode.G)
                    showGrid = !showGrid;
                else
                    used = false;

                if (used)
                {
                    e.Use();
                    Repaint();
                }
            }
        }

        // ========================================================================
        // SCENE VIEW INTEGRATION
        // ========================================================================

        void OnSceneGUI(SceneView sceneView)
        {
            if (mapData == null) return;

            // Draw grid overlay in scene view
            Handles.color = new Color(0, 1, 0, 0.2f);

            for (int y = 0; y <= mapData.height; y++)
            {
                var start = mapData.GridToWorld(0, y);
                var end = mapData.GridToWorld(mapData.width, y);
                Handles.DrawLine(start, end);
            }

            for (int x = 0; x <= mapData.width; x++)
            {
                var start = mapData.GridToWorld(x, 0);
                var end = mapData.GridToWorld(x, mapData.height);
                Handles.DrawLine(start, end);
            }
        }

        // ========================================================================
        // UTILITIES
        // ========================================================================

        void CreateNewMapData()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Map Grid Data",
                "NewMapGridData",
                "asset",
                "Create new map grid data asset"
            );

            if (!string.IsNullOrEmpty(path))
            {
                var newMapData = CreateInstance<MapGridData>();
                newMapData.width = 20;
                newMapData.height = 20;
                newMapData.Initialize();
                AssetDatabase.CreateAsset(newMapData, path);
                AssetDatabase.SaveAssets();
                mapData = newMapData;
                ShowNotification(new GUIContent("✓ Map Created!"));
            }
        }
    }
}