public static class FinancialYearHelper
{
    public static short GetCurrentFinancialYear()
    {
        DateTime today = DateTime.Now;

        int startYear;
        int endYear;

        // If month is April (4) or later → FY starts this year
        if (today.Month >= 4)
        {
            startYear = today.Year;
            endYear = today.Year + 1;
        }
        else
        {
            // Jan, Feb, Mar → FY started previous year
            startYear = today.Year - 1;
            endYear = today.Year;
        }

        // Convert 2025 → 25 and 2026 → 26
        int startYY = startYear % 100;
        int endYY = endYear % 100;

        // Combine into 2526 format
        return (short)(startYY * 100 + endYY);
    }
}