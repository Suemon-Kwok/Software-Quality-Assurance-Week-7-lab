namespace AppointmentBooking.Metrics;

public static class MetricsCalculator
{
    public static double ValidSuccessRate(IEnumerable<BookingEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        var validAttempts = events
            .Where(e => e.IsValidRequest)
            .ToList();

        if (validAttempts.Count == 0)
        {
            throw new InvalidOperationException(
                "Valid-request success rate cannot be calculated without valid requests.");
        }

        int successes = validAttempts.Count(e =>
            string.Equals(e.Outcome, "Success", StringComparison.OrdinalIgnoreCase));

        return Math.Round(successes * 100.0 / validAttempts.Count, 2);
    }

    public static double Percentile(IEnumerable<double> observations, double percentile)
    {
        ArgumentNullException.ThrowIfNull(observations);

        if (percentile <= 0 || percentile > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(percentile),
                "Percentile must be greater than 0 and no greater than 1.");
        }

        var sorted = observations.OrderBy(value => value).ToArray();

        if (sorted.Length == 0)
        {
            throw new InvalidOperationException(
                "A percentile cannot be calculated without observations.");
        }

        int rank = (int)Math.Ceiling(percentile * sorted.Length);
        return sorted[rank - 1];
    }

    public static IReadOnlyDictionary<string, double> ValidSuccessRateByTimeBand(
        IEnumerable<BookingEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        return events
            .Where(e => e.IsValidRequest)
            .GroupBy(e => e.TimeBand, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => Math.Round(
                    group.Count(e => string.Equals(
                        e.Outcome,
                        "Success",
                        StringComparison.OrdinalIgnoreCase))
                    * 100.0 / group.Count(),
                    2),
                StringComparer.OrdinalIgnoreCase);
    }
}

