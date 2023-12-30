using System;
using System.Collections.Generic;

namespace MedShiftGen {

    #region class MedicShiftStats

    /// <summary>
    /// Class that describes a month stats
    /// </summary>
    public class MedicShiftStats {

        /// <summary>
        /// Total Shift Count
        /// </summary>
        public int count { get { int c = 0; foreach (DayOfWeek v in wdl) c += GetCount(v); return c; } }
        static DayOfWeek[] wdl = new DayOfWeek[] { DayOfWeek.Sunday,DayOfWeek.Monday,DayOfWeek.Tuesday,DayOfWeek.Wednesday,DayOfWeek.Thursday,DayOfWeek.Friday };

        /// <summary>
        /// Returns the number of holidays worked.
        /// </summary>
        public int holidayCount { get { return GetCount((DayOfWeek)(100)); } }

        /// <summary>
        /// Number of vacation days.
        /// </summary>
        public int vacations;

        /// <summary>
        /// Total number of shifts available.
        /// </summary>
        public int totalShifts;

        /// <summary>
        /// Internals
        /// </summary>
        private Dictionary<DayOfWeek,int> m_weekday_count_table;

        /// <summary>
        /// CTOR.
        /// </summary>
        public MedicShiftStats() {
            m_weekday_count_table = new Dictionary<DayOfWeek,int>();
            vacations = 0;
        }

        /// <summary>
        /// Flag that tell the medic is available in this month
        /// </summary>
        public bool available { get { return count < (totalShifts - vacations); } }

        /// <summary>
        /// Clear the counts.
        /// </summary>
        public void Clear() {
            m_weekday_count_table.Clear();

        }

        /// <summary>
        /// Increment the number of shifts for a weekday.
        /// </summary>
        /// <param name="p_weekday"></param>
        public void Increment(DayOfWeek p_weekday) {
            Dictionary<DayOfWeek,int> wct = m_weekday_count_table;
            if (!wct.ContainsKey(p_weekday)) wct[p_weekday] = 0;
            wct[p_weekday]++;
        }

        /// <summary>
        /// Increment the amount of holidays
        /// </summary>
        public void IncrementHoliday() { Increment((DayOfWeek)(100)); }

        /// <summary>
        /// Returns the number of shifts of a given weekday
        /// </summary>
        /// <param name="p_weekday"></param>
        /// <returns></returns>
        public int GetCount(DayOfWeek p_weekday) {
            Dictionary<DayOfWeek,int> wct = m_weekday_count_table;
            if (!wct.ContainsKey(p_weekday)) wct[p_weekday] = 0;
            return wct[p_weekday];
        }

        /// <summary>
        /// Returns the ratio of weekends over all shift count.
        /// </summary>
        /// <returns></returns>
        public float GetWeekendRatio() {
            float c0 = GetCount(DayOfWeek.Saturday);
            float c1 = GetCount(DayOfWeek.Sunday);
            float t = totalShifts;
            return t <= 0f ? 0f : Math.Min(c0,c1) / t;
        }

    }

    #endregion

    #region class Medic

    /// <summary>
    /// Class that describes a medic ent0ry00
    /// </summary>
    public class Medic {

        #region Sort/Query

        /// <summary>
        /// Returns a comparer for medics in the same weekday
        /// </summary>
        /// <param name="p_week_day"></param>
        /// <returns></returns>
        static public Comparison<Medic> GetSortMethod(bool p_priority,int p_month,DayOfWeek p_week_day) {
            return delegate (Medic a,Medic b) {
                int dm = p_month;
                DayOfWeek wd = p_week_day;

                if (a.preferences.Contains(wd)) if (!b.preferences.Contains(wd)) return -1;
                if (!a.preferences.Contains(wd)) if (b.preferences.Contains(wd)) return 1;

                if (p_priority) {
                    int wkd_count_a = a.stats[dm].count;
                    int wkd_count_b = b.stats[dm].count;
                    if (wkd_count_a <= 3) if (wkd_count_b > 3) return -1;
                    if (wkd_count_b <= 3) if (wkd_count_a > 3) return 1;
                    if (a.order < b.order) return -1;
                    if (b.order < a.order) return 1;
                }

                switch (p_week_day) {

                    default: {
                        int wkd_count_a = a.stats[dm].GetCount(wd);
                        int wkd_count_b = b.stats[dm].GetCount(wd);
                        if (wkd_count_a < wkd_count_b) return -1;
                        if (wkd_count_a > wkd_count_b) return 1;
                    }
                    break;

                    case (DayOfWeek)(100): {
                        int wkd_count_a = a.holidayCount;
                        int wkd_count_b = b.holidayCount;
                        if (wkd_count_a < wkd_count_b) return -1;
                        if (wkd_count_a > wkd_count_b) return 1;
                        //Reverse priority
                        if (a.order < b.order) return 1;
                        if (b.order < a.order) return -1;
                    }
                    break;

                    case DayOfWeek.Saturday:
                    case DayOfWeek.Sunday: {
                        int fdspi_a = (int)Math.Round(a.stats[dm].GetWeekendRatio() * 100f);
                        int fdspi_b = (int)Math.Round(b.stats[dm].GetWeekendRatio() * 100f);
                        if (fdspi_a < fdspi_b) return -1;
                        if (fdspi_a > fdspi_b) return 1;
                        //Reverse priority 
                        if (a.order < b.order) return 1;
                        if (b.order < a.order) return -1;
                    }
                    break;
                    //*/
                }
                //return string.Compare(a.name,b.name);
                return m_med_rnd.NextDouble() < 0.5f ? -1 : 1;
            };
        }

        /// <summary>
        /// Sorts by name
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        static public int SortByName(Medic a,Medic b) { return string.Compare(a.name,b.name); }

        /// <summary>
        /// Sorts by priority
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        static public int SortByPriority(Medic a,Medic b) { return a.order < b.order ? -1 : (b.order < a.order ? 1 : 0) ; }

        /// <summary>
        /// Sorts random
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        static public int SortRandom(Medic a,Medic b) { return m_med_rnd.NextDouble() < 0.5f ? -1 : 1; }

        #endregion

        /// <summary>
        /// Medic Name
        /// </summary>
        public string name;

        /// <summary>
        /// Priority Order
        /// </summary>
        public int order;

        /// <summary>
        /// Vacation date internal.
        /// </summary>
        public List<DateTime> vacations;

        /// <summary>
        /// Flag that tells there is a vacation internval
        /// </summary>
        public bool hasVacations { get { return vacations == null ? false : vacations.Count >= 2; } }

        /// <summary>
        /// List of stats
        /// </summary>
        public List<MedicShiftStats> stats;

        /// <summary>
        /// Number of holidays shifts
        /// </summary>
        public int holidayCount { get { int c = 0; for (int i = 0;i < stats.Count;i++) c += stats[i].holidayCount; return c; } }

        /// <summary>
        /// List of preferred days of week
        /// </summary>
        public List<DayOfWeek> preferences;

        /// <summary>
        /// Table of shifts count per week day
        /// </summary>
        private Dictionary<DayOfWeek,int> m_weekday_count;
        private Dictionary<int,int> m_month_count;

        /// <summary>
        /// CTOR.
        /// </summary>
        public Medic() {
            m_weekday_count = new Dictionary<DayOfWeek,int>();
            m_month_count = new Dictionary<int,int>();
            stats = new List<MedicShiftStats>();
            for (int i = 0;i <= 12;i++) stats.Add(new MedicShiftStats());
            preferences = new List<DayOfWeek>();
        }

        /// <summary>
        /// Sets the preferences in string format
        /// </summary>
        /// <param name="p_prefs_list"></param>
        public void SetPreferences(IList<string> p_prefs_list) {
            foreach (string it in p_prefs_list) {
                switch (it.ToUpper()) {
                    case "2": preferences.Add(DayOfWeek.Monday); break;
                    case "3": preferences.Add(DayOfWeek.Tuesday); break;
                    case "4": preferences.Add(DayOfWeek.Wednesday); break;
                    case "5": preferences.Add(DayOfWeek.Thursday); break;
                    case "6": preferences.Add(DayOfWeek.Friday); break;
                    case "S": preferences.Add(DayOfWeek.Saturday); break;
                    case "D": preferences.Add(DayOfWeek.Sunday); break;
                }
            }
        }

        /// <summary>
        /// Sets the number of shifts for all months
        /// </summary>
        /// <param name="p_count"></param>
        public void SetShiftsPerMonth(int p_count) {
            for (int i = 0;i < stats.Count;i++) stats[i].totalShifts = p_count;
        }

        /// <summary>
        /// Populate the vacations
        /// </summary>
        /// <param name="p_list"></param>
        public void SetVacations(List<DateTime> p_list) {
            vacations = new List<DateTime>(p_list);
            if (!hasVacations) return;
            DateTime d0 = vacations[0];
            DateTime d1 = vacations[1];
            int td = (d1 - d0).Days;
            DateTime d = d0;
            for (int i = 0;i < td;i++) {
                stats[d.Month].vacations++;
                d.AddDays(1);
            }
        }

        /// <summary>
        /// Given a text pair of dates populates the vacations interval.
        /// </summary>
        /// <param name="p_interval"></param>
        public void SetVacations(IList<string> p_interval) {
            IList<string> vl = p_interval;
            vacations = new List<DateTime>();
            if (vl == null) return;
            if (vl.Count < 2) return;
            DateTime d0 = DateTime.Parse(vl[0]);
            DateTime d1 = DateTime.Parse(vl[1]);
            if (d1 < d0) return;
            SetVacations(new List<DateTime>() { d0,d1 });
        }

        /// <summary>
        /// Check if the date is within vacation interval
        /// </summary>
        /// <param name="p_date"></param>
        /// <returns></returns>
        public bool IsVacation(DateTime p_date) {
            if (vacations.Count < 2) return false;
            if (p_date < vacations[0]) return false;
            if (p_date > vacations[1]) return false;
            return true;
        }

        public override string ToString() {
            string wds = "";
            for (int i = 0;i < 7;i++) wds += $" | {((DayOfWeek)i).GetDayOfWeekLetterPTBR()}: {GetWeekDayShiftCount((DayOfWeek)i).ToString("00")}";
            wds += $" | HD: {holidayCount}";
            return $"{name.PadRight(12)} | s: { GetShiftCount()}/{GetShiftsPerMonth()} {wds} | {GetWeekendShiftRatio().ToString("0.00")}";
        }

        /// <summary>
        /// Get all months shift counts
        /// </summary>
        /// <returns></returns>
        public int GetShiftCount() {
            int c = 0;
            for (int i = 0;i < stats.Count;i++) c += stats[i].count;
            return c;
        }

        /// <summary>
        /// Returns the shifts per month
        /// </summary>
        /// <returns></returns>
        public int GetShiftsPerMonth() {
            return stats[0].totalShifts;
        }

        /// <summary>
        /// Returns the total shifts allowed in all months
        /// </summary>
        /// <returns></returns>
        public int GetTotalShiftsPerMonth() {
            int c = 0;
            for (int i = 0;i < stats.Count;i++) c += stats[i].totalShifts;
            return c;
        }

        /// <summary>
        /// Returns the weekday count for all months
        /// </summary>
        /// <param name="p_weekday"></param>
        /// <returns></returns>
        public int GetWeekDayShiftCount(DayOfWeek p_weekday) {
            int c = 0;
            for (int i = 0;i < stats.Count;i++) c += stats[i].GetCount(p_weekday);
            return c;
        }

        /// <summary>
        /// Returns the ratio of weekends over all shift count.
        /// </summary>
        /// <returns></returns>
        public float GetWeekendShiftRatio() {
            float c0 = GetWeekDayShiftCount(DayOfWeek.Saturday);
            float c1 = GetWeekDayShiftCount(DayOfWeek.Sunday);
            float t = GetShiftCount();
            return t <= 0f ? 0f : Math.Min(c0,c1) / t;
        }

        /// <summary>
        /// Internals
        /// </summary>        
        static Random m_med_rnd = new Random((int)DateTime.Now.Millisecond);
    }

    #endregion

}
