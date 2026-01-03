using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text;
using Storia.Diagnostics;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
#endif

namespace Storia.Editor
{
    /// <summary>
    /// Geliştirici günlüğü penceresi.
    /// 
    /// Sorumlulukları:
    /// 1. DevModeManager'dan gemi ve konteyner debug verilerini okuma
    /// 2. Odin Inspector ile zengin UI gösterimi (varsa)
    /// 3. IMGUI fallback (Odin yoksa)
    /// 4. Filtreleme (gemi adı, konteyner ID'si)
    /// 5. Panoya kopyalama
    /// 
    /// Odin varsa: PropertyTree ile otomatik Inspector rendering
    /// Odin yoksa: Manuel IMGUI foldout/list rendering
    /// </summary>
    public sealed class DevLogWindow : EditorWindow
    {
        // ========== Ortak Alanlar (Odin ve IMGUI için) ==========
        /// <summary>DevModeManager referansı (sahneden bulunur)</summary>
        private DevModeManager _manager;

        /// <summary>Son veri yenileme zamanı (EditorApplication.timeSinceStartup)</summary>
        private double _lastRefreshTime;

        /// <summary>Otomatik yenileme aralığı (saniye)</summary>
        private const double RefreshInterval = 0.1;

        /// <summary>Filtre metni (gemi adı, konteyner ID'si)</summary>
        private string _filter = string.Empty;

        /// <summary>Scroll pozisyonu</summary>
        private Vector2 _scrollPos;

#if ODIN_INSPECTOR
        // ========== Odin Inspector Alanları ==========
        /// <summary>Odin PropertyTree için ViewModel wrapper</summary>
        private DevInfoViewModel _vm;

        /// <summary>Odin PropertyTree (otomatik Inspector rendering)</summary>
        private PropertyTree _tree;

        /// <summary>Tree rebuild gerekip gerekmediğini kontrol için son manager referansı</summary>
        private DevModeManager _lastManagerForTree;

        /// <summary>Gemi adı eşleşirse tüm konteynerları göster (filtre davranışı)</summary>
        private bool _showAllContainersWhenShipNameMatches = true;

        /// <summary>Odin UI için deterministik snapshot (IEnumerable cast yok, direkt List)</summary>
        private readonly List<DevModeManager.ShipDebugInfo> _shipSnapshot = new List<DevModeManager.ShipDebugInfo>(128);

        /// <summary>
        /// Assembly reload ve editor quit sırasında PropertyTree'leri dispose et.
        /// Memory leak önleme (Odin PropertyTree native resource kullanır).
        /// </summary>
        [InitializeOnLoadMethod]
        private static void InstallDisposeHooks()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= DisposeAllOpenWindows;
            AssemblyReloadEvents.beforeAssemblyReload += DisposeAllOpenWindows;

            EditorApplication.quitting -= DisposeAllOpenWindows;
            EditorApplication.quitting += DisposeAllOpenWindows;
        }

        /// <summary>
        /// Tüm açık DevLogWindow'ların PropertyTree'lerini dispose et.
        /// </summary>
        private static void DisposeAllOpenWindows()
        {
            DevLogWindow[] windows = Resources.FindObjectsOfTypeAll<DevLogWindow>();
            if (windows == null) return;

            for (int i = 0; i < windows.Length; i++)
            {
                DevLogWindow w = windows[i];
                if (w == null) continue;
                w.DisposeTree();
            }
        }
#else
        // ========== IMGUI Fallback Alanları (Odin yoksa) ==========
        /// <summary>Gemi foldout durumları (shipIndex → expanded)</summary>
        private readonly Dictionary<int, bool> _shipFoldouts = new Dictionary<int, bool>();

        /// <summary>Gemi listesi snapshot (filtrelenmiş)</summary>
        private IReadOnlyList<DevModeManager.ShipDebugInfo> _ships;
        
        /// <summary>Mevcut seed değeri</summary>
        private int _seed;
        
        /// <summary>Toplam gemi sayısı</summary>
        private int _totalShips;
        
        /// <summary>Toplam konteyner sayısı</summary>
        private int _totalContainers;
#endif

        /// <summary>
        /// Window/Storia/Geliştirici Günlüğü menüsünden pencereyi aç.
        /// DevModeManager component'inden de çağrılabilir.
        /// </summary>
        [MenuItem("Window/Storia/Geliştirici Günlüğü")]
        public static void OpenWindow()
        {
            DevLogWindow wnd = GetWindow<DevLogWindow>("Geliştirici Günlüğü");
            wnd.minSize = new Vector2(700, 360);
            wnd.Show();
        }

        /// <summary>
        /// Pencere açıldığında: EditorUpdate callback'i bağla, veri yenile, Odin tree oluştur.
        /// </summary>
        private void OnEnable()
        {
            EditorApplication.update += EditorUpdate;
            RefreshDataAndSnapshots(force: true);

#if ODIN_INSPECTOR
            RebuildOdinTree(force: true);
#endif
        }

        /// <summary>
        /// Pencere kapandığında: Callback'i çöz, Odin tree'yi dispose et.
        /// </summary>
        private void OnDisable()
        {
#if ODIN_INSPECTOR
            DisposeTree();
            _vm = null;
            _lastManagerForTree = null;
#endif
            EditorApplication.update -= EditorUpdate;
        }

        /// <summary>
        /// Pencere destroy edildiğinde: Fail-safe dispose (OnDisable her zaman çağrılmayabilir).
        /// </summary>
        private void OnDestroy()
        {
#if ODIN_INSPECTOR
            // EditorWindow her zaman OnDisable çağırmayabilir; güvenli tarafta kal.
            DisposeTree();
#endif
        }

        /// <summary>
        /// EditorApplication.update callback'i.
        /// RefreshInterval aralıklarla veri yenile ve UI'ı repaint et.
        /// </summary>
        private void EditorUpdate()
        {
            if (EditorApplication.timeSinceStartup - _lastRefreshTime < RefreshInterval)
                return;

            RefreshDataAndSnapshots(force: false);

#if ODIN_INSPECTOR
            if (_tree != null) _tree.UpdateTree();
#endif
            Repaint();
        }

        /// <summary>
        /// DevModeManager'dan veri yenile ve filtrelenmiş snapshot oluştur.
        /// Odin varsa: ViewModel + snapshot doldur, tree rebuild kontrol et.
        /// Odin yoksa: IMGUI için basit snapshot al.
        /// </summary>
        /// <param name="force">True ise koşulsuz yenile (scene reload, manual refresh)</param>
        private void RefreshDataAndSnapshots(bool force)
        {
            _manager = DevModeManager.Instance;
            _lastRefreshTime = EditorApplication.timeSinceStartup;

#if ODIN_INSPECTOR
            if (_vm == null) _vm = new DevInfoViewModel();

            _vm.SetManager(_manager);
            _vm.SetFilter(_filter);
            _vm.SetContainerPolicy(_showAllContainersWhenShipNameMatches);

            _shipSnapshot.Clear();

            if (_manager != null && _manager.IsEnabled)
            {
                IReadOnlyList<DevModeManager.ShipDebugInfo> ships = _manager.ShipLogReadonly;
                if (ships != null && ships.Count > 0)
                {
                    string filterTrim = string.IsNullOrWhiteSpace(_filter) ? null : _filter.Trim();

                    for (int i = 0; i < ships.Count; i++)
                    {
                        DevModeManager.ShipDebugInfo ship = ships[i];

                        if (filterTrim == null)
                        {
                            _shipSnapshot.Add(ship);
                            continue;
                        }

                        bool shipNameMatch =
                            ship.shipName != null &&
                            ship.shipName.IndexOf(filterTrim, System.StringComparison.OrdinalIgnoreCase) >= 0;

                        if (shipNameMatch)
                        {
                            _shipSnapshot.Add(ship);
                            continue;
                        }

                        bool anyContainerMatch = false;
                        if (ship.containers != null)
                        {
                            for (int ci = 0; ci < ship.containers.Count; ci++)
                            {
                                DevModeManager.ContainerDebugInfo c = ship.containers[ci];
                                if (c.containerId != null &&
                                    c.containerId.IndexOf(filterTrim, System.StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    anyContainerMatch = true;
                                    break;
                                }
                            }
                        }

                        if (anyContainerMatch) _shipSnapshot.Add(ship);
                    }
                }
            }

            _vm.SetShipSnapshot(_shipSnapshot);

            // Manager instance değiştiyse tree rebuild gerekir (scene reload vs.)
            RebuildOdinTree(force: force);
#else
        if (_manager == null)
        {
            _ships = null;
            _seed = 0;
            _totalShips = 0;
            _totalContainers = 0;
            return;
        }

        _seed = _manager.CurrentSeedValue;
        _ships = _manager.ShipLogReadonly;
        _totalShips = _ships != null ? _ships.Count : 0;
        _totalContainers = _manager.TotalContainersCount;
#endif
        }

#if ODIN_INSPECTOR
        /// <summary>
        /// Odin PropertyTree'yi yeniden oluştur.
        /// Manager değiştiyse (scene reload) mutlaka rebuild gerekir.
        /// </summary>
        /// <param name="force">True ise koşulsuz rebuild</param>
        private void RebuildOdinTree(bool force)
        {
            // Manager aynıysa ve tree varsa rebuild gereksiz
            if (!force && _tree != null && ReferenceEquals(_manager, _lastManagerForTree))
                return;

            DisposeTree();

            _lastManagerForTree = _manager;

            if (_vm == null) _vm = new DevInfoViewModel();
            _tree = PropertyTree.Create(_vm);
        }

        /// <summary>
        /// PropertyTree'yi dispose et (memory leak önleme).
        /// Idempotent - null kontrolü var.
        /// </summary>
        private void DisposeTree()
        {
            if (_tree == null) return;
            _tree.Dispose();
            _tree = null;
        }
#endif

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.Space();

            if (_manager == null)
            {
                EditorGUILayout.HelpBox("DevModeManager bulunamadı (sahnede yok veya kapalı).", MessageType.Info);

                if (GUILayout.Button("Yenile", GUILayout.Width(120)))
                {
                    RefreshDataAndSnapshots(force: true);
#if ODIN_INSPECTOR
                    RebuildOdinTree(force: true);
#endif
                }

                EditorGUILayout.EndVertical();
                return;
            }

            // ========== Üst Toolbar ==========
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Yenile", GUILayout.Width(90)))
            {
                RefreshDataAndSnapshots(force: true);
#if ODIN_INSPECTOR
                RebuildOdinTree(force: true);
#endif
            }

            if (GUILayout.Button("Panoya Kopyala", GUILayout.Width(160)))
            {
                CopyToClipboard();
            }

            GUILayout.FlexibleSpace();

            EditorGUILayout.LabelField("Filtre", GUILayout.Width(40));
            string newFilter = EditorGUILayout.TextField(_filter, GUILayout.Width(260));

            EditorGUILayout.EndHorizontal();

            if (!string.Equals(newFilter, _filter))
            {
                _filter = newFilter;
                RefreshDataAndSnapshots(force: false);
            }

#if ODIN_INSPECTOR
            EditorGUILayout.Space(6);

            bool newPolicy = EditorGUILayout.ToggleLeft(
                "Gemi adı eşleşirse tüm konteyner'ları göster",
                _showAllContainersWhenShipNameMatches);

            if (newPolicy != _showAllContainersWhenShipNameMatches)
            {
                _showAllContainersWhenShipNameMatches = newPolicy;
                if (_vm != null) _vm.SetContainerPolicy(_showAllContainersWhenShipNameMatches);
            }

            EditorGUILayout.Space(6);

            if (!_manager.IsEnabled)
            {
                EditorGUILayout.HelpBox("Dev Mode kapalı. Geliştirici bilgileri içeriği gizleniyor.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            if (_tree == null)
            {
                RebuildOdinTree(force: true);
            }

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            _tree.Draw(false);
            EditorGUILayout.EndScrollView();
#else
        // Odin yokken IMGUI fallback
        EditorGUILayout.Space();

        if (!_manager.IsEnabled)
        {
            EditorGUILayout.HelpBox("Dev Mode kapalı. Geliştirici bilgileri içeriği gizleniyor.", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        EditorGUILayout.LabelField($"Tohum Değeri: {_seed}", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Toplam Gemi: {_totalShips}    Toplam Konteyner: {_totalContainers}");

        EditorGUILayout.Space();
        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

        string filterTrim = string.IsNullOrWhiteSpace(_filter) ? null : _filter.Trim();

        if (_ships != null)
        {
            for (int i = 0; i < _ships.Count; i++)
            {
                DevModeManager.ShipDebugInfo ship = _ships[i];

                bool shipMatches;
                bool shipNameMatch = false;

                if (filterTrim == null)
                {
                    shipMatches = true;
                }
                else
                {
                    if (ship.shipName != null &&
                        ship.shipName.IndexOf(filterTrim, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        shipNameMatch = true;
                        shipMatches = true;
                    }
                    else
                    {
                        shipMatches = false;
                        if (ship.containers != null)
                        {
                            for (int ci = 0; ci < ship.containers.Count; ci++)
                            {
                                DevModeManager.ContainerDebugInfo cc = ship.containers[ci];
                                if (cc.containerId != null &&
                                    cc.containerId.IndexOf(filterTrim, System.StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    shipMatches = true;
                                    break;
                                }
                            }
                        }
                    }
                }

                if (!shipMatches) continue;

                if (!_shipFoldouts.TryGetValue(ship.shipIndex, out bool expanded)) expanded = false;

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();

                expanded = EditorGUILayout.Foldout(expanded, $"{ship.shipIndex} - {ship.shipName} ({ship.shipId})", true);
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField($"Origin: {ship.originPort}", GUILayout.Width(220));
                EditorGUILayout.LabelField($"Cnt: {ship.containerCount}", GUILayout.Width(60));
                EditorGUILayout.LabelField($"Voyage: {ship.voyageHours}", GUILayout.Width(90));

                EditorGUILayout.EndHorizontal();

                _shipFoldouts[ship.shipIndex] = expanded;

                if (expanded)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label("idx", GUILayout.Width(30));
                    GUILayout.Label("gemi", GUILayout.Width(40));
                    GUILayout.Label("konteynerID", GUILayout.Width(140));
                    GUILayout.Label("köken", GUILayout.Width(120));
                    GUILayout.Label("yük", GUILayout.Width(120));
                    GUILayout.Label("manifest", GUILayout.Width(120));
                    GUILayout.Label("etiket", GUILayout.Width(120));
                    GUILayout.Label("çelişki", GUILayout.Width(100));
                    EditorGUILayout.EndHorizontal();

                    if (ship.containers != null)
                    {
                        foreach (DevModeManager.ContainerDebugInfo c in ship.containers)
                        {
                            if (filterTrim != null)
                            {
                                bool containerMatches =
                                    c.containerId != null &&
                                    c.containerId.IndexOf(filterTrim, System.StringComparison.OrdinalIgnoreCase) >= 0;

                                if (!shipNameMatch && !containerMatches) continue;
                            }

                            EditorGUILayout.BeginHorizontal();
                            GUILayout.Label(c.index.ToString(), GUILayout.Width(30));
                            GUILayout.Label(c.shipIndex.ToString(), GUILayout.Width(40));
                            GUILayout.Label(c.containerId, GUILayout.Width(140));
                            GUILayout.Label(c.trueOrigin, GUILayout.Width(120));
                            GUILayout.Label(c.trueCargo, GUILayout.Width(120));
                            GUILayout.Label(c.manifestShown.ToString(), GUILayout.Width(120));
                            GUILayout.Label(c.labelShown.ToString(), GUILayout.Width(120));
                            GUILayout.Label(c.conflictType.ToString(), GUILayout.Width(100));
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }
        }

        EditorGUILayout.EndScrollView();
#endif

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Debug log'u panoya kopyala (text formatında).
        /// Odin varsa: ViewModel'den formatlanmış text al.
        /// Odin yoksa: Boş string kopyala.
        /// </summary>
        private void CopyToClipboard()
        {
#if ODIN_INSPECTOR
            if (_vm != null && _vm.Manager != null)
            {
                EditorGUIUtility.systemCopyBuffer = _vm.BuildClipboardText();
                return;
            }
#endif
            EditorGUIUtility.systemCopyBuffer = string.Empty;
        }

#if ODIN_INSPECTOR
        /// <summary>
        /// Odin Inspector için ViewModel wrapper.
        /// PropertyTree.Create() için gerekli (Odin serialize edebilsin diye).
        /// 
        /// Sorumlulukları:
        /// 1. DevModeManager verilerini Odin-friendly formatta sunmak
        /// 2. Filtreleme ve görünürlük mantığı
        /// 3. Clipboard text oluşturma
        /// </summary>
        private sealed class DevInfoViewModel
        {
            /// <summary>DevModeManager referansı</summary>
            private DevModeManager _manager;

            /// <summary>Filtre metni (trim'lenmiş, null = filtre yok)</summary>
            private string _filterTrim;

            /// <summary>Gemi adı eşleşirse tüm konteynerları göster (filtre davranışı)</summary>
            private bool _showAllContainersWhenShipNameMatches = true;

            /// <summary>Filtrelenmiş gemi snapshot'u (Odin liste olarak gösterir)</summary>
            private List<DevModeManager.ShipDebugInfo> _ships;

            /// <summary>Manager'a erişim (clipboard text için)</summary>
            public DevModeManager Manager => _manager;

            /// <summary>Manager'ı ayarla</summary>
            public void SetManager(DevModeManager manager) => _manager = manager;

            /// <summary>Filtre metnini ayarla (trim yapılır, boşsa null)</summary>
            public void SetFilter(string filter)
            {
                _filterTrim = string.IsNullOrWhiteSpace(filter) ? null : filter.Trim();
            }

            /// <summary>Konteyner görünürlük politikasını ayarla</summary>
            public void SetContainerPolicy(bool showAllWhenShipNameMatches)
            {
                _showAllContainersWhenShipNameMatches = showAllWhenShipNameMatches;
            }

            /// <summary>Filtrelenmiş gemi snapshot'unu ayarla</summary>
            public void SetShipSnapshot(List<DevModeManager.ShipDebugInfo> ships)
            {
                _ships = ships;
            }

            /// <summary>Dev info gösterilecek mi? (Manager var ve dev mode açık)</summary>
            private bool DevInfoVisible => _manager != null && _manager.IsEnabled;

            // ========== Odin Inspector Properties ==========
            /// <summary>Mevcut tohum değeri (seed)</summary>
            [FoldoutGroup("Geliştirici Bilgileri", expanded: true)]
            [ShowIf(nameof(DevInfoVisible))]
            [ShowInInspector, ReadOnly]
            private string TohumDegeri => _manager != null ? $"Tohum: {_manager.CurrentSeedValue}" : "Tohum: -";

            /// <summary>Toplam gemi sayısı</summary>
            [FoldoutGroup("Geliştirici Bilgileri")]
            [ShowIf(nameof(DevInfoVisible))]
            [ShowInInspector, ReadOnly]
            private int ToplamGemi => _manager != null ? _manager.TotalShipsCount : 0;

            /// <summary>Toplam spawn edilen konteyner sayısı</summary>
            [FoldoutGroup("Geliştirici Bilgileri")]
            [ShowIf(nameof(DevInfoVisible))]
            [ShowInInspector, ReadOnly]
            private int ToplamKonteyner => _manager != null ? _manager.TotalContainersCount : 0;

            /// <summary>Gemi ve konteyner log'u (filtrelenmiş)</summary>
            [FoldoutGroup("Geliştirici Bilgileri/Gemiler ve Konteynerler", expanded: true)]
            [ShowIf(nameof(DevInfoVisible))]
            [ShowInInspector]
            [ListDrawerSettings(
                ShowFoldout = true,
                DefaultExpandedState = true,
                DraggableItems = false,
                HideAddButton = true,
                HideRemoveButton = true,
                IsReadOnly = true)]
            private List<DevModeManager.ShipDebugInfo> GemiGunlugu => _ships;

            /// <summary>
            /// Panoya kopyalanacak text formatında log oluştur.
            /// Filtre uygulanır, gemi + konteyner bilgileri tab-separated.
            /// </summary>
            public string BuildClipboardText()
            {
                if (_manager == null) return string.Empty;

                var sb = new StringBuilder();
                sb.AppendLine($"Tohum: {_manager.CurrentSeedValue}");
                sb.AppendLine($"Toplam Gemi: {_manager.TotalShipsCount}");
                sb.AppendLine($"Toplam Konteyner: {_manager.TotalContainersCount}");
                sb.AppendLine();

                IReadOnlyList<DevModeManager.ShipDebugInfo> ships = _manager.ShipLogReadonly;
                if (ships == null) return sb.ToString();

                for (int i = 0; i < ships.Count; i++)
                {
                    DevModeManager.ShipDebugInfo ship = ships[i];

                    bool shipMatches;
                    bool shipNameMatch = false;

                    if (_filterTrim == null)
                    {
                        shipMatches = true;
                    }
                    else
                    {
                        if (ship.shipName != null &&
                            ship.shipName.IndexOf(_filterTrim, System.StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            shipNameMatch = true;
                            shipMatches = true;
                        }
                        else
                        {
                            shipMatches = false;
                            if (ship.containers != null)
                            {
                                for (int ci = 0; ci < ship.containers.Count; ci++)
                                {
                                    DevModeManager.ContainerDebugInfo cc = ship.containers[ci];
                                    if (cc.containerId != null &&
                                        cc.containerId.IndexOf(_filterTrim, System.StringComparison.OrdinalIgnoreCase) >= 0)
                                    {
                                        shipMatches = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    if (!shipMatches) continue;

                    sb.AppendLine(
                        $"Gemi {ship.shipIndex}: {ship.shipName} ({ship.shipId}) köken:{ship.originPort} konteyner:{ship.containerCount} yolculuk:{ship.voyageHours}sa");

                    if (ship.containers != null)
                    {
                        for (int ci = 0; ci < ship.containers.Count; ci++)
                        {
                            DevModeManager.ContainerDebugInfo c = ship.containers[ci];

                            if (_filterTrim != null)
                            {
                                bool containerMatches =
                                    c.containerId != null &&
                                    c.containerId.IndexOf(_filterTrim, System.StringComparison.OrdinalIgnoreCase) >= 0;

                                if (_showAllContainersWhenShipNameMatches)
                                {
                                    if (!shipNameMatch && !containerMatches) continue;
                                }
                                else
                                {
                                    if (!containerMatches) continue;
                                }
                            }

                            sb.AppendLine(
                                $"\t{c.index}\t{c.shipIndex}\t{c.containerId}\t{c.trueOrigin}\t{c.trueCargo}\t{c.manifestShown}\t{c.labelShown}\t{c.conflictType}");
                        }
                    }
                }

                return sb.ToString();
            }
#endif
        }
    }
}
