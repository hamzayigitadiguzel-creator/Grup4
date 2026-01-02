using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Scanner : MonoBehaviour
{
    public TextMeshProUGUI[] texts;
    private int URUNISIM = 0, URUNFIYAT = 1, TOPLAMFIYAT = 2; 
    [HideInInspector]public int toplam = 0;
    private Draggable urun;
    [SerializeField]private AudioClip beepSound;
    [SerializeField]private AudioSource audio;
    void OnTriggerEnter(Collider other)
    {
        urun = other.gameObject.GetComponent<Draggable>();
        if(urun != null)
        {
            if(urun.isDragging && !urun.isScanned && urun.value != 0)
            {
                audio.PlayOneShot(beepSound);
                urun.isScanned = true;
                toplam += urun.value;
                texts[URUNISIM].text = "Ürün Adı: " + urun.name;
                texts[URUNFIYAT].text = "Ürün Fiyat: " + urun.value + "TL";
                texts[TOPLAMFIYAT].text = "Toplam Fiyat: " + toplam + "TL";
            } 
        }
    }

    public void ScannerReset()
    {
        texts[URUNISIM].text = "Ürün Adı: ";
        texts[URUNFIYAT].text = "Ürün Fiyat: ";
        texts[TOPLAMFIYAT].text = "Toplam Fiyat: ";
    }
}
