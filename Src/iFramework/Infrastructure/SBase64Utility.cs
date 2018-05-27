using System;
using System.IO;
using System.Net;
using System.Text;

namespace IFramework.Infrastructure
{
    /// <summary>
    ///     有关base64编码算法的相关操作
    ///     By 自由奔腾（wgscd）
    ///     改 Sithere
    /// </summary>
    public class SBase64Utility
    {
        //--------------------------------------------------------------------------------
        /// <summary>
        ///     将字符串使用base64算法加密
        /// </summary>
        /// <param name="sourceString">待加密的字符串</param>
        /// <param name="ens">System.Text.Encoding 对象，如创建中文编码集对象：System.Text.Encoding.GetEncoding(54936)</param>
        /// <returns>加码后的文本字符串</returns>
        public static string EncodingForString(string sourceString, Encoding ens = null)
        {
            try
            {
                ens = ens ?? (ens = Encoding.GetEncoding(54936));
                return Convert.ToBase64String(ens.GetBytes(sourceString));
            }
            catch
            {
                return sourceString;
            }
        }

        /// <summary>
        ///     从base64编码的字符串中还原字符串，支持中文
        /// </summary>
        /// <param name="base64String">base64加密后的字符串</param>
        /// <param name="ens">System.Text.Encoding 对象，如创建中文编码集对象：System.Text.Encoding.GetEncoding(54936)</param>
        /// <returns>还原后的文本字符串</returns>
        public static string DecodingForString(string base64String, Encoding ens = null)
        {
            //从base64String中得到原始字符
            try
            {
                ens = ens ?? (ens = Encoding.GetEncoding(54936));
                return ens.GetString(Convert.FromBase64String(base64String));
            }
            catch
            {
                return base64String;
            }
        }


        //--------------------------------------------------------------------------------------

        /// <summary>
        ///     对任意类型的文件进行base64加码
        /// </summary>
        /// <param name="fileName">文件的路径和文件名</param>
        /// <returns>对文件进行base64编码后的字符串</returns>
        public static string EncodingForFile(string fileName)
        {
            var fs = File.OpenRead(fileName);
            var br = new BinaryReader(fs);

            /*System.Byte[] b=new System.Byte[fs.Length];
            fs.Read(b,0,Convert.ToInt32(fs.Length));*/


            var base64String = Convert.ToBase64String(br.ReadBytes((int) fs.Length));


            br.Close();
            fs.Close();
            return base64String;
        }

        /// <summary>
        ///     把经过base64编码的字符串保存为文件
        /// </summary>
        /// <param name="base64String">经base64加码后的字符串</param>
        /// <param name="fileName">保存文件的路径和文件名</param>
        /// <returns>保存文件是否成功</returns>
        public static bool SaveDecodingToFile(string base64String, string fileName)
        {
            var fs = new FileStream(fileName, FileMode.Create);
            var bw = new BinaryWriter(fs);
            bw.Write(Convert.FromBase64String(base64String));
            bw.Close();
            //fs.Close();
            return true;
        }


        //-------------------------------------------------------------------------------

        /// <summary>
        ///     从网络地址一取得文件并转化为base64编码
        /// </summary>
        /// <param name="url">文件的url地址,一个绝对的url地址</param>
        /// <param name="objWebClient">System.Net.WebClient 对象</param>
        /// <returns></returns>
        public static string EncodingFileFromUrl(string url, WebClient objWebClient)
        {
            return Convert.ToBase64String(objWebClient.DownloadData(url));
        }


        /// <summary>
        ///     从网络地址一取得文件并转化为base64编码
        /// </summary>
        /// <param name="url">文件的url地址,一个绝对的url地址</param>
        /// <returns>将文件转化后的base64字符串</returns>
        public static string EncodingFileFromUrl(string url)
        {
            //System.Net.WebClient myWebClient = new System.Net.WebClient();
            return EncodingFileFromUrl(url, new WebClient());
        }
    }
}