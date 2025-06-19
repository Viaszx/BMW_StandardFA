using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Linq;
using StandardFA;
using Microsoft.Win32;

namespace StandardFA
{
	public partial class MainForm : Form
	{
		private FAData faData;
		private FASign faSign;
		private UdsMessages udsMessages;
		public byte[] UdsWriteFA = { 0x2E, 0x3F, 0x06 };
		public string StrNotAvaible = "N/A";
		public string BtldData;
		public string Title = "StandardFA: ";
		public MainForm()
		{
			InitializeComponent();
			faData = new FAData();
			faSign = new FASign();
			udsMessages = new UdsMessages();
			cmbEcu.SelectedIndex = 0;
			InitializeTextBoxEvents();
			string savedIp = ReadZgwIpFromRegistry();

			if (!string.IsNullOrEmpty(savedIp)) {
				tbIpZgw.Text = savedIp;
			}
		}
			
		private void InitializeTextBoxEvents()
		{
			tbCreatedBy.TextChanged += (sender, e) => faData.CreatedBy = tbCreatedBy.Text.ToUpper();
			tbDate.TextChanged += (sender, e) => faData.Date = tbDate.Text.ToUpper();
			tbTime.TextChanged += (sender, e) => faData.Time = tbTime.Text.ToUpper();
			tbVin.TextChanged += (sender, e) => faData.Vin = tbVin.Text.ToUpper();
    
			tbFaVer.TextChanged += (sender, e) => {
				faData.FaVersion = tbFaVer.Text.ToUpper();
				UpdateSignature();
			};

			tbTimeCrit.TextChanged += (sender, e) => {
				faData.Zeitkriterium = tbTimeCrit.Text == StrNotAvaible ? null : tbTimeCrit.Text.ToUpper();
				UpdateSignature();
			};

			tbSeries.TextChanged += (sender, e) => {
				faData.Series = tbSeries.Text == StrNotAvaible ? null : tbSeries.Text.ToUpper();
				UpdateSignature();
			};

			tbTypeKey.TextChanged += (sender, e) => {
				faData.Type = tbTypeKey.Text == StrNotAvaible ? null : tbTypeKey.Text.ToUpper();
				UpdateSignature();
			};

			tbFabricCode.TextChanged += (sender, e) => {
				faData.Lackcode = tbFabricCode.Text == StrNotAvaible ? null : tbFabricCode.Text.ToUpper();
				UpdateSignature();
			};

			tbColorCode.TextChanged += (sender, e) => {
				faData.Polstercode = tbColorCode.Text == StrNotAvaible ? null : tbColorCode.Text.ToUpper();
				UpdateSignature();
			};

			tbSalapa.TextChanged += (sender, e) => {
				faData.Salapas = ParseStringToList(tbSalapa.Text.ToUpper());
				UpdateSignature();
			};

			tbEWort.TextChanged += (sender, e) => {
				faData.EWords = ParseStringToList(tbEWort.Text.ToUpper());
				UpdateSignature();
			};

			tbHoWort.TextChanged += (sender, e) => {
				faData.HoWords = ParseStringToList(tbHoWort.Text.ToUpper());
				UpdateSignature();
			};

			tbDate.KeyPress += (sender, e) => RestrictInputToEnglishLettersAndDigits(sender, e);
			tbTime.KeyPress += (sender, e) => RestrictInputToEnglishLettersAndDigits(sender, e);
			tbVin.KeyPress += (sender, e) => RestrictInputToEnglishLettersAndDigits(sender, e);
			tbFaVer.KeyPress += (sender, e) => RestrictInputToEnglishLettersAndDigits(sender, e);
			tbTimeCrit.KeyPress += (sender, e) => RestrictInputToEnglishLettersAndDigits(sender, e);
			tbSeries.KeyPress += (sender, e) => RestrictInputToEnglishLettersAndDigits(sender, e);
			tbTypeKey.KeyPress += (sender, e) => RestrictInputToEnglishLettersAndDigits(sender, e);
			tbFabricCode.KeyPress += (sender, e) => RestrictInputToEnglishLettersAndDigits(sender, e);
			tbColorCode.KeyPress += (sender, e) => RestrictInputToEnglishLettersAndDigits(sender, e);
			tbSalapa.KeyPress += (sender, e) => RestrictInputToEnglishLettersAndDigits(sender, e, ",");
			tbEWort.KeyPress += (sender, e) => RestrictInputToEnglishLettersAndDigits(sender, e, ",");
			tbHoWort.KeyPress += (sender, e) => RestrictInputToEnglishLettersAndDigits(sender, e);


			tbCreatedBy.ContextMenuStrip = new ContextMenuStrip();
			tbDate.ContextMenuStrip = new ContextMenuStrip();
			tbTime.ContextMenuStrip = new ContextMenuStrip();
			tbVin.ContextMenuStrip = new ContextMenuStrip();
			tbFaVer.ContextMenuStrip = new ContextMenuStrip();
			tbTimeCrit.ContextMenuStrip = new ContextMenuStrip();
			tbSeries.ContextMenuStrip = new ContextMenuStrip();
			tbTypeKey.ContextMenuStrip = new ContextMenuStrip();
			tbFabricCode.ContextMenuStrip = new ContextMenuStrip();
			tbColorCode.ContextMenuStrip = new ContextMenuStrip();
			tbSalapa.ContextMenuStrip = new ContextMenuStrip();
			tbEWort.ContextMenuStrip = new ContextMenuStrip();
			tbHoWort.ContextMenuStrip = new ContextMenuStrip();
		}

		private void RestrictInputToEnglishLettersAndDigits(object sender, KeyPressEventArgs e, string allowedChars = "")
		{
			if (Char.IsLetterOrDigit(e.KeyChar) || allowedChars.Contains(e.KeyChar)) {
				if ((e.KeyChar >= 'A' && e.KeyChar <= 'Z') || (e.KeyChar >= 'a' && e.KeyChar <= 'z') || (e.KeyChar >= '0' && e.KeyChar <= '9') || allowedChars.Contains(e.KeyChar)) {
					e.KeyChar = Char.ToUpper(e.KeyChar);
				} else {
					e.Handled = true;
				}
			} else if (e.KeyChar != 8) { 
				e.Handled = true;
			}
		}

		private List<string> ParseStringToList(string text)
		{
			if (string.IsNullOrEmpty(text) || text == StrNotAvaible)
				return new List<string>();

			return text.Split(',')
               .Select(item => item.Trim())
               .Where(item => !string.IsNullOrEmpty(item))
               .OrderBy(item => item)
               .ToList();
		}

		private string ReadZgwIpFromRegistry()
		{
			string registryKeyPath = @"SOFTWARE\StandardFA";
			string registryValueName = "ZGW_IP";

			try {
				object registryValue = Registry.GetValue(@"HKEY_CURRENT_USER\" + registryKeyPath, registryValueName, null);

				return registryValue as string;
			} catch {
				return null;
			}
		}
		
		private void SaveZgwIpToRegistry(string zgwIp)
		{
			string registryKeyPath = @"SOFTWARE\StandardFA";
			string registryValueName = "ZGW_IP";

			try {
				RegistryKey registryKey = Registry.CurrentUser.CreateSubKey(registryKeyPath);

				registryKey.SetValue(registryValueName, zgwIp);

				registryKey.Close();
			} catch (Exception ex) {
				MessageBox.Show("Error writing to registry: " + ex.Message);
			}
		}
		
		private byte[] ConvertHexStringToByteArray(string hex)
		{
			int length = hex.Length;
			byte[] byteArray = new byte[length / 2];
			for (int i = 0; i < length; i += 2) {
				byteArray[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
			}
			return byteArray;
		}
		
		private void UpdateTextFields(FAData faData)
		{			
			tbCreatedBy.Text = faData.CreatedBy;
			tbDate.Text = faData.Date;
			tbTime.Text = faData.Time;
			tbVin.Text = faData.Vin;
				
			tbFaVer.Text = faData.FaVersion;
			tbTimeCrit.Text = faData.Zeitkriterium ?? StrNotAvaible;
			tbSeries.Text = faData.Series ?? StrNotAvaible;
			tbTypeKey.Text = faData.Type ?? StrNotAvaible;
			tbFabricCode.Text = faData.Lackcode ?? StrNotAvaible;
			tbColorCode.Text = faData.Polstercode ?? StrNotAvaible;

			tbSalapa.Text = faData.Salapas.Count > 0 ? string.Join(", ", faData.Salapas) : StrNotAvaible;
			tbEWort.Text = faData.EWords.Count > 0 ? string.Join(", ", faData.EWords) : StrNotAvaible;
			tbHoWort.Text = faData.HoWords.Count > 0 ? string.Join(", ", faData.HoWords) : StrNotAvaible;
		}
		
		private void BtnOpenFaClick(object sender, EventArgs e)
		{
			faData.ClearFAData();

			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = "XML Files|*.xml";
			openFileDialog.Title = "Open FA";

			if (openFileDialog.ShowDialog() == DialogResult.OK) {
				try {
					FAParseXml.ProcessReadXml(openFileDialog.FileName, faData);

					btnWriteFa.Enabled = true;
					btnSaveFa.Enabled = true;
					UpdateTextFields(faData);

					UpdateSignature();

				} catch (Exception ex) {
					MessageBox.Show("The file format is incorrect or there was an error while processing it: " + ex.Message, 
						"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
		}
		
		void BtnWriteFaClick(object sender, EventArgs e)
		{
			try {

				udsMessages.OpenConnection(tbIpZgw.Text, 6801);

				byte[] resultFaData = FAHelper.GetAsStream(faData);
				byte[] byteHMACResult;

				if (BtldData == null) {
					byteHMACResult = faSign.GenerateHMAC(resultFaData, GetSelectedType());
				} else {
					byteHMACResult = faSign.GenerateHMAC(resultFaData, BtldData);
				}
				byte[] byteFaData = ConstructFAData(resultFaData, byteHMACResult);
				byte[] UdsByteArray = UdsMessages.AddCommandToStart(UdsWriteFA, byteFaData);

				bool isSuccess = udsMessages.SendResponseFA(UdsByteArray);
        
				if (isSuccess) {
					ShowUdsCommand(byteFaData);
					MessageBox.Show("Done");
				} else {
					MessageBox.Show("Error: Unexpected response or failure.", 
						"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			} catch (Exception ex) {
				MessageBox.Show("Write FA: " + ex.Message, 
					"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			} finally {
				udsMessages.CloseConnection();
			}
		}

		void ShowUdsCommand(byte[] FAData)
		{
			string UDScom = BitConverter.ToString(UdsWriteFA).Replace("-", "") + BitConverter.ToString(FAData).Replace("-", "");
			tbUds.Text = UDScom;
		}
		
		public static byte[] ConstructFAData(byte[] pData, byte[] pSignature)
		{
			byte[] dataLength = BitConverter.GetBytes((ushort)pData.Length);
			Array.Reverse(dataLength);

			byte[] signatureLength = BitConverter.GetBytes((ushort)pSignature.Length);
			Array.Reverse(signatureLength);

			byte[] transferData = new byte[2 + pData.Length + 2 + pSignature.Length];

			Array.Copy(dataLength, 0, transferData, 0, 2);
			Array.Copy(pData, 0, transferData, 2, pData.Length);
			Array.Copy(signatureLength, 0, transferData, 2 + pData.Length, 2);
			Array.Copy(pSignature, 0, transferData, 2 + pData.Length + 2, pSignature.Length);

			return transferData;
		}
		
		private void SetDateTime()
		{
			DateTime currentDateTime = DateTime.Now;

			TimeZoneInfo localTimeZone = TimeZoneInfo.Local;
			TimeSpan offset = localTimeZone.GetUtcOffset(currentDateTime);

			string formattedDate = currentDateTime.ToString("yyyy-MM-dd");
    
			string timeZoneOffset = offset.ToString(@"hh\:mm");
			if (offset < TimeSpan.Zero) {
				timeZoneOffset = "-" + timeZoneOffset;
			} else {
				timeZoneOffset = "+" + timeZoneOffset;
			}
			tbDate.Text = formattedDate + timeZoneOffset;
			tbTime.Text = currentDateTime.ToString("HH:mm:ss");
			faData.Date = tbDate.Text;
			faData.Time = tbTime.Text;
		}
		
		private void BtnReadFaClick(object sender, EventArgs e)
		{
			try {
				faData.ClearFAData();
				udsMessages.OpenConnection(tbIpZgw.Text, 6801);

				string vinData = udsMessages.SendRequestVin();
				if (vinData != null) {
					SetDateTime();
					tbVin.Text = vinData;
					faData.Vin = tbVin.Text;
					
					BtldData = udsMessages.SendRequestSvk();
					switch (BtldData) {
						case "type1":
							cmbEcu.SelectedItem = 0;
							break;
						case "type2":
							cmbEcu.SelectedItem = 1;
							break;
						default:
							break;
					}
					string FaData = udsMessages.SendRequestFA();        
					byte[] byteArray = ConvertHexStringToByteArray(FaData);
					FAHelper.DecodingStream(byteArray, faData);
					
					UpdateTextFields(faData);	
					byte[] resultFaData = FAHelper.GetAsStream(faData);
					byte[] byteHMACResult = faSign.GenerateHMAC(resultFaData, BtldData);
					byte[] byteFaData = ConstructFAData(resultFaData, byteHMACResult);
					ShowUdsCommand(byteFaData);
					tbCreatedBy.ReadOnly = false;
					btnWriteFa.Enabled = true;
					btnSaveFa.Enabled = true;
		
				}
			} catch (Exception ex) {
				MessageBox.Show("Error: " + ex.Message);
			} finally {				
				udsMessages.CloseConnection();
			}
		}
		
		void BtnSaveFaClick(object sender, EventArgs e)
		{
			try {
				SaveFileDialog saveFileDialog = new SaveFileDialog {
					Filter = "XML Files|*.xml",
					Title = "Save FA"
				};

				if (saveFileDialog.ShowDialog() == DialogResult.OK) {
					string filePath = saveFileDialog.FileName;

					bool isSaved = FAParseXml.CreateXmlFromFaData(faData, filePath);

					if (isSaved) {
						MessageBox.Show("The FA file has been successfully saved!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
					} else {
						MessageBox.Show("There was an error saving the FA file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			} catch (Exception ex) {
				MessageBox.Show("Error during saving: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
		
		private async void BtnFindZgwClick(object sender, EventArgs e)
		{
			ZgwInfo info = await StandardFA.FindZGW.SearchZGWAsync();
    
			if (info != null && info.ZgwIP != null) {
				tbIpZgw.Text = info.ZgwIP;
				this.Text = Title + "ZGW VIN = " + info.ZgwVIN;
				SaveZgwIpToRegistry(info.ZgwIP);
			} else {
				Text = Title + "ZGW not found.";
			}
		}
		
		void CmbEcuSelectedIndexChanged(object sender, EventArgs e)
		{
			UpdateSignature();
		}
		
		private void UpdateSignature()
		{
			string selectedType = GetSelectedType();
			if (BtldData != null || btnWriteFa.Enabled) {
				ProcessFaDataAndShowCommand(selectedType);
			} 
		}
		
		private void ProcessFaDataAndShowCommand(string selectedType)
		{
			byte[] resultFaData = FAHelper.GetAsStream(faData);
			byte[] byteHMACResult = faSign.GenerateHMAC(resultFaData, selectedType);
			byte[] byteFaData = ConstructFAData(resultFaData, byteHMACResult);
			ShowUdsCommand(byteFaData);
			tbUds.SelectAll();
		}

		private string GetSelectedType()
		{
			switch (cmbEcu.SelectedIndex) {
				case 0:
					return "type1";
				case 1:
					return "type2";
				default:
					return BtldData;
			}
		}

		private void TbIpZgwTextChanged(object sender, EventArgs e)
		{
			string input = tbIpZgw.Text;
			int cursorPosition = tbIpZgw.SelectionStart;

			string cleanedInput = new string(input.Where(c => char.IsDigit(c) || c == '.').ToArray());

			string[] parts = cleanedInput.Split('.');

			if (parts.Length > 4) {
				parts = parts.Take(4).ToArray();
			}

			for (int i = 0; i < parts.Length; i++) {
				int num;
				if (int.TryParse(parts[i], out num)) {
					if (num < 0 || num > 255) {
						parts[i] = "255"; 
					}
				}
			}

			string formattedInput = string.Join(".", parts);

			if (formattedInput.Length < input.Length && formattedInput.EndsWith(".")) {
				formattedInput = formattedInput.Substring(0, formattedInput.Length - 1);
			}

			tbIpZgw.Text = formattedInput;

			if (cursorPosition < tbIpZgw.Text.Length) {
				tbIpZgw.SelectionStart = cursorPosition;
			} else {
				tbIpZgw.SelectionStart = tbIpZgw.Text.Length;
			}
		}

		private void TbIpZgwKeyDown(object sender, KeyEventArgs e)
		{
			if (!char.IsDigit((char)e.KeyCode) && e.KeyCode != Keys.Back && e.KeyCode != Keys.OemPeriod) {
				e.SuppressKeyPress = true;
			}
		}
		
		private void TbIpZgwKeyPress(object sender, KeyPressEventArgs e)
		{
			if (e.KeyChar == '.') {
				if (tbIpZgw.Text.Contains(".") && tbIpZgw.Text.LastIndexOf('.') == tbIpZgw.Text.Length - 1) {
					e.Handled = true;
				}
				if (tbIpZgw.Text.Length == 0 || tbIpZgw.Text[0] == '.' || tbIpZgw.Text[tbIpZgw.Text.Length - 1] == '.') {
					e.Handled = true;
				}
			}
		}
		
		private void TbIpZgwMouseClick(object sender, MouseEventArgs e)
		{
			int cursorPosition = tbIpZgw.SelectionStart;
		}
	}
}
