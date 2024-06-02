using System;
using System.Collections.Generic;
using System.Linq;

namespace Rapport_sport
{
    public static class BestPerformanceCalculator
    {
        public static List<BestPerformance> CalculateBestPerformances(List<TrainingSession> sessions)
        {
            return sessions
                .Where(s => s.Weight.HasValue && s.Reps.HasValue)
                .GroupBy(s => s.Exercise.Trim())
                .Select(g => new BestPerformance
                {
                    Exercise = g.Key,
                    BestSession = g.OrderByDescending(s => s.Weight.Value * (1 + 0.025 * s.Reps.Value)).First()
                })
                .ToList();
        }
    }

    public class BestPerformance
    {
        public string Exercise { get; set; }
        public TrainingSession BestSession { get; set; }
    }
}
