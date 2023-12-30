using System;

namespace MedShiftGen {

    #region enum ShiftRoundFlag
    /// <summary>
    /// Flag that
    /// </summary>
    public enum ShiftRoundFlag {

        /// <summary>
        /// None / Invalid
        /// </summary>
        None,
        /// <summary>
        /// Day Time
        /// </summary>
        Day,
        /// <summary>
        /// Night Time
        /// </summary>
        Night

    }
    #endregion

    #region struct HolidayDateTime
    /// <summary>
    /// Struct that describes a holiday datetime.
    /// </summary>
    public struct HolidayDateTime {

        /// <summary>
        /// Name of holiday
        /// </summary>
        public string name;

        /// <summary>
        /// DateTime
        /// </summary>
        public DateTime date;

    }
    #endregion

    #region class MedShiftGenExt
    /// <summary>
    /// Extensions utilities
    /// </summary>
    static public class MedShiftGenExt {

        #region string GetDayOfWeekLetterPTBR

        /// <summary>
        /// Returns the day of week in a single letter for PTBR locale
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        static public string GetDayOfWeekLetterPTBR(this DateTime d) {
            switch (d.DayOfWeek) {
                case DayOfWeek.Monday: return "2";
                case DayOfWeek.Tuesday: return "3";
                case DayOfWeek.Wednesday: return "4";
                case DayOfWeek.Thursday: return "5";
                case DayOfWeek.Friday: return "6";
                case DayOfWeek.Saturday: return "S";
                case DayOfWeek.Sunday: return "D";
            }
            return "";
        }

        /// <summary>
        /// Returns the day of week in a single letter for PTBR locale
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        static public string GetDayOfWeekLetterPTBR(this DayOfWeek d) {
            switch (d) {
                case DayOfWeek.Monday: return "2";
                case DayOfWeek.Tuesday: return "3";
                case DayOfWeek.Wednesday: return "4";
                case DayOfWeek.Thursday: return "5";
                case DayOfWeek.Friday: return "6";
                case DayOfWeek.Saturday: return "S";
                case DayOfWeek.Sunday: return "D";
            }
            return "";
        }

        #endregion

        #region string GetDayOfWeekLetterShortPTBR

        /// <summary>
        /// Returns the day of week in a 3 letter format at PTBR locale
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        static public string GetDayOfWeekLetterShortPTBR(this DateTime d) {
            switch (d.DayOfWeek) {
                case DayOfWeek.Monday: return "Seg";
                case DayOfWeek.Tuesday: return "Ter";
                case DayOfWeek.Wednesday: return "Qua";
                case DayOfWeek.Thursday: return "Qui";
                case DayOfWeek.Friday: return "Sex";
                case DayOfWeek.Saturday: return "Sab";
                case DayOfWeek.Sunday: return "Dom";
            }
            return "";
        }

        #endregion

        #region string GetPrefix
        /// <summary>
        /// Given the shift round enumeration return a single letter prefix
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        static public string GetPrefix(this ShiftRoundFlag v) {
            switch (v) {
                case ShiftRoundFlag.None: return "*";
                case ShiftRoundFlag.Day: return "M";
                case ShiftRoundFlag.Night: return "N";
            }
            return "I";
        }
        #endregion

        #region DayOfWeek Next
        /// <summary>
        /// Returns the following day of week
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        static public DayOfWeek Next(this DayOfWeek v) {
            return (DayOfWeek)(((int)v + 1) % 7);
        }
        #endregion

    }
    #endregion

}
