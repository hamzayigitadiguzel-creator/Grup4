using System.Collections.Generic;
using System.Text;
using Storia.Constants;
using Storia.Data.Generated;
using Storia.Rules.Runtime;

namespace Storia.Managers.Decision
{
    /// <summary>
    /// Konteyner kabul/red kararlarını ve logunu yöneten sınıf.
    /// Artık runtime-generated rule'lar ile çalışır.
    /// </summary>
    public sealed class DecisionManager
    {
        private readonly List<DecisionRecord> _decisionLog;
        private readonly ITaskRule _taskRule;
        private readonly PlacementRuleData _placementRule;

        private bool _currentDecisionAccepted;
        private bool _currentDecisionIsTarget;

        public int TotalDecisions { get; private set; }
        public int CorrectDecisions { get; private set; }
        public int WrongDecisions { get; private set; }
        public int CorrectPlacements { get; private set; }
        public int WrongPlacements { get; private set; }

        public DecisionManager(ITaskRule taskRule, PlacementRuleData placementRule)
        {
            _decisionLog = new List<DecisionRecord>(GameConstants.DecisionLogInitialCapacity);
            _taskRule = taskRule;
            _placementRule = placementRule;
        }

        public void Reset()
        {
            _decisionLog.Clear();
            TotalDecisions = 0;
            CorrectDecisions = 0;
            WrongDecisions = 0;
            CorrectPlacements = 0;
            WrongPlacements = 0;
            _currentDecisionAccepted = false;
            _currentDecisionIsTarget = false;
        }

        public bool RegisterDecision(ContainerData containerData, bool isAccepted, ContainerPresentation presentation)
        {
            TotalDecisions++;

            bool isTarget = _taskRule != null && _taskRule.IsTarget(in containerData.truth);
            _currentDecisionAccepted = isAccepted;
            _currentDecisionIsTarget = isTarget;

            bool isCorrect = (isTarget && isAccepted) || (!isTarget && !isAccepted);

            if (isCorrect)
                CorrectDecisions++;
            else
                WrongDecisions++;

            // Red için log'u hemen yaz
            if (!isAccepted)
            {
                AppendDecisionLogForReject(containerData, isTarget, presentation);
            }

            return isAccepted; // Placement gerekli mi?
        }

        public void RegisterPlacement(ContainerData containerData, int pickedZoneId, ContainerPresentation presentation)
        {
            int expectedZoneId = 0;
            bool hasExpected = _placementRule != null && 
                               _placementRule.TryGetExpectedZoneId(in containerData.truth, out expectedZoneId);

            if (hasExpected)
            {
                bool isCorrectPlacement = expectedZoneId == pickedZoneId;

                if (isCorrectPlacement)
                    CorrectPlacements++;
                else
                    WrongPlacements++;
            }

            // Kabul kararı için log: placement ile tamamlanır
            DecisionRecord rec = new DecisionRecord
            {
                containerId = containerData.truth.containerId,
                isTarget = _currentDecisionIsTarget,
                accepted = true,
                placementHappened = true,
                pickedZoneId = pickedZoneId,
                expectedZoneId = expectedZoneId,
                conflict = presentation.conflict
            };

            _decisionLog.Add(rec);
        }

        private void AppendDecisionLogForReject(ContainerData containerData, bool isTarget, ContainerPresentation presentation)
        {
            DecisionRecord rec = new DecisionRecord
            {
                containerId = containerData.truth.containerId,
                isTarget = isTarget,
                accepted = false,
                placementHappened = false,
                pickedZoneId = 0,
                expectedZoneId = 0,
                conflict = presentation != null ? presentation.conflict : PresentationConflict.None
            };

            _decisionLog.Add(rec);
        }

        public string BuildEndOfDayLogText(int seed)
        {
            StringBuilder sb = new StringBuilder(GameConstants.StringBuilderLargeCapacity);

            FormatDecisionLogHeader(sb, seed);

            int issueCount = 0;

            for (int i = 0; i < _decisionLog.Count; i++)
            {
                DecisionRecord r = _decisionLog[i];

                // Karar doğruluğu
                bool decisionCorrect = (r.isTarget && r.accepted) || (!r.isTarget && !r.accepted);

                // Yerleşim doğruluğu
                bool placementChecked = r.placementHappened && r.expectedZoneId != 0;
                bool placementCorrect = !placementChecked || (r.pickedZoneId == r.expectedZoneId);

                bool hasIssue = !decisionCorrect || !placementCorrect;

                if (!hasIssue)
                    continue;

                issueCount++;
                FormatDecisionEntry(sb, r, issueCount, decisionCorrect, placementCorrect, placementChecked);
            }

            FormatStatsSummary(sb, issueCount);

            return sb.ToString();
        }

        /// <summary>
        /// Karar log'u başlığını formatlar.
        /// </summary>
        private void FormatDecisionLogHeader(StringBuilder sb, int seed)
        {
            sb.AppendLine("Karar Dökümü (Hatalar):");
#if UNITY_EDITOR
            sb.AppendLine($"Seed: {seed}");
#endif
            sb.AppendLine();
        }

        /// <summary>
        /// Tek bir karar kaydını formatlar.
        /// </summary>
        private void FormatDecisionEntry(StringBuilder sb, DecisionRecord r, int issueNumber, 
                                        bool decisionCorrect, bool placementCorrect, bool placementChecked)
        {
            sb.AppendLine($"#{issueNumber}  ID: {r.containerId}");
            sb.AppendLine($"- Hedef Mi?: {(r.isTarget ? "EVET" : "HAYIR")}  | Karar Ne?: {(r.accepted ? "KABUL" : "RED")}  | Karar Doğru Mu?: {(decisionCorrect ? "EVET" : "HAYIR")}");
            sb.AppendLine($"- Çelişki Ne?: {r.conflict}");

            if (r.accepted)
            {
                if (placementChecked)
                {
                    string pickedName = _placementRule?.GetZoneNameForDisplay(r.pickedZoneId) ?? $"Zone {r.pickedZoneId}";
                    string expectedName = _placementRule?.GetZoneNameForDisplay(r.expectedZoneId) ?? $"Zone {r.expectedZoneId}";
                    sb.AppendLine($"- Yerleşim: Seçilen Zone = {pickedName}  Beklenen Zone = {expectedName}  | Yerleşim Doğru Mu?: {(placementCorrect ? "EVET" : "HAYIR")}");
                }
                else
                {
                    string pickedName = _placementRule?.GetZoneNameForDisplay(r.pickedZoneId) ?? $"Zone {r.pickedZoneId}";
                    sb.AppendLine($"- Yerleşim: (Beklenen Zone tanımlı değil)  Seçilen Zone = {pickedName}");
                }
            }

            sb.AppendLine();
        }

        /// <summary>
        /// Özet istatistiklerini formatlar.
        /// </summary>
        private void FormatStatsSummary(StringBuilder sb, int issueCount)
        {
            if (issueCount == 0)
            {
                sb.AppendLine("Hata bulunmadı. (Bu seed de oynanan oyunda tüm kararlar doğru.)");
                sb.AppendLine();
            }

            sb.AppendLine("Not: Bu liste prototip ölçümü içindir. Nihai kurallar GDD ile değişebilir.");
        }

        public int GetDecisionLogCount() => _decisionLog.Count;
    }
}
