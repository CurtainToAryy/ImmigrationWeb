using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImmigrationWeb
{
    class Program
    {
        static void Main(string[] args)
        {
            string str = AppDomain.CurrentDomain.BaseDirectory + @"\WIKIFX.csv";
            string settingPath = AppDomain.CurrentDomain.BaseDirectory + @"\PathSetting.txt";
            StreamReader fileStream = new StreamReader(settingPath, Encoding.Default);
            string htmlPath = fileStream.ReadToEnd();
            var data = WebHandler.readCsvTxt(str);
            foreach (DataRow dr in data.Rows)
            {
                string url = dr[0].ToString();
                string[] arry = url.Split('/');
                string path = arry[4].Replace(".html","");
                try
                {
                   WebHandler.GetFilterHtml(url, htmlPath + path, htmlPath);
                }
                catch (Exception e)
                {

                    Console.WriteLine(e.Message) ;
                }
            }
        }
    }
}
