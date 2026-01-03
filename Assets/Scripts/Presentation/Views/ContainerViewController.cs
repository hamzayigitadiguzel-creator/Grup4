using UnityEngine;
using TMPro;
using Storia.Data.Generated;

namespace Storia.Presentation
{
    /// <summary>
    /// Konteyner 3D görsel bileşeni.
    /// 
    /// Sorumlulukları:
    /// 1. Konteyner presentation verilerine göre görsel güncellemesi (label, indicator)
    /// 2. UI/görsel bileşenlerin yönetimi (mesh, label text, conflict indicator'ları)
    /// 3. Konteyner veri ve presentation verilerine erişim sağlama
    /// 
    /// Kullanım:
    /// - Container pool'dan GetComponent olarak alınır
    /// - Initialize() ile presentation verisi ve model yüklenir
    /// - Görsel otomatik güncellenir
    /// </summary>
    public sealed class ContainerViewController : MonoBehaviour
    {
        [Header("Görsel Bileşenler")]
        /// <summary>Konteyner 3D mesh'i (fiziksel görsel)</summary>
        [Tooltip("Konteyner 3D mesh'i (fiziksel görsel)")]
        [SerializeField] private MeshRenderer _meshRenderer;
        
        /// <summary>Konteyner üzerindeki etiket GameObject'i (manifest-label info)</summary>
        [Tooltip("Konteyner üzerindeki etiket GameObject'i (manifest-label info)")]
        [SerializeField] private GameObject _labelObject;
        
        /// <summary>Etiket üzerinde gösterilen metin (ID, Cargo type)</summary>
        [Tooltip("Etiket üzerinde gösterilen metin (ID, Cargo type)")]
        [SerializeField] private TMP_Text _labelText;

        [Header("Çelişki Görsel Göstergeleri")]
        /// <summary>Manifest-Label çelişkisini gösteren görsel (örn: pas, kırmızı uyarı)</summary>
        [Tooltip("Manifest-Label çelişkisini gösteren görsel (örn: pas, kırmızı uyarı)")]
        [SerializeField] private GameObject _rustIndicator;
        
        /// <summary>Hasar göstergesi (manifest-label çelişkisi durumunu vurgular)</summary>
        [Tooltip("Hasar göstergesi (manifest-label çelişkisi durumunu vurgular)")]
        [SerializeField] private GameObject _damageIndicator;

        /// <summary>Bu konteyner'in presentation verisi (manifest + label)</summary>
        private ContainerPresentation _presentation;
        
        /// <summary>Bu konteyner'in gerçek veri (truth - seed kontrolü için)</summary>
        private ContainerData _containerData;

        /// <summary>
        /// Konteyner presentation verilerine göre görseli başlat ve güncelle.
        /// Pool'dan Get() yapıldıktan sonra çağrılmalıdır.
        /// </summary>
        /// <param name="containerData">Konteyner'in gerçek verisi</param>
        /// <param name="presentation">Konteyner'in manifest + label bilgisi</param>
        public void Initialize(ContainerData containerData, ContainerPresentation presentation)
        {
            _containerData = containerData;
            _presentation = presentation;

            // Görselleri güncelle (label text, conflict indicator'ları)
            ApplyVisuals();
        }

        /// <summary>
        /// Presentation verilerine göre tüm görselleri güncelle.
        /// - Label text: manifestShown ve labelShown bilgilerini göster
        /// - Conflict indicator'ları: çelişki varsa uyar göster
        /// </summary>
        private void ApplyVisuals()
        {
            if (_presentation == null) 
                return;

            // Label gösterimi (etiket üzerindeki bilgi)
            // Manifest ve label'dan seçilen bilgiler görüntüle
            if (_labelText != null && _labelObject != null)
            {
                // Format: "ID\nCargo" (örn: "TRBU-1001\nElektronik")
                _labelText.text = $"{_presentation.labelShown.containerId}\n{_presentation.labelShown.cargoLabel}";
                _labelObject.SetActive(true);
            }

            // Çelişki varsa uyarı göstergelerini göster
            ApplyConflictIndicators();
        }

        /// <summary>
        /// Manifest-Label çelişkisi durumunu görselle belirt.
        /// Çelişki varsa rust/damage indicator'ları aktifleştir.
        /// </summary>
        private void ApplyConflictIndicators()
        {
            // Eğer çelişki varsa (IdMismatch, OriginMismatch, CargoMismatch), indicator göster
            bool hasConflict = _presentation.conflict != PresentationConflict.None;

            // Çelişki varsa rust göstergesi (pas, hasarlı görünüş)
            if (_rustIndicator != null)
                _rustIndicator.SetActive(hasConflict);

            // Çelişki varsa damage göstergesi (uyarı ışığı, bozulma işareti)
            if (_damageIndicator != null)
                _damageIndicator.SetActive(hasConflict);
        }

        /// <summary>
        /// Konteyner'in gerçek veri'sini döndür (placement zone kontrolü için).
        /// </summary>
        /// <returns>ContainerData (truth - expectedZoneId vb. bilgiler içerir)</returns>
        public ContainerData GetContainerData() => _containerData;

        /// <summary>
        /// Konteyner'in presentation verilerini döndür (manifest vs label öğrenmek için).
        /// </summary>
        /// <returns>ContainerPresentation (manifestShown, labelShown, conflict)</returns>
        public ContainerPresentation GetPresentation() => _presentation;

        /// <summary>
        /// Konteyner data property'si (GetContainerData() ile aynı - shortcut).
        /// </summary>
        public ContainerData ContainerData => _containerData;

        /// <summary>
        /// Presentation property'si (GetPresentation() ile aynı - shortcut).
        /// </summary>
        public ContainerPresentation Presentation => _presentation;
    }
}
