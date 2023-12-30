using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MedShiftGen {

    class Program {

        static int Main(string[] args) {

            MedShiftGroup e = new MedShiftGroup();

            //args = new string[] { "-f","../../debug.json","-o","../../debug.csv" };
            args = new string[] { "-f","../../debug.json","" };

            string input_file = "";
            StreamWriter output_sw  = null;
            bool         is_debug   = false;
            
            string a_state = "";

            foreach (string a in args) {

                switch (a) {                    
                    case "-f":   { a_state = "-f";   } continue;
                    case "-o":   { a_state = "-o";   } continue;
                    case "-log": { is_debug = true;  } continue;
                    default: { } break;
                }

                switch (a_state) {

                    case "-o": {
                        string fp = a;
                        if (string.IsNullOrEmpty(fp)) fp = input_file + ".csv";
                        DirectoryInfo app_dir = new DirectoryInfo(Environment.CurrentDirectory);
                        FileInfo file_info = new FileInfo($"{app_dir.FullName}/{a}");
                        FileStream fs = null;
                        try {                            
                            fs = file_info.Open(FileMode.Create);
                        }
                        catch (System.Exception) {
                            Console.WriteLine($"Failed to Open Output File [{fp}]");
                            return 1;
                        }
                        output_sw = new StreamWriter(fs);
                    }
                    break;

                    case "-f": {
                        string fp = a;
                        input_file = fp;
                        DirectoryInfo app_dir = new DirectoryInfo(Environment.CurrentDirectory);
                        FileInfo file_info = new FileInfo($"{app_dir.FullName}/{a}");
                        if (!file_info.Exists) {
                            Console.WriteLine($" Arquivo [{file_info.FullName}] nao encontrado!");
                            return 1;
                        }
                        FileStream file = File.OpenRead(file_info.FullName);
                        e = MedShiftGroup.ParseJson(file);
                    }
                    break;

                }
                if (!string.IsNullOrEmpty(a_state)) {
                    a_state = "";
                    continue;
                }

            }

            e.Clear();
            //Reverse generate for ahead-of-time rules to be applied
            for(int i=1;i<=12;i++) e.Generate(13-i);
            //e.Generate(11);

            if(is_debug) {
                e.Log();
            }
            else {
                if(output_sw==null) {

                    DateTime d0 = e.dateStart;
                    DateTime d1 = e.dateEnd;

                    DateTime d = d0;
                    while(d<=d1) {

                        if(e.HasMonth(d.Month)) {

                            string fp = $"{input_file}.{d.ToString("MM")}{d.ToString("MMM")}.csv";

                            DirectoryInfo app_dir = new DirectoryInfo(Environment.CurrentDirectory);
                            FileInfo file_info = new FileInfo($"{app_dir.FullName}/{fp}");

                            FileStream fs = null;
                            try {
                                fs = file_info.Open(FileMode.Create);
                            } catch (System.Exception) {
                                Console.WriteLine($"Failed to Open Output File [{fp}]");                                
                            }
                            if (fs == null) continue;

                            output_sw = new StreamWriter(fs);
                            output_sw.Write(e.ToCSV(d.Month));
                            output_sw.Flush();
                            output_sw.Close();

                        }

                        d = d.AddMonths(1);
                    }
                    
                }
                else {
                    output_sw.Write(e.ToCSV(e.dateStart.Month));
                    output_sw.Flush();
                    output_sw.Close();
                }                
            }

            return 0;


        }
    }
}
