using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using Newtonsoft.Json;
using System.Data;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Net.Mail;

namespace BirdWS
{
    /// <summary>
    /// Summary description for WebService1
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    public class WebService1 : System.Web.Services.WebService
    {
        //Bird ID Input
        private string Bird_Input()
        {

            return null;
        }

        private string Call_Web_Service_Method(string sID)//Call ED WS 
        {
            //ServerName.WebServiceName CallWebService =new ServerName.WebServiceName();
            //String sGetValue = CallWebService.MethodName();
            //Label1.Text = sGetValue;
            return null;
        }

        private string ReadJSON(string data)//解析JSON並匯出結果
        {
            //格式化json字符串
            List<ReadDataJson> data0 = JsonConvert.DeserializeObject<List<ReadDataJson>>(data);

            string tmpDisease = data0[0].Disease;//配對者的異常疾病
            int s = data0[0].Gender == "M" ? 1 : 2;//配對者的性別 M男F女
            List<LightResult> result = new List<LightResult>();
            for (int i = 0; i < data0.Count; i++)
            {
                LightResult tmp0 = new LightResult();
                tmp0.ID = i;
                tmp0.PeopleID = data0[i].PeopleID;
                tmp0.Disease = data0[i].Disease;
                tmp0.Gender = data0[i].Gender;

                //配對者不比對
                if (i == 0)
                {
                    tmp0.RedLight = "";
                    tmp0.YellowLight = "";
                    result.Add(tmp0);
                    continue;
                }
                string[] tmp = Compare(s, data0[i].Disease, tmpDisease);
                tmp0.RedLight = tmp[0];
                tmp0.YellowLight = tmp[1];
                tmp0.RedLightNum = Convert.ToInt32(tmp[2]);
                tmp0.YellowLightNum = Convert.ToInt32(tmp[3]);
                result.Add(tmp0);
            }
            return JsonConvert.SerializeObject(result);
        }
        private string[] Compare(int s, string sDisease1, string sDisease2)//比對Function int s 1是男生2是女生
        {
            int[] AR = new int[] { 262, 4, 3, 5, 2, 3, 2, 2, 2, 2, 2, 2, 2, 3, 3, 2, 2, 2, 2, 2, 3, 2, 2, 2, 2, 4, 2, 2 };//隱性疾病數
            int[] AR1 = new int[28];//隱性1
            int[] AR2 = new int[28];//隱性2
            int[] XL = new int[] { 27, 4, 2, 3 };//性聯疾病數
            int[][] D = new int[][]
            {
                new int[] { 7,2 },//D1和D2總數
                new int[] { 1,2,2,2},//D1同基因數
                new int[] { 2}//D2同基因數
            };//交互影響疾病數
            string[] Disease1 = sDisease1.Split(',');
            string[] Disease2 = sDisease2.Split(',');
            string RedLight = "", YellowLight = "";
            int RedLightNum = 0, YellowLightNum = 0;
            int D1Count1 = 0, D1Count2 = 0;//交互影響1
            int D2Count1 = 0, D2Count2 = 0;//交互影響2
            //先檢查配對者有無交互影響和隱性的疾病
            for (int i = 0; i < Disease2.Length; i++)
            {
                string tmp = Disease2[i];
                int n = Convert.ToInt32(tmp.Substring(2, 2));
                if (tmp == "")
                {
                    break;
                }
                else if (tmp.Substring(0, 2) == "D1")
                {
                    D1Count2 = 1;
                    break;
                }
                else if (tmp.Substring(0, 2) == "D2")
                {
                    D2Count2 = 1;
                    break;
                }
                else if (tmp.Substring(0, 2) == "AR" && n != 0)//配對者隱性
                {
                    AR2[n] = 1;
                }
            }
            if (s == 1)//男一女多
            {
                for (int i = 0; i < Disease1.Length; i++)
                {
                    string tmp1 = Disease1[i];
                    if (tmp1 == "")//無異常疾病
                    {
                        continue;
                    }
                    else if (tmp1.Substring(0, 2) == "D1")//交互影響1
                    {
                        D1Count1 = 1;
                    }
                    else if (tmp1.Substring(0, 2) == "D2")//交互影響2
                    {
                        D2Count1 = 1;
                    }
                    else if (tmp1.Substring(0, 2) == "AR")//隱性塞紅燈
                    {
                        int n = Convert.ToInt32(tmp1.Substring(2, 2));
                        if (n == 0) //皆不同基因
                        {
                            for (int j = 0; j < Disease2.Length; j++)
                            {
                                string tmp2 = Disease2[j];
                                if (tmp2 == tmp1)
                                {
                                    RedLight += tmp1 + ",";
                                    Disease1[i] = "";
                                    Disease2[j] = "";
                                    break;
                                }
                            }
                        }
                        else //有相同基因
                        {
                            AR1[n] = 1;
                        }
                    }
                    else if (tmp1.Substring(0, 2) == "XL")//性聯塞紅燈
                    {
                        int n = Convert.ToInt32(tmp1.Substring(2, 2));
                        if (n == 0) RedLight += tmp1 + ",";
                        else
                            for (int m = 1; m <= XL[n]; m++)
                                RedLight += tmp1.Substring(0, 4) + "00" + m.ToString() + ",";
                    }
                }
            }
            else//女一男多
            {
                //配對者性聯塞紅燈
                for (int i = 0; i < Disease2.Length; i++)
                {
                    string tmp1 = Disease2[i];
                    if (tmp1 == "") break;
                    else if (tmp1.Substring(0, 2) == "XL") {
                        int n = Convert.ToInt32(tmp1.Substring(2, 2));
                        if (n == 0) RedLight += tmp1 + ",";
                        else
                            for (int m = 1; m <= XL[n]; m++)
                                RedLight += tmp1.Substring(0, 4) + "00" + m.ToString() + ",";
                    }
                }
                //
                for (int i = 0; i < Disease1.Length; i++)
                {
                    string tmp1 = Disease1[i];
                    if (tmp1 == "")//無異常疾病
                    {
                        continue;
                    }
                    else if (tmp1.Substring(0, 2) == "D1")//交互影響1
                    {
                        D1Count1 = 1;
                    }
                    else if (tmp1.Substring(0, 2) == "D2")//交互影響2
                    {
                        D2Count1 = 1;
                    }
                    else if (tmp1.Substring(0, 2) == "AR")//隱性塞紅燈
                    {
                        int n = Convert.ToInt32(tmp1.Substring(2, 2));
                        if (n == 0) //皆不同基因
                        {
                            for (int j = 0; j < Disease2.Length; j++)
                            {
                                string tmp2 = Disease2[j];
                                if (tmp2 == tmp1)
                                {
                                    RedLight += tmp1 + ",";
                                    Disease1[i] = "";
                                    Disease2[j] = "";
                                    break;
                                }
                            }
                        }
                        else //有相同基因
                        {
                            AR1[n] = 1;
                        }
                    }
                }
            }
            //交互影響紅黃燈
            int D1Num = D1Count1 + D1Count2;
            int D2Num = D2Count1 + D2Count2;
            if (D1Num == 2)
            {
                for (int i = 0; i < D[1].Length; i++)
                    for (int j = 1; j <= D[1][i]; j++)
                        RedLight += "D10" + i.ToString() + "00" + j.ToString() + ",";
            }
            else if (D1Num == 1)
            {
                for (int i = 0; i < D[1].Length; i++)
                    for (int j = 1; j <= D[1][i]; j++)
                        YellowLight += "D10" + i.ToString() + "00" + j.ToString() + ",";
            }
            if (D2Num == 2)
            {
                for (int i = 0; i < D[2].Length; i++)
                    for (int j = 1; j <= D[2][i]; j++)
                        RedLight += "D20" + i.ToString() + "00" + j.ToString() + ",";
            }
            else if (D2Num == 1)
            {
                for (int i = 0; i < D[2].Length; i++)
                    for (int j = 1; j <= D[2][i]; j++)
                        YellowLight += "D20" + i.ToString() + "00" + j.ToString() + ",";
            }
            //隱性--皆不同基因的塞黃燈，有相同基因的判斷黃燈紅燈
            for (int i = 0; i < Disease1.Length; i++)
            {
                string tmp = Disease1[i];
                if (tmp == "") continue;
                int n = Convert.ToInt32(tmp.Substring(2, 2));
                if (tmp.Substring(0, 2) == "AR")
                {
                    if (n == 0) YellowLight += tmp + ",";
                    else
                    {
                        if (AR1[n] + AR2[n] == 2)
                        {
                            AR1[n] = 0;
                            AR2[n] = 0;
                            for (int m = 1; m <= AR[n]; m++) RedLight += tmp.Substring(0, 4) + "00" + m.ToString() + ",";
                        }
                        else if (AR1[n] + AR2[n] == 1)
                        {
                            AR1[n] = 0;
                            AR2[n] = 0;
                            for (int m = 1; m <= AR[n]; m++) YellowLight += tmp.Substring(0, 4) + "00" + m.ToString() + ",";
                        }
                    }
                }
            }
            for (int i = 0; i < Disease2.Length; i++)
            {
                string tmp = Disease2[i];
                if (tmp == "") continue;
                int n = Convert.ToInt32(tmp.Substring(2, 2));
                if (tmp.Substring(0, 2) == "AR")
                {
                    if (n == 0) YellowLight += tmp + ",";
                    else
                    {
                        if (AR1[n] + AR2[n] == 2)
                        {
                            AR1[n] = 0;
                            AR2[n] = 0;
                            for (int m = 1; m <= AR[n]; m++) RedLight += tmp.Substring(0, 4) + "00" + m.ToString() + ",";
                        }
                        else if (AR1[n] + AR2[n] == 1)
                        {
                            AR1[n] = 0;
                            AR2[n] = 0;
                            for (int m = 1; m <= AR[n]; m++) YellowLight += tmp.Substring(0, 4) + "00" + m.ToString() + ",";
                        }
                    }
                }
            }
            //去逗號和塞紅燈黃燈數
            if (RedLight != "")
            {
                RedLight = RedLight.Substring(0, RedLight.Length - 1);
                RedLightNum = RedLight.Split(',').Length;
            }
            else
            {
                RedLightNum = 0;
            }
            if (YellowLight != "")
            {
                YellowLight = YellowLight.Substring(0, YellowLight.Length - 1);
                YellowLightNum = YellowLight.Split(',').Length;
            }
            else
            {
                YellowLightNum = 0;
            }
            string[] result = { RedLight, YellowLight, RedLightNum.ToString(), YellowLightNum.ToString() };
            return result;
        }

        private class ReadDataJson//解析傳進來的JSON Array用
        {
            [JsonProperty("PeopleID")]
            public string PeopleID { get; set; }
            [JsonProperty("Disease")]
            public string Disease { get; set; }
            [JsonProperty("Gender")]
            public string Gender { get; set; }
        }
        private class LightResult//拋出結果用的Class
        {
            public int ID { get; set; }
            public string PeopleID { get; set; }//身分證字號
            public string Disease { get; set; }//所有疾病名
            public string RedLight { get; set; }//紅燈疾病
            public string YellowLight { get; set; }//黃燈疾病
            public int RedLightNum { get; set; }//紅燈數
            public int YellowLightNum { get; set; }//黃燈數
            public string Gender { get; set; }//性別 F卵M精
        }

        // 取得Logger(Logger以Program的Type Name命名)
        private static log4net.ILog Log { get; set; } = log4net.LogManager.GetLogger(typeof(WebService1));
        [WebMethod]
        public string Main(string ID, string Key)
        {
            Log.Info("Hello!送子鳥來比對囉!");
            string MailBody="<p>Log檔內容:<br/>";
            //Bird Input
            //try{
            //string BirdInput = Bird_Input();
            //      Log.Info("收到送子鳥的資料內容");
            //      Log.Info(資料內容);
            //      MailBody+="收到送子鳥的資料內容:<br/>"+資料內容 +"<br/>";
            //} 
            //    catch(Exception ex)
            //    {
            //      Log.Error("送子鳥傳送失敗!");
            //      Log.Error(ex.Message);
            //      MailBody+="送子鳥傳送失敗!<br/>"+ex.Message+"<br/>" ;
            //    SendEmail sendEmail = new SendEmail("送子鳥WS錯誤",MailBody, new List<string> { "FurongLiu@GGA.ASIA" });
            //return "傳送資料錯誤";
            //} 


            //ED WS Input
            //using (var client =new ServiceReference1.XXXX(ID))
            //{
            //    try
            //    {
            //      Log.Info("收到資料庫的內容");
            //      Log.Info(內容);
            //      MailBody+="收到資料庫的內容:<br/>"+資料內容 +"<br/>";
            //    }
            //    catch(Exception ex)
            //    {
            //      Log.Error("資料庫傳送失敗!");
            //      Log.Error(ex.Message);
            //      MailBody+="資料庫傳送失敗!<br/>"+ex.Message+"<br/>" ;
            //    SendEmail sendEmail = new SendEmail("送子鳥WS錯誤", MailBody, new List<string> { "FurongLiu@GGA.ASIA" });
            //        }
            //}

            
            //傳假資料 ID: A165,H224,A225,G223
            string tmp = "[{'PeopleID':'A165','Disease':'AR03003,AR03002,D101001,AR00065','Gender':'M'}," +
                "{'PeopleID':'H224','Disease':'AR00001,AR03004,AR03001,AR02003,XL00001','Gender':'F'}," +
                "{'PeopleID':'A225','Disease':'XL01002,D100001,D103002,D200002','Gender':'F'}," +
                "{'PeopleID':'G223','Disease':'','Gender':'F'}]";

            //解析JSON和匯出所有比對結果
            string result0 = ReadJSON(tmp);

            //加密
            string Key0 = "3512012595";            
            string result = EncryptString(result0, Key0);

            if (Key == Key0)
            {
                Log.Info("送子鳥解密成功!");
                Log.Info("得到的配對結果為");
                Log.Info(DecryptString(result, Key));
                //MailBody += "送子鳥解密成功!<br/>得到的配對結果為:<br/>" + DecryptString(result, Key) + "<br/>";
                //拋回解密結果
                return DecryptString(result, Key);
            }
            else
            {
                //WriteLogFile(Today, Date.ToString("HH:mm:ss") + "  " + "解密失敗!\r\n");
                Log.Warn("輸入金鑰: ["+Key+"] 錯誤!");
                MailBody += "輸入金鑰: [" + Key + "] 錯誤!<br/>";
                SendEmail sendEmail = new SendEmail("送子鳥WS錯誤", MailBody, new List<string> { "FurongLiu@GGA.ASIA" });
                //拋回解密結果
                return "解密失敗!";
            }
            
            
        }
        [WebMethod]
        public string test(string ID, string Key)
        {
            DateTime Date = DateTime.Now;
            string Today = Date.ToString("yyyyMMdd") + "_" + Date.ToString("HHmmss");
            //WriteLogFile(Today, Date.ToString("yyyy/MM/dd") + " " + Date.ToString("HH:mm:ss") + "\r\n" + "收到要比對的ID有: " + ID + "\r\n");
            //傳假資料
            string tmp = "[{'PeopleID':'A265','Disease':'AR01003,D102001,AR00065,XL01002','Gender':'F'}," +
                "{'PeopleID':'H124','Disease':'AR01001,AR00002,AR05003','Gender':'M'}," +
                "{'PeopleID':'A125','Disease':'D101001,D200002','Gender':'M'}," +
                "{'PeopleID':'G123','Disease':'','Gender':'M'}]";
            
            //Bird Input
            //string BirdInput = Bird_Input();

            //ED WS Input
            //string tmp = Call_Web_Service_Method(BirdInput);
            
            //解析JSON和匯出所有比對結果
            string result0 = ReadJSON(tmp);

            //加密
            string result = EncryptString(result0, "3512012595");

            //拋回解密結果
            return DecryptString(result, "3512012595");
        }



        /// <summary>
        /// AES加密
        /// </summary>
        /// <param name="plainText">要加密的字串</param>
        /// <param name="key">加密KEY</param>
        /// <returns>加密後的字串</returns>
        public static string EncryptString(string plainText, string key)
        {
            //密碼轉譯一定都是用byte[] 所以把string都換成byte[]
            byte[] plainTextByte = Encoding.UTF8.GetBytes(plainText);
            byte[] keyByte = Encoding.UTF8.GetBytes(key);

            //加解密函數的key通常都會有固定的長度 而使用者輸入的key長度不定 因此用hash過後的值當做key
            MD5CryptoServiceProvider provider_MD5 = new MD5CryptoServiceProvider();
            byte[] md5Byte = provider_MD5.ComputeHash(keyByte);

            //產生加密實體
            RijndaelManaged aesProvider = new RijndaelManaged();
            ICryptoTransform aesEncrypt = aesProvider.CreateEncryptor(md5Byte, md5Byte);

            //output就是加密過後的結果
            byte[] output = aesEncrypt.TransformFinalBlock(plainTextByte, 0, plainTextByte.Length);

            //    將加密後的位元組轉成16進制字串
            return BitConverter.ToString(output).Replace("-", "");
        }
        /// <summary>
        /// AES解密
        /// </summary>
        /// <param name="chipherText">加密後的密文</param>
        /// <param name="key">解密KEY</param>
        /// <returns>解密後的明文</returns>
        public static string DecryptString(string chipherText, string key)
        {
            byte[] chipherTextByte = new byte[chipherText.Length / 2];
            int j = 0;

            for (int i = 0; i < chipherText.Length / 2; i++)
            {
                chipherTextByte[i] = Byte.Parse(chipherText[j].ToString() + chipherText[j + 1].ToString(), System.Globalization.NumberStyles.HexNumber);
                j += 2;
            }

            //密碼轉譯一定都是用byte[] 所以把string都換成byte[]
            byte[] keyByte = Encoding.UTF8.GetBytes(key);

            //加解密函數的key通常都會有固定的長度 而使用者輸入的key長度不定 因此用hash過後的值當做key
            MD5CryptoServiceProvider provider_MD5 = new MD5CryptoServiceProvider();
            byte[] md5Byte = provider_MD5.ComputeHash(keyByte);

            //產生解密實體
            RijndaelManaged aesProvider = new RijndaelManaged();
            ICryptoTransform aesDecrypt = aesProvider.CreateDecryptor(md5Byte, md5Byte);

            //string_secretContent就是解密後的明文
            byte[] plainTextByte = aesDecrypt.TransformFinalBlock(chipherTextByte, 0, chipherTextByte.Length);
            string plainText = Encoding.UTF8.GetString(plainTextByte);
            return plainText;
        }

        public class SendEmail
        {
            public SendEmail(string sSubject, string sBody, List<string> lTo)
            {
                try
                {
                    MailMessage mail = new MailMessage();
                    //SmtpClient SmtpServer = new SmtpClient("itmail.GGA.ASIA");
                    mail.IsBodyHtml = true;
                    mail.BodyEncoding = System.Text.Encoding.UTF8;
                    mail.From = new MailAddress("創源測試用信箱<GGAMSCTEST@gmail.com>");
                    //mail.To.Add("FurongLiu@GGA.ASIA");
                    if (lTo != null)
                    {
                        foreach (var v in lTo)
                        {
                            mail.To.Add(v);
                        }
                    }
                    mail.Subject = sSubject;
                    mail.Body = sBody+"</p><p>From 送子鳥配對WS</p>";

                    SmtpClient SmtpServer = new SmtpClient("smtp.gmail.com");
                    SmtpServer.Port = 587;
                     SmtpServer.Credentials = new System.Net.NetworkCredential("GGAMSCTEST", "jbeqgipjmbsuulgi");//二階段認證碼 http://demo.tc/post/807
                    SmtpServer.EnableSsl = true;

                    SmtpServer.Send(mail);
                    var vEmailTo = string.Empty;
                    foreach (var v in mail.To)
                    {
                        vEmailTo += string.Format("{0},", v);
                    }
                    // Log.Info("Sent EMail to" + vEmailTo + " ; Body " + mail.Body + " ; Subject " + mail.Subject);
                }
                catch (Exception ex)
                {
                    // Log.Fatal("EMail Function is Fatal :" + ex.Message);
                }
            }
        }
    }

}
