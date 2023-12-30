using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MedShiftGen {


    #region class MedShiftGroup

    /// <summary>
    /// Group of shifts spanning a month
    /// </summary>
    public class MedShiftGroup {

        #region static

        #region MedShiftGroup ParseJson
        /// <summary>
        /// Parses the json in the stream into a new shift group instance.
        /// </summary>
        /// <param name="p_json"></param>
        /// <returns></returns>
        static public MedShiftGroup ParseJson(Stream p_json) {

            MedShiftGroup e = new MedShiftGroup();
            StreamReader file_sr = new StreamReader(p_json);
            JsonSerializerSettings json_opt = new JsonSerializerSettings();
            JsonSerializer json_s = JsonSerializer.CreateDefault();
            Dictionary<string,object> shift_table = (Dictionary<string,object>)json_s.Deserialize(file_sr,typeof(Dictionary<string,object>));

            foreach (KeyValuePair<string,object> f_it in shift_table) {
                string k = f_it.Key;
                object v = f_it.Value;
                switch (f_it.Key) {
                    case "start":  e.dateStart = DateTime.Parse((string)v); break;
                    case "end":    e.dateEnd = DateTime.Parse((string)v); break;
                    case "ignore": e.SetIgnoreRules(((JArray)v).ToObject<string[]>()); break;
                    case "quorum": e.minQuorum = (int)(long)v; break;
                    case "medics": {
                        List<Dictionary<string,object>> med_list = ((JArray)v).ToObject<List<Dictionary<string,object>>>();
                        foreach (Dictionary<string,object> med_table in med_list) {
                            Medic m = new Medic();
                            foreach (KeyValuePair<string,object> med_it in med_table) {
                                string mk = med_it.Key;
                                object mv = med_it.Value;
                                switch (mk) {
                                    case "name": m.name = (string)mv; break;
                                    case "shiftPerMonth": m.SetShiftsPerMonth((int)(long)mv); break;
                                    case "order": m.order = (int)(long)mv; break;
                                    case "vacations": {
                                        string[] vacation_interval = ((JArray)mv).ToObject<string[]>();
                                        m.SetVacations(vacation_interval);
                                    }
                                    break;
                                    case "preferences": {
                                        string[] prefs_list = ((JArray)mv).ToObject<string[]>();
                                        m.SetPreferences(prefs_list);
                                    }
                                    break;
                                }
                            }
                            e.AddMedic(m);
                        }
                    }
                    break;
                    case "holidays": {
                        List<Dictionary<string,object>> hld_list = ((JArray)v).ToObject<List<Dictionary<string,object>>>();
                        foreach (Dictionary<string,object> hld_table in hld_list) {
                            HolidayDateTime hd = new HolidayDateTime();
                            foreach (KeyValuePair<string,object> med_it in hld_table) {
                                string mk = med_it.Key;
                                object mv = med_it.Value;
                                switch (mk) {
                                    case "name": hd.name = (string)mv; break;
                                    case "date": hd.date = DateTime.Parse((string)mv); break;
                                }
                            }
                            e.holidays.Add(hd);
                        }
                    }
                    break;
                }
            }
            return e;
        }
        #endregion

        #endregion

        #region Dates

        /// <summary>
        /// Starting Date
        /// </summary>
        public DateTime dateStart;

        /// <summary>
        /// End Date
        /// </summary>
        public DateTime dateEnd;

        /// <summary>
        /// Total shift days
        /// </summary>
        public int totalDays { get { DateTime di = dateStart; DateTime df = dateEnd; return (df - di).Days; } }

        #endregion

        /// <summary>
        /// Shifts minimum quorum
        /// </summary>
        public int minQuorum;

        /// <summary>
        /// List of registered medics
        /// </summary>
        public List<Medic> medics;

        /// <summary>
        /// List of all shifts from this group.
        /// </summary>
        public List<MedShift> shifts;

        /// <summary>
        /// List of ignored shift days
        /// </summary>
        public List<MedShift> ignore;

        /// <summary>
        /// List of holidays.
        /// </summary>
        public List<HolidayDateTime> holidays;

        /// <summary>
        /// Checks if a given date is a holiday.
        /// </summary>
        /// <param name="p_date"></param>
        /// <returns></returns>
        public bool IsHoliday(DateTime p_date) { for (int i = 0;i < holidays.Count;i++) if (holidays[i].date == p_date) return true; return false; }

        /// <summary>
        /// Total number of slots available in this group
        /// </summary>
        public int totalQuorum { get { int s = 0; foreach (MedShift it in shifts) s += it.quorum; return s; } }

        /// <summary>
        /// Total number of available medic shifts from the reigstered medics.
        /// </summary>
        public int totalMedicShifts { get { int s = 0; foreach (Medic it in medics) s += it.GetShiftCount(); return s; } }

        /// <summary>
        /// Total number of available medic shifts from the reigstered medics per month
        /// </summary>
        public int totalMedicShiftPerMonth { get { int s = 0; foreach (Medic it in medics) s += it.GetShiftsPerMonth(); return s; } }

        /// <summary>
        /// CTOR.
        /// </summary>
        public MedShiftGroup() {
            dateStart = DateTime.Now;
            dateEnd = DateTime.Now.AddMonths(1);
            medics = new List<Medic>();
            ignore = new List<MedShift>();
            shifts = new List<MedShift>();
            holidays = new List<HolidayDateTime>();
        }

        /// <summary>
        /// Adds a medic and update its shift count and total
        /// </summary>
        /// <param name="p_medic"></param>
        public void AddMedic(Medic p_medic) {
            Medic m = p_medic;
            if (m == null) return;
            medics.Add(m);
        }

        #region void SetIgnoreRule

        /// <summary>
        /// Given a comma separated rule string update the ignored shifts
        /// </summary>
        /// <param name="p_rules"></param>
        public void SetIgnoreRule(string p_rules) {
            SetIgnoreRules(p_rules.Trim().Split(","));
        }

        /// <summary>
        /// Given a list of rules update the ignored list of shifts
        /// </summary>
        /// <param name="p_rules"></param>
        public void SetIgnoreRules(IList<string> p_rules) {

            IList<string> ignore_list_tks = p_rules;

            List<MedShift> ignore_l = new List<MedShift>();

            foreach (string ignore_it in ignore_list_tks) {
                if (string.IsNullOrEmpty(ignore_it)) continue;
                ShiftRoundFlag rf = ShiftRoundFlag.None;
                int wd = 0;
                switch (ignore_it[0].ToString().ToUpper()) {
                    case "M": rf = ShiftRoundFlag.Day; break;
                    case "N": rf = ShiftRoundFlag.Night; break;
                    case "D": wd = 0; break;
                    case "2": wd = 1; break;
                    case "3": wd = 2; break;
                    case "4": wd = 3; break;
                    case "5": wd = 4; break;
                    case "6": wd = 5; break;
                    case "S": wd = 6; break;
                }

                switch (ignore_it[0].ToString().ToUpper()) {
                    case "M":
                    case "N": {
                        if (rf == ShiftRoundFlag.None) break;
                        string sd = ignore_it.Substring(1);
                        int d = 0;
                        int.TryParse(sd,out d);
                        if (d <= 0) break;
                        if (d > 31) break;
                        MedShift p = new MedShift() { roundType = rf,month = 1,day = d,quorum = 0 };
                        ignore_l.Add(p);
                    }
                    break;

                    case "D":
                    case "2":
                    case "3":
                    case "4":
                    case "5":
                    case "6":
                    case "S": {
                        string rs = ignore_it.Substring(1).ToUpper();
                        if (string.IsNullOrEmpty(rs)) break;
                        rf = rs == "M" ? ShiftRoundFlag.Day : (rs == "N" ? ShiftRoundFlag.Night : ShiftRoundFlag.None);
                        if (rf == ShiftRoundFlag.None) break;
                        int num_dias = totalDays;
                        DateTime d = dateStart;
                        for (int i = 0;i < num_dias;i++) {
                            if ((int)d.DayOfWeek != wd) { d = d.AddDays(1); continue; }
                            MedShift p = new MedShift() { roundType = rf,month = d.Month,day = d.Day,year = d.Year,quorum = 0 };
                            ignore_l.Add(p);
                            d = d.AddDays(1);
                        }
                    }
                    break;
                }
            }

            ignore = new List<MedShift>(ignore_l);

        }

        #endregion

        #region CRUD

        /// <summary>
        /// Returns the amount of shifts for a given month
        /// </summary>
        /// <param name="p_month"></param>
        /// <returns></returns>
        public int GetShiftCount(int p_month) {
            int c = 0;
            DateTime d0 = dateStart;
            DateTime d1 = dateEnd;
            int td = (d1 - d0).Days;
            DateTime d = d0;
            for (int i = 0;i < td;i++) {
                if (d.Month > p_month) break;
                if (d.Month == p_month) {
                    c += FindShift(d).Count;
                }
                d = d.AddDays(1);
            }
            return c;
        }

        /// <summary>
        /// Get the quorum count for a given month
        /// </summary>
        /// <param name="p_month"></param>
        /// <returns></returns>
        public int GetShiftQuorum(int p_month) {
            int c = 0;
            for (int i = 0;i < shifts.Count;i++) if (shifts[i].month == p_month) c += shifts[i].quorum;
            return c;
        }

        /// <summary>
        /// Clear all shifts
        /// </summary>
        public void Clear() {
            shifts.Clear();
        }

        #region RegisterMedics

        /// <summary>
        /// Given a pool of medics, sort based on the sort method and register into the shift
        /// </summary>
        /// <param name="p_shift"></param>
        /// <param name="p_medics"></param>
        /// <param name="p_sort"></param>
        internal void RegisterMedicPool(MedShift p_shift,List<Medic> p_medics,Comparison<Medic> p_sort) {
            MedShift ms = p_shift;
            if (ms == null) return;
            if (ms.isFull)  return;
            if (!ms.isOpen) return;
            List<Medic> ml = p_medics;

            if (p_sort != null) ml.Sort(p_sort);

            for (int i = 0;i < ml.Count;i++) {
                Medic m = ml[i];
                MedicShiftStats mstats = m.stats[ms.month];
                if (ms.medics.Contains(m)) continue;
                if (!mstats.available) continue;
                if (!ms.IsAllowed(m)) continue;
                if (ms.isHoliday) mstats.IncrementHoliday();
                mstats.Increment(ms.dayOfWeek);
                ms.medics.Add(m);
                break;
            }

            ms.medics.Sort(Medic.SortByName);

            //Following Shift Rules
            MedShift next_ms;
            //If night shift block the following day shift
            if (ms.roundType == ShiftRoundFlag.Night) {
                next_ms = FindNextShift(ms,ShiftRoundFlag.Day,ms.dayOfWeek.Next());
                if (next_ms != null) next_ms.Block(ms.medics);
            }
            //If day shift block the following night shift
            if (ms.roundType == ShiftRoundFlag.Day) {
                next_ms = FindNextShift(ms,ShiftRoundFlag.Night,ms.dayOfWeek);
                if (next_ms != null) next_ms.Block(ms.medics);
            }

            switch (ms.dayOfWeek) {

                case DayOfWeek.Saturday: {
                    if (ms.roundType == ShiftRoundFlag.Day) {
                        //If saturday day block the following saturday day
                        next_ms = FindNextShift(ms);
                        if (next_ms != null) next_ms.Block(ms.medics);
                        //Block the 2 following weekends days to prioritize nights
                        for (int wi = 0;wi < 2;wi++) { next_ms = FindNextShift(next_ms); if (next_ms != null) next_ms.Block(ms.medics); }
                        //Block following night shift
                        next_ms = FindNextShift(ms,ShiftRoundFlag.Night);
                        if (next_ms != null) next_ms.Block(ms.medics);
                    }
                    //Move medics to following sunday same round
                    next_ms = FindNextShift(ms,DayOfWeek.Sunday);
                    if (next_ms != null) {
                        RegisterMedics(next_ms,ms.medics);
                    }
                }
                break;

                case DayOfWeek.Sunday: {
                    if (ms.roundType == ShiftRoundFlag.Day) {
                        //If sunday day block the following sunday day
                        next_ms = FindNextShift(ms);
                        if (next_ms != null) next_ms.Block(ms.medics);
                        //Block the 2 following weekends days to prioritize nights
                        for (int wi = 0;wi < 2;wi++) { next_ms = FindNextShift(next_ms); if (next_ms != null) next_ms.Block(ms.medics); }
                        //Block following night shift
                        next_ms = FindNextShift(ms,ShiftRoundFlag.Night);
                        if (next_ms != null) next_ms.Block(ms.medics);
                    }
                }
                break;
            }

        }

        /// <summary>
        /// Directly register the medics in list if appropriate
        /// </summary>
        /// <param name="p_shift"></param>
        /// <param name="p_medics"></param>
        internal void RegisterMedics(MedShift p_shift,IList<Medic> p_medics) {

            MedShift ms = p_shift;
            if (ms == null)  return;
            if (ms.isFull)   return;
            if (!ms.isOpen)  return;
            IList<Medic> ml = p_medics;

            for (int i = 0;i < ml.Count;i++) {
                Medic m = ml[i];
                MedicShiftStats mstats = m.stats[ms.month];
                if (ms.medics.Contains(m)) continue;
                if (!mstats.available) continue;
                if (!ms.IsAllowed(m)) continue;
                if (ms.isHoliday) mstats.IncrementHoliday();
                mstats.Increment(ms.dayOfWeek);
                ms.medics.Add(m);
            }

            ms.medics.Sort(Medic.SortByName);
        }

        #endregion

        #endregion

        /// <summary>
        /// Generate the shift list.
        /// </summary>
        public void Generate(int p_month) {

            DateTime d;

            //Date iterator start
            DateTime d0 = dateStart;
            DateTime d1 = dateEnd;

            if (p_month < d0.Month) return;

            //Advance to chosen month
            d = d0; while (d.Month != p_month) { d = d.AddDays(1); }
            d0 = d;
            d = d0; while (d.Month == p_month) { d = d.AddDays(1); }
            d1 = d;

            //Desired rounds to be generated
            ShiftRoundFlag[] tl = new ShiftRoundFlag[] { ShiftRoundFlag.Day,ShiftRoundFlag.Night };

            List<Medic> ml;

            //Generate base shift list
            int td = (d1 - d0).Days;
            d = d0;
            for (int i = 0;i < td;i++) {
                foreach (ShiftRoundFlag t in tl) {
                    if (IsShiftIgnored(t,d)) continue;
                    MedShift p = new MedShift() {
                        roundType = t,
                        month  = d.Month,
                        day    = d.Day,
                        year   = d.Year,
                        quorum = minQuorum
                    };
                    if (IsHoliday(p.date)) {
                        p.isHoliday = true;
                        //For now holidays are manually assigned
                        p.isOpen    = false;
                    }
                    shifts.Add(p);
                }
                d = d.AddDays(1);
            }

            //Sort by day and round flags
            shifts.Sort(MedShift.SortByDayAndRound);

            //Mark Shift Vacations and reset counts
            for (int i = 0;i < medics.Count;i++) {
                Medic m = medics[i];
                if (!m.hasVacations) continue;
                DateTime vd0 = m.vacations[0];
                DateTime vd1 = m.vacations[1];
                int vtd = (vd1 - vd0).Days;
                DateTime vd = d0;
                for (int vi = 0;vi < vtd;vi++) {
                    List<MedShift> msl = FindShift(vd);
                    foreach (MedShift ms in msl) { ms.Block(m); }
                    vd = vd.AddDays(1);
                }
            }

            //List of holidays fridays and weekends
            List<DayOfWeek> mandatory_group = new List<DayOfWeek>() { (DayOfWeek)(100),DayOfWeek.Friday,DayOfWeek.Saturday,DayOfWeek.Sunday };
            //First Holidays and Friday and Weekends
            Generate(p_month,mandatory_group);
            //Next preferences sorted by priority
            ml = new List<Medic>(medics);
            ml.Sort(Medic.SortByPriority);
            //Iterate medics
            for(int i=0;i<ml.Count;i++) {
                Medic m = ml[i];
                //If no preferences skip
                if (m.preferences.Count <= 0) continue;
                //Fetch shifts matching preferences
                List<MedShift> prefs_list = FindShift(m.preferences.ToArray());
                //If no shifts available skip
                if (prefs_list.Count <= 0) continue;
                for(int j=0;j<prefs_list.Count;j++) {
                    MedShift ms = prefs_list[j];
                    RegisterMedics(ms,new Medic[] { m });
                }
            }
            //List of weekdays
            List<DayOfWeek> weekday_normal = new List<DayOfWeek>() { DayOfWeek.Monday,DayOfWeek.Tuesday,DayOfWeek.Wednesday,DayOfWeek.Thursday };
            Random rnd = new Random(DateTime.Now.Millisecond);
            for (int i = 0;i < weekday_normal.Count;i++) weekday_normal.Sort(delegate (DayOfWeek a,DayOfWeek b) { return rnd.NextDouble() < 0.5f ? -1 : 1; });
            //Last randomized weekdays
            Generate(p_month,weekday_normal);

            //Sort into the proper display format
            // mm/dd/yyyy R M1 M2            
            shifts.Sort(MedShift.SortByDayAndRound);

        }

        /// <summary>
        /// Given a month and weekdays update the shifts data
        /// </summary>
        /// <param name="p_month"></param>
        /// <param name="p_weekdays"></param>
        private void Generate(int p_month,IList<DayOfWeek> p_weekdays) {

            List<Medic> ml;

            for (int k = 0;k < p_weekdays.Count;k++) {

                DayOfWeek wdp = p_weekdays[k];

                bool use_priority = false;
                switch (wdp) {
                    case DayOfWeek.Monday:
                    case DayOfWeek.Tuesday:
                    case DayOfWeek.Wednesday:
                    case DayOfWeek.Thursday: use_priority = true; break;
                }

                Comparison<Medic> med_sort = Medic.GetSortMethod(use_priority,p_month,wdp);

                for (int i = 0;i < shifts.Count;i++) {
                    MedShift ms = shifts[i];
                    if (ms.month != p_month) continue;
                    if (!ms.MatchDayOfWeek(wdp)) continue;
                    if (ms.isFull) continue;

                    //Fetch medics and random sort
                    ml = new List<Medic>(medics);

                    //Console.WriteLine($">>>  [{ms.roundTypePrefix}] {ms.date}");
                    //for (int mi = 0;mi < ml.Count;mi++) Console.WriteLine($"   {ml[mi]}");

                    //Add medic per medshift quorum
                    for (int j = 0;j < ms.quorum;j++) {
                        RegisterMedicPool(ms,ml,med_sort);
                    }

                }

            }
        }

        #region Find

        /// <summary>
        /// Searches for the next round matching shift.
        /// </summary>
        /// <param name="p_current"></param>
        /// <returns></returns>
        public MedShift FindNextShift(MedShift p_current,ShiftRoundFlag p_round,DayOfWeek p_week_day) {
            MedShift c = p_current;
            if (c == null) return null;
            int idx = shifts.IndexOf(c);
            if (idx < 0) return null;
            for (int i = idx + 1;i < shifts.Count;i++) {
                MedShift ms = shifts[i];
                ShiftRoundFlag match_rf = p_round <= 0 ? c.roundType : p_round;
                if (ms.roundType != match_rf) continue;
                DayOfWeek match_wd = p_week_day < 0 ? c.dayOfWeek : p_week_day;
                if (ms.dayOfWeek != match_wd) continue;
                return ms;
            }
            return null;
        }

        public MedShift FindNextShift(MedShift p_current,DayOfWeek p_week_day) {
            MedShift c = p_current;
            if (c == null) return null;
            return FindNextShift(p_current,p_current.roundType,p_week_day);
        }

        public MedShift FindNextShift(MedShift p_current,ShiftRoundFlag p_round) {
            MedShift c = p_current;
            if (c == null) return null;
            return FindNextShift(p_current,p_round,c.dayOfWeek);
        }

        public MedShift FindNextShift(MedShift p_current) {
            MedShift c = p_current;
            if (c == null) return null;
            return FindNextShift(p_current,c.roundType,c.dayOfWeek);
        }

        public List<MedShift> FindShift(ShiftRoundFlag p_round,params DayOfWeek[] p_week_days) {
            List<MedShift> res = new List<MedShift>();
            List<DayOfWeek> ddsl = new List<DayOfWeek>(p_week_days);
            foreach (MedShift p in shifts) {
                if (p_round != ShiftRoundFlag.None) if (p.roundType != p_round) continue;
                bool is_dds = ddsl.Count <= 0 ? true : ddsl.Contains(p.date.DayOfWeek);
                if (!is_dds) continue;
                res.Add(p);
            }
            return res;
        }

        public List<MedShift> FindShift(params DayOfWeek[] p_week_days) {
            return FindShift(ShiftRoundFlag.None,p_week_days);
        }

        public List<MedShift> FindShift(string p_name,int p_month,ShiftRoundFlag p_round,params DayOfWeek[] p_week_days) {
            List<MedShift> res = new List<MedShift>();
            List<DayOfWeek> ddsl = new List<DayOfWeek>(p_week_days);
            foreach (MedShift p in shifts) {
                if (p_month > 0) if (p.month != p_month) continue;
                if (p_round != ShiftRoundFlag.None) if (p.roundType != p_round) continue;
                bool is_wd = ddsl.Count <= 0 ? true : ddsl.Contains(p.date.DayOfWeek);
                if (is_wd)
                    foreach (Medic m in p.medics) {
                        if (m.name != p_name) continue;
                        res.Add(p);
                    }
            }
            return res;
        }

        public List<MedShift> FindShift(string p_name,ShiftRoundFlag p_round,params DayOfWeek[] p_week_days) {
            return FindShift(p_name,0,p_round,p_week_days);
        }

        public List<MedShift> FindShift(string p_name,params DayOfWeek[] p_week_days) {
            return FindShift(p_name,ShiftRoundFlag.None,p_week_days);
        }

        public MedShift FindShift(ShiftRoundFlag p_round_flag,DateTime p_date) {
            foreach (MedShift it in shifts) if (it.roundType == p_round_flag) if (it.date == p_date) return it;
            return null;
        }

        public List<MedShift> FindShift(DateTime p_date) {
            List<MedShift> res = new List<MedShift>();
            foreach (MedShift it in shifts) if (it.date == p_date) res.Add(it);
            return res;
        }

        public bool HasShift(DateTime p_date) {
            foreach (MedShift it in shifts) if (it.date == p_date) return true;
            return false;
        }

        public bool HasMonth(int p_month) {
            foreach (MedShift it in shifts) if (it.month == p_month) return true;
            return false;
        }

        #endregion

        public bool IsShiftIgnored(ShiftRoundFlag p_round_type,DateTime p_date) {
            bool f = false;
            foreach (MedShift it in ignore) {
                if (IsHoliday(p_date)) continue;
                if (it.roundType != p_round_type) continue;
                if (it.month != p_date.Month) continue;
                if (it.day != p_date.Day) continue;
                f = true;
                break;
            }
            return f;
        }

        public string GetYearMonthString() {
            string ms = dateStart.ToString("MMMM yyyy").ToUpper();
            return $"{ms}";
        }

        #region void Log

        /// <summary>
        /// Generates a log of the shift group.
        /// </summary>
        public void Log(params DayOfWeek[] p_filter) {

            Console.WriteLine("Gerador de Escala v1.0");
            Console.WriteLine(" ");

            List<DayOfWeek> flt = new List<DayOfWeek>(p_filter);

            int c = 0;
            int k = 0;

            DateTime d0 = dateStart;
            DateTime d1 = dateEnd;

            DateTime d = d0;
            while (d < d1) {

                string log = "";

                int msc = 0;

                log += $"Escala: {d.ToString("MMMM yyyy").ToUpper()}\n";
                log += $" \n";

                bool is_2a = false;
                for (int i = 0;i < shifts.Count;i++) {
                    MedShift p = shifts[i];
                    if (p.month != d.Month) continue;

                    if (i > 0) if (!is_2a) if (p.date.DayOfWeek == DayOfWeek.Monday) { is_2a = true; log += ("\n"); }
                    if (p.date.DayOfWeek != DayOfWeek.Monday) is_2a = false;

                    bool match_flt = flt.Count <= 0 ? true : (flt.Contains(p.dayOfWeek));
                    if (match_flt) {
                        log += ($"{p}\n");
                        msc++;
                    }
                }



                log += ($"\n=== Resumo ===\n\n");
                log += ($"Plantoes Totais: {GetShiftCount(d.Month)}\n");
                log += ($"Vagas Totais:    {GetShiftQuorum(d.Month)}\n");
                log += ($"Quorum Medico:   {totalMedicShiftPerMonth} plantoes\n");

                log += ($"\n=== Equipe ===\n\n");

                c = 0;
                k = 0;
                foreach (Medic m in medics) {
                    MedicShiftStats mstats = m.stats[d.Month];
                    int ms_count = mstats.count;
                    int ms_sat_c = mstats.GetCount(DayOfWeek.Saturday);
                    int ms_sun_c = mstats.GetCount(DayOfWeek.Sunday);
                    int ms_wkd_pct = (int)(mstats.GetWeekendRatio() * 100f);
                    log += ($"#{((k++) + 1).ToString("00")} {m.name.PadRight(12)} : {ms_count.ToString("00")}/{mstats.totalShifts.ToString("00")} plantoes | {ms_sat_c} Sab | {ms_sun_c} Dom\n");
                    c += ms_count;
                }


                log += ($"\n------------------------------------------\n\n");

                if (msc > 0) Console.WriteLine(log);

                do { d = d.AddDays(1); } while (d.Day != 1);
            }



            Console.WriteLine($"\n=== Resumo Completo ===\n");

            Console.WriteLine($"Plantoes Totais: {shifts.Count}");
            Console.WriteLine($"Vagas Totais:    {totalQuorum}");
            Console.WriteLine($"Medicos Totais:  {medics.Count}");
            Console.WriteLine($"Quorum Medico:   {totalMedicShifts} plantoes");

            Console.WriteLine($"\n=== Equipe ===\n");

            c = 0;
            k = 0;
            foreach (Medic m in medics) {
                int ms_count = m.GetShiftCount();
                int ms_sat_c = m.GetWeekDayShiftCount(DayOfWeek.Saturday);
                int ms_sun_c = m.GetWeekDayShiftCount(DayOfWeek.Sunday);
                int ms_wkd_pct = (int)Math.Round(m.GetWeekendShiftRatio() * 100f);
                Console.WriteLine($"#{((k++) + 1).ToString("00")} {m.name.PadRight(12)} : {ms_count.ToString("000")}/{m.GetTotalShiftsPerMonth().ToString("00")} plantoes | {ms_sat_c} Sab | {ms_sun_c} Dom | Fds {ms_wkd_pct} %");
                c += ms_count;
            }

            Console.WriteLine($"----- Total {c} plantoes");

        }

        #endregion

        #region string ToCSV

        /// <summary>
        /// Returns the CSV of this shift group for a 7 by 42 cell grid
        /// </summary>
        /// <returns></returns>
        public string ToCSV(int p_month) {
            //CSV
            StringBuilder sb = new StringBuilder();
            //Shift Group start
            DateTime d0 = new DateTime(dateStart.Year,p_month,1);
            //Find previous monday
            while (d0.DayOfWeek != DayOfWeek.Monday) { d0 = d0.AddDays(-1); }

            DateTime d1 = new DateTime(dateStart.Year,p_month,1).AddMonths(1);

            //Find last sunday
            while (d1.DayOfWeek != DayOfWeek.Sunday) { d1 = d1.AddDays(1); }

            int td = (d1 - d0).Days;

            ShiftRoundFlag[] rfl = new ShiftRoundFlag[] { ShiftRoundFlag.Day,ShiftRoundFlag.Night };

            DateTime d = d0;

            int data_idx = 0;

            for (int y = 0;y < 43;y++) {
                if (y > 0) sb.Append("\n");
                for (int x = 0;x < 13;x++) {

                    if (x > 0) sb.Append(",");

                    string sector = "";

                    if (x >= 0) if (x < 7) if (y >= 0) if (y < 43) sector = "shifts";
                    if (x >= 8) if (x < 13) if (y >= 0) if (y < 15) sector = "team";
                    if (x >= 8) if (x < 13) if (y >= 16) if (y < 22) sector = "summary";

                    string cell_type = "";

                    switch (sector) {

                        #region case "shifts"
                        //Medic / Dates / Shifts Region
                        case "shifts": {
                            //Starts as 'data'
                            cell_type = "data";
                            //Each 7th row is the date header
                            if (y % 7 == 0) cell_type = "date-header";
                            //Week offset 1 - 2 - 3 - 4 - 5
                            int week_index = y / 6;
                            //Print by cell type
                            switch (cell_type) {

                                #region case "date-header"
                                //Write the current data
                                case "date-header": {
                                    int day_delta = x + (week_index * 7);
                                    DateTime hd = d.AddDays(day_delta);
                                    bool month_match = p_month <= 0 ? true : (hd.Month == p_month);
                                    string hds = (hd.Month == p_month) ? hd.ToString("dd/MM") : " ";
                                    if (IsHoliday(hd)) if(hds != " ") hds = $"* {hds} *";
                                    sb.Append(hds);
                                }
                                break;
                                #endregion

                                #region case "data"
                                //Write the medic shift data
                                case "data": {
                                    //Which week the cell data is
                                    int data_week_index = data_idx / 42;
                                    //Which shift round it is
                                    int data_round_index = (data_idx / 21) % 2;
                                    //Round index to enumeration
                                    ShiftRoundFlag rf = ShiftRoundFlag.None;
                                    switch (data_round_index) {
                                        case 0: rf = ShiftRoundFlag.Day; break;
                                        case 1: rf = ShiftRoundFlag.Night; break;
                                    }
                                    //Which month day
                                    int data_day_index = x + data_week_index * 7;
                                    //Which medic
                                    int data_medic_index = (data_idx / 7) % 3;
                                    //Fetch date from day
                                    DateTime dd = d.AddDays(data_day_index);
                                    //Find shift
                                    MedShift ms = FindShift(rf,dd);
                                    //Defaults to empty
                                    string data_value = " ";
                                    //If there is a shift
                                    if (dd.Month == p_month)
                                        if (ms != null) {
                                            //Fetch medic name otherwise fill with '-'
                                            if (data_medic_index < ms.medics.Count) data_value = ms.medics[data_medic_index].name; else data_value = "-";
                                        }
                                    //Print data
                                    sb.Append(data_value);
                                    //Increment data iterator
                                    data_idx++;
                                }
                                break;
                                #endregion

                            }
                        }
                        break;
                        #endregion

                        #region team

                        case "team": {
                            int column_index = (x - 8) % 5;
                            int row_index = y;

                            Medic m = row_index < medics.Count ? medics[row_index] : null;

                            if (m == null) {
                                sb.Append(" ");
                                break;
                            }

                            MedicShiftStats mstats = m.stats[p_month];

                            int ms_count = mstats.count;
                            int ms_sat_c = mstats.GetCount(DayOfWeek.Saturday);
                            int ms_sun_c = mstats.GetCount(DayOfWeek.Sunday);

                            switch (column_index) {
                                case 0: sb.Append(row_index + 1); break;
                                case 1: sb.Append(m.name); break;
                                case 2: sb.Append($"{ms_count.ToString("00")}/{m.GetShiftsPerMonth().ToString("00")}"); break;
                                case 3: sb.Append($"{ms_sat_c.ToString("0")}"); break;
                                case 4: sb.Append($"{ms_sun_c.ToString("0")}"); break;
                                default: sb.Append(" "); break;
                            }

                        }
                        break;

                        #endregion

                        #region summary

                        //Shift summary data
                        case "summary": {
                            int column_index = (x - 8) % 5;
                            int row_index = y - 16;

                            switch (row_index) {
                                //Title
                                case 0: sb.Append(column_index == 0 ? "Resumo" : " "); break;
                                //Shift Year Month
                                case 1: {
                                    switch (column_index) {
                                        case 0: sb.Append("Escala:"); break;
                                        case 2: sb.Append($"{new DateTime(d0.Year,p_month,1).ToString("MMMM yyyy").ToUpper()}"); break;
                                        default: sb.Append(" "); break;
                                    }
                                }
                                break;
                                //Total Shifts
                                case 2: {
                                    switch (column_index) {
                                        case 0: sb.Append("Plantoes Totais: "); break;
                                        case 2: sb.Append($"{GetShiftCount(p_month)}"); break;
                                        default: sb.Append(" "); break;
                                    }
                                }
                                break;
                                //Total Quorum
                                case 3: {
                                    switch (column_index) {
                                        case 0: sb.Append("Vagas Totais: "); break;
                                        case 2: sb.Append($"{GetShiftQuorum(p_month)}"); break;
                                        default: sb.Append(" "); break;
                                    }
                                }
                                break;
                                //Total Medics
                                case 4: {
                                    switch (column_index) {
                                        case 0: sb.Append("Medicos Totais: "); break;
                                        case 2: sb.Append($"{medics.Count}"); break;
                                        default: sb.Append(" "); break;
                                    }
                                }
                                break;
                                //Total Medic Quorum
                                case 5: {
                                    switch (column_index) {
                                        case 0: sb.Append("Quorum Medico: "); break;
                                        case 2: sb.Append($"{totalMedicShiftPerMonth} plantoes"); break;
                                        default: sb.Append(" "); break;
                                    }
                                }
                                break;
                            }

                        }
                        break;

                        #endregion

                        default: {
                            sb.Append(" ");
                        }
                        break;

                    }

                }
            }

            string res = sb.ToString();

            return res;
        }

        #endregion

    }

    #endregion


}