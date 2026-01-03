public struct DecisionRecord
{
    public string containerId;

    public bool isTarget;          // Görev listesine göre hedef mi?
    public bool accepted;          // Oyuncu kabul mü etti?

    public bool placementHappened; // Kabul sonrası yer seçildi mi?
    public int pickedZoneId;       // Oyuncunun seçtiği ZoneId
    public int expectedZoneId;     // Policy'ye göre beklenen ZoneId (0 = none)

    public PresentationConflict conflict; // Manifest/Etiket çelişki türü (debug için)
}
