using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
namespace ImmigrationWeb
{
    public class WebHandler
    {
        //根据Url地址得到网页的html源码 
        public static string GetWebContent(string Url)
        {
            string strResult = "";
            try
            {
                //System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
                //声明一个HttpWebRequest请求 
                request.Timeout = 30000;
                //设置连接超时时间 
                request.Headers.Set("Pragma", "no-cache");
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream streamReceive = response.GetResponseStream();
                Encoding encoding = Encoding.GetEncoding("utf-8");
                StreamReader streamReader = new StreamReader(streamReceive, encoding);
                strResult = streamReader.ReadToEnd();
            }
            catch (Exception ex)
            {
                return strResult = ex.Message;
            }
            return strResult;
        }

        /// <summary>
        /// 获取Html字符串中指定标签的指定属性的值 
        /// </summary>
        /// <param name="html">Html字符</param>
        /// <param name="tag">指定标签名</param>
        /// <param name="attr">指定属性名</param>
        /// <returns></returns>
        public static List<string> GetHtmlAttr(string html, string tag, string attr)
        {

            Regex re = new Regex(@"(<" + tag + @"[\w\W].+?>)");
            MatchCollection imgreg = re.Matches(html);
            List<string> m_Attributes = new List<string>();
            Regex attrReg = new Regex(@"([a-zA-Z1-9_-]+)\s*=\s*(\x27|\x22)([^\x27\x22]*)(\x27|\x22)", RegexOptions.IgnoreCase);

            for (int i = 0; i < imgreg.Count; i++)
            {
                MatchCollection matchs = attrReg.Matches(imgreg[i].ToString());

                for (int j = 0; j < matchs.Count; j++)
                {
                    GroupCollection groups = matchs[j].Groups;

                    if (attr.ToUpper() == groups[1].Value.ToUpper())
                    {
                        m_Attributes.Add(groups[3].Value);
                        break;
                    }
                }

            }
            return m_Attributes;

        }
        /// <summary>
        /// 按文本内容长度截取HTML字符串(支持截取带HTML代码样式的字符串)
        /// </summary>
        /// <param name="html">将要截取的字符串参数</param>
        /// <param name="len">截取的字节长度</param>
        /// <param name="endString">字符串末尾补上的字符串</param>
        /// <returns>返回截取后的字符串</returns>
        public static string HTMLSubstring(string html, int len, string endString)
        {
            string r = "";
            if (!string.IsNullOrEmpty(html) && html.Length > len)
            {
                MatchCollection mcentiry, mchtmlTag;
                List<string> inputHTMLTag = new List<string>();
                string tmpValue, nowtag, losetag;
                int rWordCount = 0, wordNum = 0, i = 0;
                Regex rxSingle = new Regex("^<(br|hr|img|input|param|meta|link|wbr)", RegexOptions.Compiled | RegexOptions.IgnoreCase)//是否单标签正则
                    , rxEndTag = new Regex("</[^>]+>", RegexOptions.Compiled)//是否结束标签正则
                    , rxTagName = new Regex("</?([a-z\\d]+)[^>]*>", RegexOptions.Compiled | RegexOptions.IgnoreCase)//获取标签名正则
                    , rxHtmlTag = new Regex("<[^>]+>", RegexOptions.Compiled)//html标签正则
                    , rxEntity = new Regex("&[a-z]{1,9};", RegexOptions.Compiled | RegexOptions.IgnoreCase)//实体正则
                    , rxEntityReverse = new Regex("§", RegexOptions.Compiled)//反向替换实体正则
                    ;
                html = html.Replace("§", "&#167;");//替换字符§为他的实体“&#167;”，以便进行下一步替换
                mcentiry = rxEntity.Matches(html);//收集实体对象到匹配数组中
                html = rxEntity.Replace(html, "§");//替换实体为特殊字符§，这样好控制一个实体占用一个字符
                mchtmlTag = rxHtmlTag.Matches(html);//收集html标签到匹配数组中

                string[] arrWord = rxHtmlTag.Split(html);//拆分
                wordNum = arrWord.Length;


                //获取指定内容长度及HTML标签
                for (; i < wordNum; i++)
                {
                    if (rWordCount + arrWord[i].Length >= len) r += arrWord[i].Substring(0, len - rWordCount) + endString;
                    else r += arrWord[i];


                    rWordCount += arrWord[i].Length;//计算已经获取到的字符长度

                    if (rWordCount >= len) break;

                    //搜集已经添加的非单标签，以便封闭HTML标签对
                    if (i < wordNum - 1)
                    {
                        tmpValue = mchtmlTag[i].Value.ToLower();
                        if (!rxSingle.IsMatch(tmpValue))
                        { //不是单标签
                            if (rxEndTag.IsMatch(tmpValue) && inputHTMLTag.Count > 0)
                            {
                                nowtag = rxTagName.Match(tmpValue).Groups[1].Value.ToLower();
                                losetag = rxTagName.Match(inputHTMLTag[inputHTMLTag.Count - 1]).Groups[1].Value.ToLower();
                                inputHTMLTag.RemoveAt(inputHTMLTag.Count - 1);
                                if (nowtag != losetag)
                                {
                                    RemoveTag(inputHTMLTag, nowtag);
                                    r += "</" + losetag + ">";
                                }
                            }
                            else inputHTMLTag.Add(tmpValue);
                        }
                        r += tmpValue;
                    }
                }
                //替换回实体
                for (i = 0; i < mcentiry.Count; i++) r = rxEntityReverse.Replace(r, mcentiry[i].Value, 1);
                //封闭标签
                for (i = inputHTMLTag.Count - 1; i >= 0; i--) r += "</" + rxTagName.Match(inputHTMLTag[i].ToString()).Groups[1].Value + ">";
            }
            else r = html;

            return r;
        }
        /// <summary>
        /// 删除标签
        /// </summary>
        /// <param name="tags"></param>
        /// <param name="tag"></param>
        private static void RemoveTag(List<string> tags, string tag)
        {
            for (int i = tags.Count - 1; i >= 0; i--) if (tags[i].IndexOf(tag) == 1) { tags.RemoveAt(i); break; }
        }
        /// <summary>
        /// 父页面所有需要抓取数据的连接
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static List<string> htmlFather(string url, string path)
        {
            List<string> LastA = new List<string>();
            HtmlWeb htmlWeb = new HtmlWeb();
            HtmlAgilityPack.HtmlDocument document = htmlWeb.Load(path);
            List<string> list = new List<string>();
            list.Add("//div[@class='wss_UsaHot']");
            bool isUsa = false;
            if (url.Contains("usa.html"))
            {
                isUsa = true;
                // list.Add("//div[@class='yymmg_info_us1']");
                // list.Add("//div[@class='yymmg_info_us2']");
                // list.Add("//div[@class='yymmg_info_us3']");
                //list.Add("//div[@class='yymmg_info_us4']");
                list.Add("//div[@class='yymmg_wrap']");
            }
            else
            {
                list.Add("//div[@class='yymmg_wrap']");
            }
            string htmlstring = "";
            //int i = 1;
            foreach (string str in list)
            {
                HtmlNodeCollection collectionTi = document.DocumentNode.SelectNodes(str);
                if (collectionTi != null)
                {
                    foreach (HtmlNode item in collectionTi)
                    {
                        int j = 0;
                        htmlstring = item.OuterHtml;
                        List<string> listA = WebHandler.GetHtmlAttr(htmlstring, "a", "href");
                        List<string> LatA = new List<string>();
                        foreach (string obj in listA)
                        {
                            if (obj.Contains("/newsdetail") && !LatA.Contains(obj))
                            {
                                LatA.Add(obj);
                            }
                        }
                        if (isUsa && str != list[0])
                        {
                            //需要爬取的连接另存list
                            foreach (string obj in LatA)
                            {
                                j++;
                                if ((12 <= j && j <= 17) ||(24<=j&& j<=29) ||(48<=j&&j<=53) || (90<=j&&j<=95))
                                {
                                    LastA.Add(obj);
                                }
                            }

                        }
                        else
                        {
                            foreach (string obj in LatA)
                            {
                                LastA.Add(obj);
                            }
                        }
                    }
                }

            }
            return LastA;
        }
        /// <summary>
        /// 子页面
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string html(string path)
        {

            HtmlWeb htmlWeb = new HtmlWeb();
            HtmlAgilityPack.HtmlDocument document = htmlWeb.Load(path);
            string xpath = "//div[@class='ycase_left']";
            HtmlNodeCollection collection = document.DocumentNode.SelectNodes(xpath);
            string htmlstring = "";
            foreach (HtmlNode item in collection)
            {
                htmlstring = item.OuterHtml;
            }
            //剔除多余数据
            xpath = "//div[@class='tag']";
            List<string> list = new List<string>();
            list.Add(xpath);
            list.Add("//div[@class='fanye_jia']");
            list.Add("//div[@class='ycase_det_share']");
            list.Add("//div[@class='y_casefy']");
            list.Add("//div[@class='relnews']");
            list.Add("//div[@class='ycase_right']");
            list.Add("//div[@class='index_new_footer']");
            list.Add("//div[@class='new_side_float']");
            list.Add("//div[@class='foot_float_20']");
            list.Add("//script");
            list.Add("//div[@class='new_side_float_mini']");
            list.Add("//iframe");
            list.Add("//a[@href]");
            int i = 0;
            foreach (string str in list)
            {
                
                HtmlNodeCollection collectionTi = document.DocumentNode.SelectNodes(str);
                if (collectionTi != null)
                {
                    foreach (HtmlNode item in collectionTi)
                    {
                        if (i < 12)
                        {
                            htmlstring = htmlstring.Replace(item.OuterHtml, "");
                        }
                        else
                        {
                            htmlstring = htmlstring.Replace(item.OuterHtml, item.InnerText);
                        }
                    }
                }
                i++;
            }
            //Regex regImg = new Regex(@"(?is)<a[^>]*?href=(['""\s]?)(?<href>([^'""\s]*\.doc)|([^'""\s]*\.docx)|([^'""\s]*\.xls)|([^'""\s]*\.xlsx)|([^'""\s]*\.ppt)|([^'""\s]*\.txt)|([^'""\s]*\.zip)|([^'""\s]*\.rar)|([^'""\s]*\.gz)|([^'""\s]*\.bz2))\1[^>]*?>");
            //替换掉所有a标签

            return htmlstring;
        }

        /// <summary>
        /// 子页面
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string html1(string path)
        {

            HtmlWeb htmlWeb = new HtmlWeb();
            HtmlAgilityPack.HtmlDocument document = htmlWeb.Load(path);
            string xpath = "//head";
            HtmlNodeCollection collection = document.DocumentNode.SelectNodes(xpath);
            string htmlstring = "";
            foreach (HtmlNode item in collection)
            {
                htmlstring = item.OuterHtml;
            }           
            return htmlstring;
        }

        /// <summary>
        /// 获取网页转换后的字符串
        /// </summary>
        /// <returns></returns>
        public static void GetFilterHtml(string url, string path,string imgPath)
        {
            try
            {
                Encoding code = Encoding.GetEncoding("UTF-8"); //声明文件编码
                string ycase_left = "";
                WebPage webInfo = new WebPage(url);
                string htmlSstring = webInfo.M_html;
                string[] arryfather = url.Split('/');
                WriteFile(path, htmlSstring, code, arryfather[4]);
                List<string> listA = htmlFather(url, path + @"/" + arryfather[4]);
                //读取html头部
                string htmltopPath = AppDomain.CurrentDomain.BaseDirectory + @"\htmltop.txt";
                StreamReader fileStream = new StreamReader(htmltopPath, Encoding.Default);
                string htmlTop = fileStream.ReadToEnd();
                //读取html底部
                string htmlfilterPath = AppDomain.CurrentDomain.BaseDirectory + @"\htmlfitter.txt";
                StreamReader fileStream1 = new StreamReader(htmlfilterPath, Encoding.Default);
                string htmlfilter = fileStream1.ReadToEnd();
                htmlfilter = htmlfilter.Replace("?", "©");
                //var y = webInfo.InsiteLinks;
                //List<string> listA = new List<string>();
                //foreach (var item in y)
                //{
                //    listA.Add(item.NavigateUrl);
                //}
                ////List<string> listA = WebHandler.GetHtmlAttr(htmlSstring, "a", "href");
                //List<string> a = new List<string>();
                //List<string> LatA = new List<string>();
                //foreach (string item in listA)
                //{
                //    if (item.Contains("/newsdetail") && !LatA.Contains(item))
                //    {
                //        LatA.Add(item);
                //    }
                //}
                //int i = 0;
                ////需要爬取的连接另存list
                //foreach (string item in a)
                //{
                //    i++;
                //    if ((1 <= i  && i<= 7) || 22<=i)
                //    {
                //        LatA.Add(item);
                //    }
                //}
                //开始爬取网页子链接下的html
                foreach (string item in listA)
                {
                    string[] arry = item.Split('/');
                    WebPage webInfoChirld = new WebPage("http://www.worldwayhk.com/" + item);
                    string htmlSstringChirld = webInfoChirld.M_html;
                    WriteFile(path, htmlSstringChirld, code, arry[1]);
                    ycase_left = html(path + @"/" + arry[1]);
                    ////去除html下的a标签链接属性
                    //List<string> listchirldA = WebHandler.GetHtmlAttr(ycase_left, "a", "href");
                    //foreach (string list in listchirldA)
                    //{
                    //    ycase_left = ycase_left.Replace(list, "");
                    //}
                    //获取网页图片
                    List<string> listImg = WebHandler.GetHtmlAttr(ycase_left, "img", "src");
                    foreach (string img in listImg)
                    {
                        if (img.Contains("http://www.worldwayhk.com"))
                        {
                            string imguRL = img.Replace("http://www.worldwayhk.com", "").Replace("/asp.net/../", "/");
                            //替换网页上的图片地址
                            ycase_left = ycase_left.Replace(img, ".."+ imguRL);
                        }
                        else
                        {
                            //替换网页上的图片地址
                            ycase_left = ycase_left.Replace(img, ".."+img);
                        }
                    }
                    // string htmlTop = ""; //html1(path + @"/" + arry[1]);
                    //ycase_left = "<!DOCTYPE html><html lang = \"en\"><head><meta charset = \"UTF-8\" ></head ><body>"+ycase_left;
                    ycase_left = htmlTop + ycase_left;
                    ycase_left = ycase_left + htmlfilter;
                    //保存文章页面
                    WriteFile(path, ycase_left.Replace("/asp.net/../", "/"), code, arry[1]);
                    foreach (string img in listImg)
                    {
                        string imguRL = img.Replace("http://www.worldwayhk.com", "").Replace("/asp.net/../","/");
                        string[] arryImg = imguRL.Split('/');
                        string name = arryImg[5];
                            imguRL = "http://www.worldwayhk.com" + imguRL;
                        BaoCun(imgPath + arryImg[1] + "/"+ arryImg[2] + "/" + arryImg[3] + "/" + arryImg[4], imguRL, name);
                    }
                    //KindEditor / attached / image / 20160412 / 20160412140904_9121.jpg
                    ///KindEditor/asp.net/../attached/image/20161019/20161019162715_5449.jpg
                }
            }
            catch (Exception e)
            {

                throw new Exception(e.Message);
            }

        }
        /// <summary>
        /// 保存图片
        /// </summary>
        public static void BaoCun(string path,string url,string name)
        {
            WebRequest request = WebRequest.Create(url);
            WebResponse response = request.GetResponse();
            Stream reader = response.GetResponseStream();
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            //判断文件是否存在
            if (!File.Exists(path + "/" + name))
            {
                FileStream writer = new FileStream(path + "/" + name, FileMode.Create, FileAccess.Write);
                byte[] buff = new byte[512];
                int c = 0; //实际读取的字节数
                while ((c = reader.Read(buff, 0, buff.Length)) > 0)
                {
                    writer.Write(buff, 0, c);
                }
                writer.Close();
                writer.Dispose();
                reader.Close();
                reader.Dispose();
                response.Close();
            }
        }

        /// <summary>
        /// 保存文件
        /// </summary>

        /// <param name="htmlSstring"></param>
        /// <param name="code"></param>
        /// <param name="htmlfilename"></param>
        public static void WriteFile(string path, string htmlSstring, Encoding code, string htmlfilename)
        {
            //创建文件夹               
            string FilePath = path;
            if (!Directory.Exists(FilePath))
            {
                Directory.CreateDirectory(FilePath);
            }
            // 写入文件
            using (var fileStream1 = new FileStream(FilePath + @"/" + htmlfilename, FileMode.Create))
            {
                var sw1 = new StreamWriter(fileStream1, code);
                sw1.WriteLine(htmlSstring);
                sw1.Close();
            }
            //sw = new StreamWriter(FilePath + @"\" + htmlfilename, false, code);
        }

        /// <summary>
                /// 读取CSV文件通过文本格式
                /// </summary>
                /// <param name="strpath"></param>
                /// <returns></returns>
        public static DataTable readCsvTxt(string strpath)
        {
            int intColCount = 0;
            bool blnFlag = true;
            DataTable mydt = new DataTable("myTableName");

            DataColumn mydc;
            DataRow mydr;

            string strline;
            string[] aryline;

            System.IO.StreamReader mysr = new System.IO.StreamReader(strpath);

            while ((strline = mysr.ReadLine()) != null)
            {
                aryline = strline.Split(',');

                if (blnFlag)
                {
                    blnFlag = false;
                    intColCount = aryline.Length;
                    for (int i = 0; i < aryline.Length; i++)
                    {
                        mydc = new DataColumn(aryline[i]);
                        mydt.Columns.Add(mydc);
                    }
                }

                mydr = mydt.NewRow();
                for (int i = 0; i < intColCount; i++)
                {
                    mydr[i] = aryline[i];
                }
                mydt.Rows.Add(mydr);
            }

            return mydt;
        }

        /// <summary>
        /// 截取字符串中指定标签内的内容
        /// </summary>
        /// <param name="Content">需要截取的字符串</param>
        /// <param name="start">开始标签</param>
        /// <param name="end">结束标签</param>
        /// <returns></returns>
        public string GetStr(string Content, string start, string end)
        {
            var posStart = Content.IndexOf(start);
            var posEnd = Content.IndexOf(end);
            return Content.Substring(posStart, (posEnd - posStart + end.Length));
        }
    }
}
