using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Linq;
using StandardFA;

namespace StandardFA
{
    public class UdsMessages
    {
        private TcpClient client;
        private NetworkStream stream;
        byte DiagAdresses = 0xF5;

        public void OpenConnection(string serverAddress, int port)
        {
            try
            {
                client = new TcpClient(serverAddress, port);
                stream = client.GetStream();
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to establish connection: " + ex.Message);
            }
        }
        
        private string SendRequest(byte[] requestBytes, string expectedResponsePattern, int responseLength)
        {
            try
            {
                stream.Write(requestBytes, 0, requestBytes.Length);

                byte[] response = new byte[1024];
                int totalBytesRead = 0;
                int bytesRead;

                while ((bytesRead = stream.Read(response, totalBytesRead, response.Length - totalBytesRead)) > 0)
                {
                    totalBytesRead += bytesRead;

                    if (totalBytesRead >= responseLength) 
                    {
                        break;
                    }
                }

                string responseData = BitConverter.ToString(response, 0, totalBytesRead).Replace("-", " ");

                if (responseData.Contains(expectedResponsePattern))
                {
                    return responseData;
                }

                return "Expected pattern not found in response.";
            }
            catch (Exception ex) {
				return "N/A"; 
			}
		}
		public static byte[] AddCommandToStart(byte[] firstArray, byte[] secondArray)
		{

			byte[] result = new byte[firstArray.Length + secondArray.Length];

			Buffer.BlockCopy(firstArray, 0, result, 0, firstArray.Length);

			Buffer.BlockCopy(secondArray, 0, result, firstArray.Length, secondArray.Length);

			return result;
		}

		public string SendRequestVin()
		{
            byte[] RequestVin = new byte[] { 0x00, 0x00, 0x00, 0x05, 0x00, 0x01, 0xF4, 0x10, 0x22, 0xF1, 0x90 };
            RequestVin[6] = DiagAdresses;

            string responseData = SendRequest(RequestVin, "62 F1 90", 32);

            if (responseData != "Expected pattern not found in response." && responseData != "N/A")
            {
                return ExtractVinData(responseData);
            }

            return responseData;
        }

        public string SendRequestSvk()
        {
            byte[] requestSvk = new byte[] { 0x00, 0x00, 0x00, 0x05, 0x00, 0x01, 0xF4, 0x10, 0x22, 0xF1, 0x01 };
            requestSvk[6] = DiagAdresses;

            string responseData = SendRequest(requestSvk, "06 00 00", 32);

            if (responseData != "Expected pattern not found in response." && responseData != "N/A")
            {
                return ExtractSvkResponse(responseData);
            }

            return responseData;
        }

        public string SendRequestFA()
        {
            byte[] requestSvk = new byte[] { 0x00, 0x00, 0x00, 0x05, 0x00, 0x01, 0xF4, 0x10, 0x22, 0x3F, 0x06 };
            requestSvk[6] = DiagAdresses;
            
            string responseData = SendRequest(requestSvk, "62 3F 06", 32);

            if (responseData != "Expected pattern not found in response." && responseData != "N/A")
            {
				return ExtractFAResponse(responseData);
			}

			return responseData;
		}
        
		public bool SendResponseFA(byte[] responseBytes)
		{
			byte lengthByte = GetLengthInBigEndian(responseBytes.Length);
   			byte[] responseFA = new byte[] { 0x00, 0x00, 0x00, 0x05, 0x00, 0x01, 0xF4, 0x10 };

			responseFA[3] = (byte)(lengthByte + 2);
			responseFA[6] = DiagAdresses;

			byte[] finalResponse = UdsMessages.AddCommandToStart(responseFA, responseBytes);
			
			string responseData = SendRequest(finalResponse, "6E 3F 06", 22);

			if (responseData != "Expected pattern not found in response." && responseData != "N/A") {
				return responseData.Contains("6E 3F 06");
			}

			return false;
		}


		public static byte GetLengthInBigEndian(int length)
		{
			return (byte)(length & 0xFF);
		}

		private string ExtractVinData(string responseData)
		{
			int index = responseData.IndexOf("62 F1 90");

			if (index == -1) {
				return "Unexpected response";
			}

			string[] dataParts = responseData.Split(' ');
			int startIndex = index / 3 + 3;

            if (dataParts.Length >= startIndex + 17)
            {
                byte[] vinBytes = new byte[17];
                for (int i = 0; i < 17; i++)
                {
                    vinBytes[i] = Convert.ToByte(dataParts[startIndex + i], 16);
                }

                return Encoding.ASCII.GetString(vinBytes);
            }
            return "vin error";
		}

		private string ExtractSvkResponse(string responseData)
		{
			int index = responseData.IndexOf("06 00 00") + 9;
			string nextBytes = responseData.Substring(index, 5); 

			if (nextBytes.Contains("04 52") || nextBytes.Contains("07 9C") || nextBytes.Contains("09 29") || nextBytes.Contains("10 F5") || nextBytes.Contains("16 B3") || nextBytes.Contains("17 B5")) {
				return "type1";
			} else if (nextBytes.Contains("64 F8") || nextBytes.Contains("41 39") || nextBytes.Contains("1D F1")) {
				return "type2";
			}
			return "Unexpected response";
		}

        private string ExtractFAResponse(string responseData)
        {
            int index = responseData.IndexOf("62 3F 06");

            if (index == -1)
            {
                return "Pattern '62 3F 06' not found.";
            }
            int startIndex = index + "62 3F 06".Length;

            string dataAfterPattern = responseData.Substring(startIndex).Trim();

            dataAfterPattern = dataAfterPattern.Replace(" ", "");
            
            if (dataAfterPattern.Length < 4)
            {
                return "Insufficient data to extract length.";
            }

            string lengthBytes = dataAfterPattern.Substring(0, 4); 

            int length = Convert.ToInt32(lengthBytes, 16);

            if (dataAfterPattern.Length < 4 + length * 2)
            {
                return "Data length exceeds available bytes.";
            }

            string extractedData = dataAfterPattern.Substring(4, length * 2);

            return extractedData;
        }

        public void CloseConnection()
        {
                stream.Close();
                client.Close();
        }
    }
}
