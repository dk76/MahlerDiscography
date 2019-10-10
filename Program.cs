using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace dotnetcoreconsole
{

    class Record
    {
        public string Name { get; set; } = "";
        public string Year { get; set; } = "";

        public string Conductor { get; set; } = "";

        public string Orchestra { get; set; } = "";

        public string Version { get; set; } = "";

        public string Label { get; set; } = "";

        public string RecDate { get; set; } = "";

        public string Soloists { get; set; } = "";

        public string Live { get; set; } = "";
        public string Image { get; set; } = "";

    }

    class ColumnIndexes
    {
        public int YearIndex = -1;
        public int ConductorIndex = -1;

        public int OrchestraIndex = -1;
        public int VersionIndex = -1;
        public int LabelIndex = -1;
        public int RecDateIndex = -1;

        public int SoloistIndex = -1;

        public int LiveIndex = -1;

    }

    class ConcurentList
    {
        List<Record> _items = new List<Record>();
        object sync = new Object();

        public void addItem(Record r)
        {
            lock (sync)
            {
                _items.Add(r);
            }
        }

        public void addItems(List<Record> l)
        {
            lock (sync)
            {
                _items.AddRange(l);

            }
        }

        public Record[] ToArray()
        {
            lock (sync)
                return _items.ToArray();

        }

    }

    static class ExtendStaticClass
    {

        public static string deleteTags(this string s)
        {
            s = s.Replace("<td>", "").Replace("</td>", "").Replace("<p>", "").Replace("</p>", " ").Replace("<br />", " ").Trim();
            Regex r = new Regex("<td(.|\\n)*?>");
            var res = r.Match(s);
            if (res.Success)
                return s.Replace(res.Value, "");
            return s;
        }

    }

    class Program
    {

        static string catalogURL = "https://mahlerfoundation.info/index.php/discography2";
        static string domainURL = "https://mahlerfoundation.info";
        static async Task<string> LoadPage(string url)
        {
            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();
            return responseBody;
        }
        async static Task<String[]> LoadUrls()
        {
            var l = new List<string>();
            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(catalogURL);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();
            Regex r = new Regex("<td headers=\"categorylist_header_title\" class=\"list-title\">(.|\\n)*?</td>");
            var res = r.Match(responseBody);
            while (res.Success)
            {
                Regex r1 = new Regex("<a href=\"(.|\\n)*?\">");
                var res1 = r1.Match(res.Value);
                if (res1.Success)
                {
                    var s = domainURL + res1.Value.
                    Replace("<a href=\"", "").
                    Replace("\">", "").
                    Trim();
                    Console.WriteLine(s);
                    l.Add(s);

                }
                res = res.NextMatch();
            }

            return l.ToArray();
        }

        static string getName(string body)
        {
            if (body == null) return "";

            Regex r = new Regex("<h2 itemprop=\"headline\">(.|\\n)*?</h2>");
            var res = r.Match(body);

            if (res.Success)
            {
                return res.Value.Replace("<h2 itemprop=\"headline\">", "").Replace("</h2>", "").Trim();
            }
            return "";
        }
        static (string name, string image) extractConductorAndImage(string s)
        {
            (string name, string image) t = ("", "");
            Regex r = new Regex("<img(.|\\n)*?/>");
            var res = r.Match(s);
            if (res.Success)
            {
                t.image = res.Value;
                t.name = s.Replace(t.image, "").Trim();
            }
            else
                t.name = s;
            return t;
        }
        static Record getRecord(string s, ColumnIndexes ci)
        {
            if (s == null) return null;
            Regex r = new Regex("<td(.|\\n)*?</td>");
            var res = r.Match(s);
            var record = new Record();
            int cnt = 0;
            while (res.Success)
            {
                if (cnt == ci.YearIndex) record.Year = res.Value.deleteTags();
                else
                if (cnt == ci.ConductorIndex) record.Conductor = res.Value.deleteTags();
                else
                if (cnt == ci.LabelIndex) record.Label = res.Value.deleteTags();
                else
                if (cnt == ci.LiveIndex) record.Live = res.Value.deleteTags();
                else
                if (cnt == ci.OrchestraIndex) record.Orchestra = res.Value.deleteTags();
                else
                if (cnt == ci.RecDateIndex) record.RecDate = res.Value.deleteTags();
                else
                if (cnt == ci.SoloistIndex) record.Soloists = res.Value.deleteTags();
                else
                if (cnt == ci.VersionIndex) record.Version = res.Value.deleteTags();
                cnt++;
                res = res.NextMatch();
            }
            if ((record.Conductor != null) && (record.Conductor != ""))
            {
                var t = extractConductorAndImage(record.Conductor);
                record.Conductor = t.name.Replace("/", "");
                record.Image = t.image.Replace("/images/", domainURL + "/images/");
            }
            return record;
        }

        static ColumnIndexes getColumnIndexes(string header)
        {
            var c = new ColumnIndexes();
            Regex r = new Regex("<td(.|\\n)*?</td>");
            var res = r.Match(header);
            int index = 0;
            while (res.Success)
            {
                if (res.Value.IndexOf("Conductor") >= 0)
                    c.ConductorIndex = index;
                else
                    if (res.Value.IndexOf("Soloist") >= 0)
                    c.SoloistIndex = index;
                else
                    if (res.Value.IndexOf("Version") >= 0)
                    c.VersionIndex = index;
                else
                    if (res.Value.IndexOf("Label") >= 0)
                    c.LabelIndex = index;
                else
                    if (res.Value.IndexOf("Live") >= 0)
                    c.LabelIndex = index;
                else
                    if (res.Value.IndexOf("Rec") >= 0)
                    c.RecDateIndex = index;
                else
                    if (res.Value.IndexOf("Year") >= 0)
                    c.YearIndex = index;
                else
                    if (res.Value.IndexOf("Orchestra") >= 0)
                    c.OrchestraIndex = index;
                index++;
                res = res.NextMatch();
            }
            return c;
        }

        static List<Record> getItems(string body, string name)
        {
            var l = new List<Record>();
            if (body == null) return l;
            Regex r = new Regex("<tr>(.|\n)*?</tr>");
            var res = r.Match(body);
            if (!res.Success) return l;
            var ci = getColumnIndexes(res.Value);
            res = res.NextMatch();
            while (res.Success)
            {
                var record = getRecord(res.Value, ci);
                record.Name = name;
                if ((record != null) && (record.Name != ""))
                    l.Add(record);
                res = res.NextMatch();
            }
            return l;
        }

        static string ConvertToSql(Record[] a)
        {
            var s = "";
            foreach (var r in a)
            {
                s += " Insert into z_items(Name,Year,Conductor,Orchestra,Version,Label,RecDate,Soloists,Live,Image) values(";
                s += $"'{r.Name.Replace("'", "''")}','{r.Year.Replace("'", "''")}','{r.Conductor.Replace("'", "''")}','{r.Orchestra.Replace("'", "''")}','{r.Version.Replace("'", "''")}','{r.Label.Replace("'", "''")}','{r.RecDate.Replace("'", "''")}','{r.Soloists.Replace("'", "''")}','{r.Live.Replace("'", "''")}','{r.Image.Replace("'", "''")}'";
                s += ")\n";
            }
            return s;
        }

        static void Main(string[] args)
        {
            var items = new ConcurentList();
            var urls_task = LoadUrls();
            var urls = urls_task.Result;
            Parallel.For(0, urls.Length, (index) => { Console.WriteLine($"index={index} url={urls[index]}"); var s = LoadPage(urls[index]); var l = getItems(s.Result, getName(s.Result)); items.addItems(l); });
            var a = items.ToArray();
            var js = JsonSerializer.Serialize<Record[]>(a);
            System.IO.File.WriteAllText("data.json", js);
            System.IO.File.WriteAllText("script.sql", ConvertToSql(a));
            Console.WriteLine("End");
            Console.ReadLine();
        }

    }
}
