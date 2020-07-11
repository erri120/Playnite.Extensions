using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Extensions.Common;

namespace VNDBMetadata
{
    public class VNDBClient : IDisposable
    {
        private readonly TcpClient _client;

        public VNDBClient()
        {
            _client = new TcpClient();
        }

        private static string ToVNDBJson<T>(string command, T obj)
        {
            var json = obj.ToJson();
            var result = $"{command} {json}";
            return result;
        }

        private static byte[] AddEndOfTransmissionChar(string s)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(s);
            var buffer = new byte[bytes.Length + 1];
            bytes.CopyTo(buffer, 0);
            buffer[buffer.Length - 1] = Consts.EndOfTransmissionChar;

            return buffer;
        }

        private static Result<T> FromResults<T>(string res)
        {
            if (!res.StartsWith("results"))
                throw new ArgumentException();
            res = res.Substring("results".Length).TrimStart();
            return res.FromJson<Result<T>>();
        }

        private async Task<string> RequestAndReceive(string command)
        {
            return await RequestAndReceive<string>(command, null);
        }

        private async Task<string> RequestAndReceive<T>(string command, T obj)
        {
            if (!_client.Connected)
                await _client.ConnectAsync(Consts.Host, Consts.TCPPort);

            var stream = _client.GetStream();

            byte[] buffer = obj == null 
                ? AddEndOfTransmissionChar(command)
                : AddEndOfTransmissionChar(ToVNDBJson(command, obj));

            await stream.WriteAsync(buffer, 0, buffer.Length);

            buffer = new byte[1024];
            int byteRead;
            var response = new StringBuilder();
            var retries = 0;

            RETRY:
            do
            {
                byteRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (byteRead == 0)
                {
                    if(retries > Consts.MaxRetries)
                        throw new Exception("Max Retires reached!");

                    Thread.Sleep(10);
                    retries++;
                    goto RETRY;
                }

                var last = buffer[byteRead - 1];
                response.AppendFormat("{0}", Encoding.UTF8.GetString(buffer, 0, last == Consts.EndOfTransmissionChar ? byteRead - 1 : byteRead));
            } while (stream.DataAvailable);

            if (buffer[byteRead - 1] != Consts.EndOfTransmissionChar)
                throw new Exception($"Last byte read is not the End Of Transmission char but {buffer[byteRead - 1]:X}");
            

            return response.ToString();
        }

        public async Task<bool> Login(string username = null, string password = null)
        {
            var login = new Login
            {
                protocol = 1,
                client = Consts.ClientName,
                clientver = Consts.ClientVersion,
            };

            var res = await RequestAndReceive("login", login);
            if(res != Consts.OK)
                throw new Exception($"Login failed: {res}");

            return true;
        }

        public async Task<Result<GetVN>> GetVN(int id)
        {
            //var res = await RequestAndReceive($"get vn basic,details,anime,stats,screens,tags,relations,staff (id = {id})");
            var res = await RequestAndReceive($"get vn basic,details,stats,screens,tags (id = {id})");
            return FromResults<GetVN>(res);
        }

        public void Dispose()
        {
            _client.Dispose();
        }
    }
}
