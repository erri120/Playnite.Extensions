using System;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Extensions.Common;

namespace VNDBMetadata
{
    public class VNDBClient : IDisposable
    {
        private readonly TcpClient _client;
        private readonly bool _useTLS;
        private SslStream _sslStream;

        public VNDBClient(bool useTLS)
        {
            _useTLS = useTLS;
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

        private async Task Connect()
        {
            if (_client.Connected)
            {
                if (!_useTLS)
                    return;

                if(_sslStream == null)
                    throw new Exception("Client is connected but ssl stream is null!");

                return;
            }

            await _client.ConnectAsync(Consts.Host, _useTLS ? Consts.TLSPort : Consts.TCPPort);

            if (!_useTLS)
                return;

            _sslStream = new SslStream(_client.GetStream());
            await _sslStream.AuthenticateAsClientAsync(Consts.Host);

            if(!_sslStream.IsAuthenticated)
                throw new Exception("Authentication failed!");
        }

        private async Task<string> RequestAndReceive<T>(string command, T obj)
        {
            await Connect();

            byte[] buffer = obj == null
                ? AddEndOfTransmissionChar(command)
                : AddEndOfTransmissionChar(ToVNDBJson(command, obj));

            var stream = _useTLS ? _sslStream : (Stream)_client.GetStream();

            await stream.WriteAsync(buffer, 0, buffer.Length);

            buffer = new byte[2048];
            var response = new StringBuilder();
            int bytes;
            var iterations = 0;

            do
            {
                bytes = await stream.ReadAsync(buffer, 0, buffer.Length);
                var decoder = Encoding.UTF8.GetDecoder();
                var chars = new char[decoder.GetCharCount(buffer, 0, bytes)];
                decoder.GetChars(buffer, 0, bytes, chars, 0);
                

                if (chars.Any(x => (byte)x == Consts.EndOfTransmissionChar))
                {
                    response.Append(chars, 0, chars.Length-1);
                    break;
                }

                response.Append(chars);

                if(iterations >= Consts.MaxIterations)
                    throw new Exception($"Max iterations reached: {iterations}");

                iterations++;
            } while (bytes != 0);

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

            if (!username.IsEmpty() && !password.IsEmpty())
            {
                if(!_useTLS)
                    throw new Exception($"Username and password were provided but Client is not in TLS mode!");

                login.username = username;
                login.password = password;
            }

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
            _sslStream?.Dispose();
            _client.Dispose();
        }
    }
}
