using System.Collections.Generic;
using Storia.Data.Generated;

namespace Storia.Managers.Deck
{
    /// <summary>
    /// Konteyner destesini yöneten ve karıştıran sınıf.
    /// </summary>
    public sealed class DeckManager
    {
        private readonly List<ContainerData> _originalDeck;
        private List<ContainerData> _runtimeDeck;
        private int _currentIndex;

        public DeckManager(List<ContainerData> deck)
        {
            _originalDeck = deck;
        }

        public void ShuffleDeck(DeterministicRng rng)
        {
            if (_originalDeck == null || _originalDeck.Count == 0)
            {
                _runtimeDeck = _originalDeck;
                _currentIndex = 0;
                return;
            }

            // Liste kopyası oluştur
            _runtimeDeck = new List<ContainerData>(_originalDeck);

            // DeterministicRng'nin Shuffle metodunu kullan
            rng.Shuffle(_runtimeDeck);

            _currentIndex = 0;
        }

        public bool HasNext()
        {
            if (_runtimeDeck == null || _runtimeDeck.Count == 0)
                return false;

            return _currentIndex < _runtimeDeck.Count;
        }

        public ContainerData GetNext()
        {
            if (!HasNext())
                return null;

            ContainerData card = _runtimeDeck[_currentIndex];
            _currentIndex++;
            return card;
        }

        public void Reset()
        {
            _currentIndex = 0;
        }
    }
}
