using UnityEngine;
using UnityEngine.SceneManagement;
using Storia.Core.Spawning;
using Storia.Core.Placement;
using Storia.Core.Ship;
using Storia.Interaction;
using System.Collections.Generic;
using Storia.Data.Generated;
using Storia.Rules.Runtime;
using Storia.Managers.Decision;
using Storia.Managers.Deck;
using Storia.Core.Initialization;
using Storia.Core.Container;
using Storia.UI.Coordination;
using Storia.Core.GameFlow;
using Storia.Generation;
using Storia.Diagnostics;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

/// <summary>
/// Day 01 prototip ana kontrolörü - oyun akışını koordinatörler aracılığıyla düzenler.
/// </summary>
public sealed class Day01PrototypeController : MonoBehaviour
{
    [Header("Prosedürel Oluşturma Yapılandırması")]
    [SerializeField] private GenerationConfig _generationConfig;
    [SerializeField] private PoolsConfig _poolsConfig;
    [SerializeField] private DaySeedConfig _daySeedConfig;

    [Header("Sahne Referansları")]
    [SerializeField] private SceneReferencesProvider _sceneReferences;

    // Kolay erişim araçları
    private DayTimer Timer => _sceneReferences?.Timer;
    private ContainerSpawner ContainerSpawner => _sceneReferences?.ContainerSpawner;
    private InteractionPoint CraneInteractionPoint => _sceneReferences?.CraneInteractionPoint;
    private DevModeManager DevModeManager => _sceneReferences?.DevModeManager;

    // UI Koordinasyonu
    private GameUICoordinator _uiCoordinator;

    // Oyun Akışı Durum Makinesi
    private GameFlowStateMachine _stateMachine;
    private StateTransitionHandler _stateTransitionHandler;

    // Başlatma sistemi
    private GameInitializer _gameInitializer;
    private InitializationResult _initResult;
    private Dictionary<int, PlacementZone> _zoneMap;

    // Yöneticiler (InitializationResult'tan)
    private DecisionManager _decisionManager;
    private DeckManager _deckManager;
    private ContainerPresentationManager _presentationManager;
    private IShipService _shipService;

    // Gemi Sistemi Koordinatörleri
    private ShipLifecycleCoordinator _shipLifecycleCoordinator;
    private ShipContainerSpawner _shipContainerSpawner;
    private ShipDecisionHandler _shipDecisionHandler;

    // Konteyner Sistemi Koordinatörleri
    private ContainerDecisionHandler _containerDecisionHandler;
    private ContainerPlacementCoordinator _containerPlacementCoordinator;
    private ContainerLifecycleManager _containerLifecycleManager;

    // Üretilen veriler (InitializationResult'tan)
    private List<ContainerData> _generatedContainers;
    private List<ShipData> _generatedShips;
    private CompositeRule _taskRule;
    private PlacementRuleData _placementRule;

    // Çalışma zamanı durumu
    private int _lastRunSeed;
    private DeterministicRng _rng;

    // Gemi durumu
    private ShipInstance _currentShip;
    private int _globalContainerSequence = 0;  // Tüm gemiler arasında konteyner sırası takibi

    // Olay yaşam döngüsü takibi
    private bool _eventsBound = false;

    private void Awake()
    {
        // Sahne referanslarının atanıp atanmadığını kontrol et
        if (_sceneReferences == null)
        {
            UnityEngine.Debug.LogError("[Day01PrototypeController] HATA: SceneReferencesProvider atanmamış!");
            return;
        }

        // Oyun başlatıcısını oluştur ve UI'ı hazırla
        _gameInitializer = new GameInitializer();
        InitializeUI();
    }

    private void OnDisable()
    {
        UnbindEvents();
    }

    private void OnDestroy()
    {
        // Fail-safe unbind (idempotent)
        UnbindEvents();
    }

    private void Start()
    {
        StartDay();
    }

    private void Update()
    {
        UpdateTimer();
    }

    private void InitializeUI()
    {
        // UI koordinatörünü oluştur ve sahne referansları ile başlat
        _uiCoordinator = new GameUICoordinator();
        _uiCoordinator.Initialize(_sceneReferences);

        // İş istasyonu butonlarını ilgili metodlara bağla
        _sceneReferences?.Workstation?.BindDecisionButtons(OnAcceptPressed, OnRejectPressed);
        _sceneReferences?.Workstation?.BindPlacementButtons(OnZonePicked);
        _sceneReferences?.Workstation?.BindShipDecisionButtons(OnShipAccepted, OnShipRejected);

        // Başlangıçta yerleştirme panelini gizle ve gemi bilgisini temizle
        _uiCoordinator?.Workstation?.ShowPlacementPanel(false);
        _uiCoordinator?.Workstation?.HideShipInfo();
    }

    private void UpdateTimer()
    {
        _uiCoordinator?.UpdateTimer(Time.deltaTime);
    }

    private void StartDay()
    {
        // Konteyner sıra numarasını ve mevcut gemiyi sıfırla
        _globalContainerSequence = 0;
        _currentShip = null;

        // Oyun akışı durum makinesini başlat
        _stateMachine = new GameFlowStateMachine();
        _stateTransitionHandler = new StateTransitionHandler(_stateMachine);

        // Oyunu başlat: prosedürel içerik üret, yöneticileri oluştur
        _initResult = _gameInitializer.InitializeGame(
            _daySeedConfig,
            _generationConfig,
            _poolsConfig,
            _sceneReferences
        );

        // Üretilen verileri ve yöneticileri başlatma sonucundan al
        _generatedContainers = _initResult.GeneratedContainers;
        _generatedShips = _initResult.GeneratedShips;
        _taskRule = _initResult.TaskRule;
        _placementRule = _initResult.PlacementRule;
        _decisionManager = _initResult.DecisionManager;
        _deckManager = _initResult.DeckManager;
        _presentationManager = _initResult.PresentationManager;
        _lastRunSeed = _initResult.RunSeed;
        _rng = _initResult.Rng;

        // Yerleştirme bölgeleri için ID→Zone haritası oluştur
        _zoneMap = ManagerFactory.CreateZoneMap(_sceneReferences.PlacementZones);

        // ShipManager singleton örneğini al (koordinatörler bu servise bağımlı)
        // NOT: ShipManager.Instance'a erişilen tek yer burasıdır
        _shipService = ShipManager.Instance;
        if (_shipService != null)
        {
            // Günün gemilerini başlat (üretilen gemi verilerini yükle)
            _shipService.InitializeShipsForDay(_generatedShips);
        }

        // Gemi ve konteyner sistemleri için koordinatörleri başlat
        InitializeShipSystemCoordinators();
        InitializeContainerSystemCoordinators();

        // Vinç etkileşim noktasını gemi servisi ile bağla
        if (CraneInteractionPoint != null)
        {
            CraneInteractionPoint.Initialize(_shipService);
        }

        // Koordinatörler hazır olduğuna göre tüm olayları bağla
        BindEvents();

        // Gün zamanlayıcısını sıfırla ve UI'ı hazırla
        Timer?.ResetDay();
        SetupUIForNewDay();

        // Başlatma tamamlandı, ilk gemiyi bekleme durumuna geç
        _stateMachine?.TransitionTo(Storia.Core.GameFlow.GameState.AwaitingShip);

        // İlk gemiyi getir
        GetNextShip();
    }

    private void InitializeShipSystemCoordinators()
    {
        // Gemi konteynerlerini spawn etmek için koordinatör
        _shipContainerSpawner = new ShipContainerSpawner(
            _deckManager,
            _presentationManager,
            ContainerSpawner
        );

        // Gemi kabul/red kararları için UI mantığı koordinatörü
        _shipDecisionHandler = new ShipDecisionHandler(
            _sceneReferences.Workstation,
            CraneInteractionPoint
        );

        // Gemi yaşam döngüsü üst düzey koordinatörü (varış, işleme, ayrılış)
        _shipLifecycleCoordinator = new ShipLifecycleCoordinator(
            _shipService,
            _shipContainerSpawner,
            _shipDecisionHandler,
            _sceneReferences.Workstation,
            _decisionManager,
            DevModeManager
        );
    }

    private void InitializeContainerSystemCoordinators()
    {
        // Konteyner kabul/red kararlarını işleyen koordinatör
        _containerDecisionHandler = new ContainerDecisionHandler(
            _decisionManager,
            _sceneReferences.Workstation
        );

        // Kabul edilen konteynerlerin bölgelere yerleştirilmesini yöneten koordinatör
        _containerPlacementCoordinator = new ContainerPlacementCoordinator(
            _decisionManager,
            ContainerSpawner,
            _zoneMap,
            _sceneReferences.Workstation
        );

        // Konteyner yaşam döngüsünü yöneten koordinatör (sıralama, gösterme, temizleme)
        _containerLifecycleManager = new ContainerLifecycleManager(
            _shipService,
            ContainerSpawner,
            _sceneReferences.Workstation,
            _stateMachine,
            _stateTransitionHandler
        );
    }

    /// <summary>
    /// Tüm olay aboneliklerini bağla (idempotent).
    /// Koordinatör başlatıldıktan sonra StartDay'den bir kez çağrılır.
    /// </summary>
    private void BindEvents()
    {
        // İdempotent kontrol: olaylar zaten bağlıysa tekrar bağlama
        if (_eventsBound)
        {
            UnityEngine.Debug.LogWarning("[Day01PrototypeController] UYARI: Olaylar zaten bağlı, işlem atlanıyor.");
            return;
        }

        // Gerekli bağımlılıkların null olmadığından emin ol
        if (_shipService == null || Timer == null)
        {
            UnityEngine.Debug.LogWarning("[Day01PrototypeController] UYARI: Olaylar bağlanamadı - ShipService veya Timer null!");
            return;
        }

        // ShipService olayları
        _shipService.OnShipArrived += HandleShipArrived;
        _shipService.OnShipMovementCompleted += HandleShipMovementCompleted;

        // Timer olayları
        Timer.OnDayTimeUp += HandleDayTimeUp;

        // Koordinatör olayları
        if (_shipLifecycleCoordinator != null)
        {
            _shipLifecycleCoordinator.OnReadyForNextContainer += HandleReadyForNextContainer;
            _shipLifecycleCoordinator.OnShipCompleted += HandleShipCompleted;
        }

        if (_containerDecisionHandler != null)
        {
            _containerDecisionHandler.OnPlacementRequested += HandlePlacementRequested;
            _containerDecisionHandler.OnContainerProcessed += HandleContainerProcessed;
        }

        if (_containerPlacementCoordinator != null)
        {
            _containerPlacementCoordinator.OnPlacementCompleted += HandlePlacementCompleted;
        }

        if (_containerLifecycleManager != null)
        {
            _containerLifecycleManager.OnNoMoreContainers += CompleteCurrentShip;
        }

        _eventsBound = true;
        UnityEngine.Debug.Log("[Day01PrototypeController] Tüm olaylar başarıyla bağlandı.");
    }

    /// <summary>
    /// Tüm olay aboneliklerini çöz (idempotent).
    /// OnDisable ve OnDestroy'den çağrılır.
    /// </summary>
    private void UnbindEvents()
    {
        if (!_eventsBound)
            return;

        // ShipService events
        if (_shipService != null)
        {
            _shipService.OnShipArrived -= HandleShipArrived;
            _shipService.OnShipMovementCompleted -= HandleShipMovementCompleted;
        }

        // Timer events
        if (Timer != null)
        {
            Timer.OnDayTimeUp -= HandleDayTimeUp;
        }

        // Coordinator events
        if (_shipLifecycleCoordinator != null)
        {
            _shipLifecycleCoordinator.OnReadyForNextContainer -= HandleReadyForNextContainer;
            _shipLifecycleCoordinator.OnShipCompleted -= HandleShipCompleted;
        }

        if (_containerDecisionHandler != null)
        {
            _containerDecisionHandler.OnPlacementRequested -= HandlePlacementRequested;
            _containerDecisionHandler.OnContainerProcessed -= HandleContainerProcessed;
        }

        if (_containerPlacementCoordinator != null)
        {
            _containerPlacementCoordinator.OnPlacementCompleted -= HandlePlacementCompleted;
        }

        if (_containerLifecycleManager != null)
        {
            _containerLifecycleManager.OnNoMoreContainers -= CompleteCurrentShip;
        }

        _eventsBound = false;
        UnityEngine.Debug.Log("[Day01PrototypeController] Tüm olaylar çözüldü.");
    }

    private void SetupUIForNewDay()
    {
        if (DevModeManager != null)
            DevModeManager.OnDayStarted(_lastRunSeed);

#if UNITY_EDITOR
        LogDeterminismHashes();
#endif

        UpdateUI();
        DisplayTaskInstructions();
    }

    private void LogDeterminismHashes()
    {
        // Deterministik üretim doğrulaması için hash değerlerini hesapla
        string containerHash = Storia.Utilities.Validation.DeterminismValidator.ComputeContainerHash(_generatedContainers);
        string shipHash = Storia.Utilities.Validation.DeterminismValidator.ComputeShipHash(_generatedShips);
        string targetHash = Storia.Utilities.Validation.DeterminismValidator.ComputeTargetContainerHash(_generatedContainers);
        string placementHash = Storia.Utilities.Validation.DeterminismValidator.ComputePlacementHash(_generatedContainers);

        // Hash değerlerini logla (aynı seed = aynı hash olmalı)
        UnityEngine.Debug.Log($"[DETERMİNİZM DOĞRULAMA]\n" +
                              $"Seed: {_lastRunSeed}\n" +
                              $"Konteyner Hash: {containerHash}\n" +
                              $"Gemi Hash: {shipHash}\n" +
                              $"Hedef Hash: {targetHash}\n" +
                              $"Yerleştirme Hash: {placementHash}");
    }

    private void UpdateUI()
    {
        _uiCoordinator?.Workstation?.ShowPlacementPanel(false);
    }

    private void DisplayTaskInstructions()
    {
        string taskText = _taskRule != null
            ? $"Bugün KABUL edilecek hedef konteynerler:\n{_taskRule.GetShortDescription()}\n\nDiğer tüm konteynerleri REDDET."
            : "Görev: (Task rule missing)";

        string placementText = _placementRule != null
            ? _placementRule.BuildUiInstruction()
            : "Yerleştirme Talimatı: (Placement rule missing)";

        _uiCoordinator?.DisplayTaskInstructions(taskText, placementText);
    }

    private void OnAcceptPressed()
    {
        // Shutdown gating - gün sonu sırasında/sonrasında eylemleri önlemek
        if (_stateMachine != null && _stateMachine.IsDayEnded())
            return;

        if (_uiCoordinator?.Workstation?.IsShipPanelActive ?? false)
            return;

        if (Timer == null || Timer.IsFinished || !_stateMachine.IsInState(GameState.ContainerEvaluation))
            return;

        if (_containerLifecycleManager != null)
        {
            _containerDecisionHandler?.HandleAccept(
                _containerLifecycleManager.CurrentContainerData,
                _containerLifecycleManager.CurrentPresentation
            );
        }
    }

    private void OnRejectPressed()
    {
        // Shutdown gating - gün sonu sırasında/sonrasında eylemleri önlemek
        if (_stateMachine != null && _stateMachine.IsDayEnded())
            return;

        if (_uiCoordinator?.Workstation?.IsShipPanelActive ?? false)
            return;

        if (Timer == null || Timer.IsFinished || !_stateMachine.IsInState(GameState.ContainerEvaluation))
            return;

        if (_containerLifecycleManager != null)
        {
            _containerDecisionHandler?.HandleReject(
                _containerLifecycleManager.CurrentContainerData,
                _containerLifecycleManager.CurrentPresentation
            );
        }
    }

    private void OnZonePicked(int zoneId)
    {
        // Shutdown gating - gün sonu sırasında/sonrasında eylemleri önlemek
        if (_stateMachine != null && _stateMachine.IsDayEnded())
            return;

        if (Timer == null || Timer.IsFinished || !_stateMachine.IsInState(GameState.PlacementSelection))
            return;

        if (_containerLifecycleManager != null)
        {
            _containerPlacementCoordinator?.PlaceContainer(
                _containerLifecycleManager.CurrentContainerData,
                _containerLifecycleManager.CurrentPresentation,
                zoneId
            );
        }
    }

    private void PrepareEndDayStats()
    {
        // Gün sonu istatistiklerini hazırla (karar sayıları, doğruluk oranları)
        PrototypeStats stats = new PrototypeStats
        {
            seed = _lastRunSeed,
            total = _decisionManager.TotalDecisions,
            correct = _decisionManager.CorrectDecisions,
            wrong = _decisionManager.WrongDecisions,
            placementCorrect = _decisionManager.CorrectPlacements,
            placementWrong = _decisionManager.WrongPlacements,
            decisionLogCount = _decisionManager.GetDecisionLogCount()
        };

        // Detaylı karar logunu oluştur ve UI'da göster
        string logText = _decisionManager.BuildEndOfDayLogText(_lastRunSeed);
        _uiCoordinator?.ShowEndOfDayResults(stats, logText);
    }

    #region Olay İşleyicileri

    // Gemi Yaşam Döngüsü Olayları
    private void HandleReadyForNextContainer()
    {
        _containerLifecycleManager?.NextContainer();
    }

    private void HandleShipCompleted()
    {
        // Koordinatör bildirimi alındı, şuanlık işlem gerekmez
    }

    // Konteyner Karar Olayları
    private void HandlePlacementRequested()
    {
        _stateTransitionHandler?.HandleContainerAccepted();
    }

    private void HandleContainerProcessed()
    {
        _containerLifecycleManager?.ReturnContainerToCargo();
        _containerLifecycleManager?.NextContainer();
    }

    // Konteyner Yerleştirme Olayları
    private void HandlePlacementCompleted()
    {
        _containerLifecycleManager?.RemoveContainerFromCargo();
        _containerLifecycleManager?.NextContainer();
    }

    #endregion

    #region Gemi Sistemi

    private void GetNextShip()
    {
        // Zaman dolmuşsa kalan tüm gemileri otomatik çözümle
        if (Timer != null && Timer.IsFinished)
        {
            _shipService?.AutoResolveOnTimeUp();
            PrepareEndDayStats();
            return;
        }

        // Sıradaki gemiyi al (null = tüm gemiler işlendi)
        _currentShip = _shipService?.GetNextShip();
        if (_currentShip == null)
        {
            // Tüm gemiler tamamlandı, gün sonu ekranını göster
            PrepareEndDayStats();
        }
    }

    private void HandleShipArrived(ShipInstance ship)
    {
        // Geçersiz gemi kontrolü
        if (ship == null) return;

        // Mevcut gemiyi kaydet
        _currentShip = ship;

        // Durum geçişi: GemiVarış → GemiKarar (oyuncu kabul/red kararı verecek)
        _stateTransitionHandler?.HandleShipArrival();

        // Gemi varış işlemlerini koordinatöre devret (konteyner spawn, UI güncelleme)
        _shipLifecycleCoordinator?.ProcessShipArrival(ship, _rng, ref _globalContainerSequence);
    }

    private void OnShipAccepted()
    {
        // Geçersiz durum kontrolü (zaman dolmuş veya gemi yok)
        if (Timer == null || Timer.IsFinished || _currentShip == null)
            return;

        // Gemi kabul edildi, konteyner işlemeye başla
        _shipLifecycleCoordinator?.HandleShipAccepted(_currentShip);
    }

    private void OnShipRejected()
    {
        // Geçersiz durum kontrolü (zaman dolmuş veya gemi yok)
        if (Timer == null || Timer.IsFinished || _currentShip == null)
            return;

        // Gemi reddedildi, tüm konteynerleri otomatik reddet ve gemiyi gönder
        _shipLifecycleCoordinator?.HandleShipRejected(_currentShip);
    }

    private void CompleteCurrentShip()
    {
        // Mevcut gemi yoksa işlem yapma
        if (_currentShip == null)
            return;

        // Gemideki tüm konteynerler işlendi, gemiyi tamamla ve ayrılışı başlat
        _shipLifecycleCoordinator?.CompleteCurrentShip(_currentShip);
    }

    private void HandleShipMovementCompleted()
    {
        // Gemi tamamen ayrıldı ve sahne dışına çıktı
        // Durum geçişi: herhangi bir durum → GemiBekleme
        _stateMachine?.TransitionTo(Storia.Core.GameFlow.GameState.AwaitingShip);
        
        // Sıradaki gemiyi getir (veya gün sonunu başlat)
        GetNextShip();
    }

    private void HandleDayTimeUp()
    {
        // Gün zamanlayıcısı sona erdi, bekleyen tüm gemileri ve konteynerleri çözümle
        if (_shipService != null)
        {
            _shipService.AutoResolveOnTimeUp();
        }

        // Gün henüz sonlanmadıysa, gün sonu durumuna geç ve sonuçları göster
        if (!_stateMachine.IsDayEnded())
        {
            _stateTransitionHandler?.EndDay();
            PrepareEndDayStats();
        }
    }

    #endregion

#if ODIN_INSPECTOR
    [InfoBox("Oyunu aynı seed ile yeniden başlat. Deterministik test için kullanılır.", InfoMessageType.Info)]
    [Button("Aynı Seed ile Sıfırla")]
#endif
    private void DevResetSameSeed()
    {
#if UNITY_EDITOR
        // Son çalıştırmanın seed'ini override olarak ayarla
        DevRunSeedOverride.SetOverride(_lastRunSeed);
#endif
        // Sahneyi yeniden yükle (aynı seed ile deterministik test)
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

#if ODIN_INSPECTOR
    [InfoBox("Oyunu yeni random seed ile başlat. Her çalıştırmada farklı konteyner yapılandırması.", InfoMessageType.Info)]
    [Button("Yeni Seed ile Sıfırla")]
#endif
    private void DevResetNewSeed()
    {
#if UNITY_EDITOR
        // Sistem zamanı ve tick count kullanarak yeni random seed üret
        int newSeed = unchecked(System.Environment.TickCount * Storia.Constants.GameConstants.SeedHashMultiplier);
        newSeed ^= (int)(System.DateTime.UtcNow.Ticks & 0xFFFFFFFF);
        DevRunSeedOverride.SetOverride(newSeed);
#endif
        // Sahneyi yeni seed ile yükle (farklı konteyner konfigürasyonu)
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

#if ODIN_INSPECTOR
    [InfoBox("Seed override'ı temizle. Sonraki çalışma DaySeedConfig'den seed alacaktır.", InfoMessageType.Info)]
    [Button("Seed Override'ı Temizle")]
#endif
    private void DevClearSeedOverride()
    {
#if UNITY_EDITOR
        // Seed override'ı temizle, sonraki çalışma DaySeedConfig'den seed alacak
        DevRunSeedOverride.Clear();
#endif
    }
}