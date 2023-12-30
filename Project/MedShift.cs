using System;
using System.Collections.Generic;

namespace MedShiftGen {

    #region class MedShift

    /// <summary>
    /// Class that describes a single medic shift.
    /// </summary>
    public class MedShift {

        #region Sort/Query

        /// <summary>
        /// Sort the shift by weekend first then day
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        static public int SortByWeekendAndDay(MedShift a,MedShift b) {
            int ti_a = (int)a.roundType;
            int ti_b = (int)b.roundType;
            if (a.isWeekend) if (!b.isWeekend) return -1;
            if (!a.isWeekend) if (b.isWeekend) return 1;
            if (a.date < b.date) return -1;
            if (a.date > b.date) return 1;
            if (ti_a < ti_b) return -1;
            if (ti_a > ti_b) return 1;
            return 0;
        }

        /// <summary>
        /// Sorts the shifts by round and day
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        static public int SortByDayAndRound(MedShift a,MedShift b) {
            int ti_a = (int)a.roundType;
            int ti_b = (int)b.roundType;
            if (a.date < b.date) return -1;
            if (a.date > b.date) return 1;
            if (ti_a < ti_b) return -1;
            if (ti_a > ti_b) return 1;
            return 0;
        }

        /// <summary>
        /// Sorts the shifts by round and day
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        static public int SortByDayAndRoundRandom(MedShift a,MedShift b) {
            int ti_a = (int)a.roundType;
            int ti_b = (int)b.roundType;
            if (a.date < b.date) return m_rnd.NextDouble() < 0.5f ? -1 : 1;
            if (a.date > b.date) return m_rnd.NextDouble() < 0.5f ? -1 : 1;
            if (ti_a < ti_b) return -1;
            if (ti_a > ti_b) return 1;
            return 0;
        }

        /// <summary>
        /// Sorts the shifts by day and round
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        static public int SortByRoundAndDay(MedShift a,MedShift b) {
            int ti_a = (int)a.roundType;
            int ti_b = (int)b.roundType;
            if (ti_a < ti_b) return -1;
            if (ti_a > ti_b) return 1;
            if (a.date < b.date) return -1;
            if (a.date > b.date) return 1;
            return 0;
        }

        #endregion

        #region Round

        /// <summary>
        /// Round Schedule
        /// </summary>
        public ShiftRoundFlag roundType;

        /// <summary>
        /// Returns the round flag as a single letter prefix
        /// </summary>
        public string roundTypePrefix { get { return roundType.GetPrefix(); } }

        #endregion

        #region Date

        /// <summary>
        /// Shift Date
        /// </summary>
        public DateTime date { get { return m_date; } }
        private DateTime m_date;

        /// <summary>
        /// Shift Year
        /// </summary>
        public int year { get { return m_date.Year; } set { m_date = new DateTime(value,m_date.Month,m_date.Day); } }

        /// <summary>
        /// Shift Month
        /// </summary>
        public int month { get { return m_date.Month; } set { m_date = new DateTime(m_date.Year,value,m_date.Day); } }

        /// <summary>
        /// Shift Day
        /// </summary>
        public int day { get { return m_date.Day; } set { m_date = new DateTime(m_date.Year,m_date.Month,value); } }

        /// <summary>
        /// Shift Day of Week
        /// </summary>
        public DayOfWeek dayOfWeek { get { return m_date.DayOfWeek; } }

        /// <summary>
        /// Checks if a dayofweek matches the date or holiday
        /// </summary>
        /// <param name="p_weekday"></param>
        /// <returns></returns>
        public bool MatchDayOfWeek(DayOfWeek p_weekday) {
            if (p_weekday == (DayOfWeek)(100)) return isHoliday;
            return date.DayOfWeek == p_weekday;
        }

        /// <summary>
        /// Returns the day of week as single letter
        /// </summary>
        public string dayOfWeekLetter { get { return m_date.GetDayOfWeekLetterPTBR(); } }

        /// <summary>
        /// Returns the day of week as single letter
        /// </summary>
        public string dayOfWeekShort { get { return m_date.GetDayOfWeekLetterPTBR(); } }

        /// <summary>
        /// Returns a flag telling this shift is on a weekend
        /// </summary>
        public bool isWeekend { get { DayOfWeek dds = date.DayOfWeek; return (dds == DayOfWeek.Saturday) || (dds == DayOfWeek.Sunday); } }

        #endregion

        #region Medics

        /// <summary>
        /// Flag that tells this shift is full
        /// </summary>
        public bool isFull { get { return medics == null ? false : (medics.Count >= quorum); } }

        /// <summary>
        /// Number of needed medics for this shift
        /// </summary>
        public int quorum;

        /// <summary>
        /// Number of open slots
        /// </summary>
        public int slotsCount { get { int mc = medics == null ? 0 : medics.Count; return quorum - mc; } }

        /// <summary>
        /// List of registered medics.
        /// </summary>
        public List<Medic> medics;

        /// <summary>
        /// Returns the string of medic names padded to a size
        /// </summary>
        /// <param name="p_right"></param>
        /// <param name="p_size"></param>
        /// <returns></returns>
        public string GetPaddedMedicNames(bool p_blocked,bool p_right,int p_size) {
            string res = $"";
            List<Medic> ml = p_blocked ? blocked : medics;
            for (int i = 0;i < quorum;i++) {
                string med_name = i >= ml.Count ? "" : ml[i].name;
                res += $"{(p_right ? med_name.PadRight(p_size) : med_name.PadLeft(p_size))}";
            }
            return res;
        }

        /// <summary>
        /// List of blocked medics
        /// </summary>
        public List<Medic> blocked;

        #endregion

        /// <summary>
        /// Flag that tells this shift if a holiday
        /// </summary>
        public bool isHoliday;

        /// <summary>
        /// Flag that tells this shift can be joined.
        /// </summary>
        public bool isOpen;

        /// <summary>
        /// CTOR.
        /// </summary>
        public MedShift() {
            medics  = new List<Medic>();
            blocked = new List<Medic>();
            isOpen  = true;
        }

        #region Permissions

        /// <summary>
        /// Block the informed medics to join this shift.
        /// </summary>
        /// <param name="p_medicos"></param>
        public void Block(IList<Medic> p_medicos) {
            foreach (Medic m in p_medicos) if (!blocked.Contains(m)) blocked.Add(m);
        }

        /// <summary>
        /// Block the informed medic to join this shift.
        /// </summary>
        /// <param name="p_medico"></param>
        public void Block(Medic p_medico) {
            if (!blocked.Contains(p_medico)) blocked.Add(p_medico);
        }

        public bool IsAllowed(Medic p_medico) {
            return !blocked.Contains(p_medico);
        }

        #endregion

        /// <summary>
        /// String representation of the shift
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            string mls  = GetPaddedMedicNames(false,true,12);
            string bmls = GetPaddedMedicNames(true,true,12);
            string dds = date.GetDayOfWeekLetterShortPTBR();
            string ts = roundTypePrefix;
            return $"{date.ToString("dd/MM/yyyy")} {dds} {ts} - {mls} | Vetado(s): {bmls}";
        }

        /// <summary>
        /// Internals
        /// </summary>
        static Random m_rnd = new Random((int)DateTime.Now.Millisecond);

    }

    #endregion

}