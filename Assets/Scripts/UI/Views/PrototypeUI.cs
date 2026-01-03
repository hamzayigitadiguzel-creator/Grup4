using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Storia.Constants;
using Storia.UI;
using Storia.UI.Utilities;

/// <summary>
/// Workstation UI - 3D dünyada etkileşim noktalarında görünen UI.
/// World-Space Canvas'ta render edilir.
/// Gemi kararı + Konteyner değerlendirmesi için kullanılır.
/// </summary>
public sealed class PrototypeUI : MonoBehaviour, IWorkstationView
{
    [Header("Gemi Paneli")]
    [Tooltip("Gemi bilgileri paneli (isim, ID, origin, konteyner sayısı, yolculuk süresi)")]
    [SerializeField] private GameObject _shipInfoPanel;
    [Tooltip("Gemi ismi metin alanı")]
    [SerializeField] private TMP_Text _txtShipName;
    [Tooltip("Gemi ID metin alanı")]
    [SerializeField] private TMP_Text _txtShipId;
    [Tooltip("Gemi origin liman metin alanı")]
    [SerializeField] private TMP_Text _txtOriginPort;
    [Tooltip("Gemi konteyner sayısı metin alanı")]
    [SerializeField] private TMP_Text _txtContainerCount;
    [Tooltip("Gemi yolculuk süresi metin alanı")]
    [SerializeField] private TMP_Text _txtVoyageHours;
    [Tooltip("Gemi kabul butonu")]
    [SerializeField] private Button _btnShipAccept;
    [Tooltip("Gemi reddet butonu")]
    [SerializeField] private Button _btnShipReject;

    [Header("Konteyner Paneli")]
    [Tooltip("Konteyner bilgileri paneli (manifest + label)")]
    [SerializeField] private GameObject _containerPanel;
    [Tooltip("Konteyner başlık metin alanı")]
    [SerializeField] private TMP_Text _txtContainerTitle;
    [Tooltip("Manifest bilgi bloğu metin alanı")]
    [SerializeField] private TMP_Text _txtManifestBlock;
    [Tooltip("Label bilgi bloğu metin alanı")]
    [SerializeField] private TMP_Text _txtLabelBlock;
    [Tooltip("Kabul butonu")]
    [SerializeField] private Button _btnAccept;
    [Tooltip("Reddet butonu")]
    [SerializeField] private Button _btnReject;

    [Header("Placement Paneli")]
    [Tooltip("Placement paneli root GameObject'i")]
    [SerializeField] private GameObject _placementPanelRoot;
    [Tooltip("Zone A butonu")]
    [SerializeField] private Button _btnZoneA;
    [Tooltip("Zone B butonu")]
    [SerializeField] private Button _btnZoneB;
    [Tooltip("Zone C butonu")]
    [SerializeField] private Button _btnZoneC;

    // ============ Gemi Paneli ============

    public void ShowShipInfo(string shipName, string shipId, string originPort,
                            int containerCount, float voyageHours)
    {
        // Konteyner panelini gizle
        if (_containerPanel != null)
            _containerPanel.SetActive(false);

        // Gemi panelini göster
        if (_shipInfoPanel != null)
        {
            _txtShipName.text = shipName;
            _txtShipId.text = string.Format(UIConstants.ShipIdFormat, shipId);
            _txtOriginPort.text = string.Format(UIConstants.ShipOriginFormat, originPort);
            _txtContainerCount.text = string.Format(UIConstants.ShipContainerFormat, containerCount);
            _txtVoyageHours.text = string.Format(UIConstants.ShipVoyageFormat, voyageHours);

            _shipInfoPanel.SetActive(true);
        }
    }

    public void BindShipDecisionButtons(System.Action onAccept, System.Action onReject)
    {
        UIButtonHelper.BindButton(_btnShipAccept, onAccept);
        UIButtonHelper.BindButton(_btnShipReject, onReject);
    }

    public void HideShipInfo()
    {
        if (_shipInfoPanel != null)
            _shipInfoPanel.SetActive(false);
    }

    public bool IsShipPanelActive => _shipInfoPanel != null && _shipInfoPanel.activeSelf;

    /// <summary>
    /// Konteyner panelini gizle.
    /// </summary>
    public void HideContainer()
    {
        if (_containerPanel != null)
            _containerPanel.SetActive(false);
    }

    // ============ Konteyner Paneli ============

    public void BindDecisionButtons(System.Action onAccept, System.Action onReject)
    {
        UIButtonHelper.BindButton(_btnAccept, onAccept);
        UIButtonHelper.BindButton(_btnReject, onReject);
    }

    public void BindPlacementButtons(System.Action<int> onZonePicked)
    {
        UIButtonHelper.BindButton(_btnZoneA, () => onZonePicked?.Invoke(1));
        UIButtonHelper.BindButton(_btnZoneB, () => onZonePicked?.Invoke(2));
        UIButtonHelper.BindButton(_btnZoneC, () => onZonePicked?.Invoke(3));
    }

    public void ShowContainer(ContainerFields manifestShown, ContainerFields labelShown)
    {
        // Gemi panelini gizle
        if (_shipInfoPanel != null)
            _shipInfoPanel.SetActive(false);

        // Konteyner panelini göster
        if (_containerPanel != null)
        {
            _txtContainerTitle.text = string.Format(UIConstants.ContainerTitleFormat, manifestShown.containerId);

            _txtManifestBlock.text =
                $"{UIConstants.ManifestHeader}\n" +
                $"{string.Format(UIConstants.ContainerIdFormat, manifestShown.containerId)}\n" +
                $"{string.Format(UIConstants.OriginPortFormat, manifestShown.originPort)}\n" +
                $"{string.Format(UIConstants.CargoLabelFormat, manifestShown.cargoLabel)}";

            _txtLabelBlock.text =
                $"{UIConstants.LabelHeader}\n" +
                $"{string.Format(UIConstants.ContainerIdFormat, labelShown.containerId)}\n" +
                $"{string.Format(UIConstants.OriginPortFormat, labelShown.originPort)}\n" +
                $"{string.Format(UIConstants.CargoLabelFormat, labelShown.cargoLabel)}";

            _containerPanel.SetActive(true);
        }
    }

    public void ShowPlacement(bool show)
    {
        _placementPanelRoot.SetActive(show);
    }
}
