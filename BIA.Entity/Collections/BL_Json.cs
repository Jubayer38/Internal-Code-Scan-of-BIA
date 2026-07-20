using BIA.Entity.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.Collections
{
    public class BL_Json : IBL_Json
    {
        public byte[] GetGenericJsonData<T>(T obj)
        {
            var json = JsonConvert.SerializeObject(obj);
            return Encoding.UTF8.GetBytes(json);
        }
        //public byte[] GetGenericJsonData<T>(T obj)
        //{
        //    string result = "";
        //    //Convert.FromBase64String();
        //    var content2 = JsonConvert.SerializeObject(obj);
        //    result = content2.ToString();
        //    byte[] bytedata = Encoding.ASCII.GetBytes(result);
        //    return bytedata;
        //}

        public async Task<byte[]> GetGenericJsonDataAsync<T>(T obj)
        {
            return await Task.Run(() =>
            {
                string json = JsonConvert.SerializeObject(obj);
                return Encoding.UTF8.GetBytes(json);
            });
        }


        public async Task<byte[]> GetCompressedJsonStreamAsync<T>(T obj)
        {
            using var outputStream = new MemoryStream();
            using (var gzipStream = new GZipStream(outputStream, CompressionLevel.Optimal, leaveOpen: true))
            using (var writer = new StreamWriter(gzipStream, Encoding.UTF8))
            using (var jsonWriter = new JsonTextWriter(writer))
            {
                var serializer = new JsonSerializer();
                serializer.Serialize(jsonWriter, obj);
                await jsonWriter.FlushAsync();
            }

            // Reset stream position and return bytes
            return outputStream.ToArray();
        }
    }
    public class XmlToByteConverter
    {
        public byte[] ConvertXmlToByteArray(string xmlString)
        {
            if (string.IsNullOrEmpty(xmlString))
                throw new ArgumentNullException(nameof(xmlString));

            return Encoding.UTF8.GetBytes(xmlString);
        }
    }    
}
