namespace EnweVolume.Core.Converters;

public static class AudioLevelConverter
{
    // Reference values for conversion
    private const float REFERENCE_ACOUSTIC_LEVEL = 20e-6f; // 20 micropascals (standard reference for sound pressure)
    private const float REFERENCE_DIGITAL_LEVEL = 1.0f; // Full scale in digital audio

    /// <summary>
    /// Converts dBFS (digital decibels) to dB SPL (Sound Pressure Level)
    /// </summary>
    /// <param name="dbFS">Digital audio level in decibels relative to full scale</param>
    /// <param name="calibrationFactor">Device-specific calibration factor</param>
    /// <returns>Sound Pressure Level in dB SPL</returns>
    public static float DbFSToDbSPL(float dbFS, float calibrationFactor = 90f)
    {
        /*
        // Convert dBFS to linear amplitude
        float linearAmplitude = (float)Math.Pow(10, dbFS / 20);

        // Calculate acoustic pressure
        float acousticPressure = linearAmplitude * REFERENCE_ACOUSTIC_LEVEL;

        // Convert to dB SPL with calibration
        // Default calibration assumes 94 dB SPL = 1 Pa
        float dbSPL = (float)(20 * Math.Log10(acousticPressure / REFERENCE_ACOUSTIC_LEVEL) + calibrationFactor);

        return Math.Max(0, dbSPL); // Ensure non-negative values
        */
        float dbSPL = dbFS + calibrationFactor;
        return Math.Clamp(dbSPL, 0f, 140f); // Cap at realistic maximum
    }

    /// <summary>
    /// Converts current audio meter peak value to estimated dB SPL
    /// </summary>
    /// <param name="peakValue">Audio meter peak value (0-1 range)</param>
    /// <param name="systemVolume">System volume scalar (0-1 range)</param>
    /// <param name="calibrationFactor">Device-specific calibration factor</param>
    /// <returns>Estimated Sound Pressure Level in dB</returns>
    public static float PeakValueToDbSPL(float peakValue, float systemVolume, float calibrationFactor = 90f)
    {
        /*
        // Combine peak value and system volume
        float combinedLevel = Math.Abs(peakValue * systemVolume);

        // Prevent log of zero
        combinedLevel = Math.Max(combinedLevel, float.Epsilon);

        // Convert to dBFS first
        float dbFS = (float)(20 * Math.Log10(combinedLevel));

        // Convert dBFS to dB SPL
        return DbFSToDbSPL(dbFS, calibrationFactor);
        */

        float combinedLevel = Math.Clamp(peakValue * systemVolume, float.Epsilon, 1f);
        float dbFS = 20f * (float)Math.Log10(combinedLevel); // Peak dBFS
        return DbFSToDbSPL(dbFS, calibrationFactor);
    }
}