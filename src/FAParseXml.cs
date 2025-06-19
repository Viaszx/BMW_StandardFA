using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Windows.Forms;
using System.Collections.Generic;

namespace StandardFA
{
	public class FAParseXml
	{

		public static string GetCodesAsString(XmlDocument xmlDoc, string xpath)
		{
			XmlNodeList nodeList = xmlDoc.SelectNodes(xpath, GetNamespaceManager(xmlDoc));
			if (nodeList == null || nodeList.Count == 0)
				return string.Empty;

			var codes = new StringBuilder();
			foreach (XmlNode node in nodeList) {
				if (codes.Length > 0)
					codes.Append(",");
				codes.Append(node.InnerText);
			}
			return codes.ToString();
		}

		private static XmlNamespaceManager GetNamespaceManager(XmlDocument xmlDoc)
		{
			XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
			nsmgr.AddNamespace("ns1", "http://bmw.com/2005/psdz.data.fa");
			return nsmgr;
		}

		public static void ProcessReadXml(string filePath, FAData faData)
		{
			try {
				XmlDocument xmlDoc = new XmlDocument();
				xmlDoc.Load(filePath);

				XmlNode headerNode = xmlDoc.SelectSingleNode("//ns1:header", GetNamespaceManager(xmlDoc));
				XmlNode standardFANode = xmlDoc.SelectSingleNode("//ns1:standardFA", GetNamespaceManager(xmlDoc));
				
				if (headerNode != null) {
					if (headerNode.Attributes["createdBy"] != null)
						faData.CreatedBy = headerNode.Attributes["createdBy"].Value;
					if (headerNode.Attributes["date"] != null)
						faData.Date = headerNode.Attributes["date"].Value;
					if (headerNode.Attributes["time"] != null)
						faData.Time = headerNode.Attributes["time"].Value;
					if (headerNode.Attributes["vinLong"] != null)
						faData.Vin = headerNode.Attributes["vinLong"].Value;
				}

				if (standardFANode != null) {

					if (standardFANode.Attributes["colourCode"] != null)
						faData.Lackcode = standardFANode.Attributes["colourCode"].Value;
					else
						throw new Exception("'colourCode'");

					if (standardFANode.Attributes["faVersion"] != null)
						faData.FaVersion = standardFANode.Attributes["faVersion"].Value;
					else
						throw new Exception("'faVersion'");

					if (standardFANode.Attributes["fabricCode"] != null)
						faData.Polstercode = standardFANode.Attributes["fabricCode"].Value;
					else
						throw new Exception("'fabricCode'");

					if (standardFANode.Attributes["series"] != null)
						faData.Series = standardFANode.Attributes["series"].Value;
					else
						throw new Exception("'series'");

					if (standardFANode.Attributes["timeCriteria"] != null)
						faData.Zeitkriterium = standardFANode.Attributes["timeCriteria"].Value;
					else
						throw new Exception("'timeCriteria'");

					if (standardFANode.Attributes["typeKey"] != null)
						faData.Type = standardFANode.Attributes["typeKey"].Value;
					else
						throw new Exception("'typeKey'");
				} else {
					throw new Exception("'standardFA'");
				}

				faData.EWords = GetCodesAsList(xmlDoc, "//ns1:eCodes/ns1:eCode");
				faData.Salapas = GetCodesAsList(xmlDoc, "//ns1:saCodes/ns1:saCode");
				faData.HoWords = GetCodesAsList(xmlDoc, "//ns1:hoCodes/ns1:hoCode");

			} catch (Exception) {
				throw;
			}
		}

		
		public static bool CreateXmlFromFaData(FAData faData, string filePath)
		{
			string nameSpace = "http://bmw.com/2005/psdz.data.fa";
			try {
				XmlDocument xmlDoc = new XmlDocument();

				XmlDeclaration xmlDeclaration = xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", "yes");
				xmlDoc.AppendChild(xmlDeclaration);

				XmlElement faList = xmlDoc.CreateElement("faList");
				xmlDoc.AppendChild(faList);

				XmlElement idElement = xmlDoc.CreateElement("id");
				idElement.SetAttribute("name", "");
				faList.AppendChild(idElement);

				XmlElement commentElement = xmlDoc.CreateElement("comment");
				idElement.AppendChild(commentElement);
				
				XmlElement faElement = xmlDoc.CreateElement("ns1", "fa", nameSpace);
				idElement.AppendChild(faElement);
				
				XmlElement headerElement = xmlDoc.CreateElement("ns1", "header", nameSpace);
				headerElement.SetAttribute("createdBy", faData.CreatedBy);
				headerElement.SetAttribute("date", faData.Date);
				headerElement.SetAttribute("time", faData.Time);
				headerElement.SetAttribute("vinLong", faData.Vin);
				faElement.AppendChild(headerElement);

				XmlElement standardFAElement = xmlDoc.CreateElement("ns1", "standardFA", nameSpace);
				standardFAElement.SetAttribute("colourCode", faData.Lackcode);
				string faVersionWithoutLeadingZero = faData.FaVersion.TrimStart('0');
				standardFAElement.SetAttribute("faVersion", faVersionWithoutLeadingZero);
				standardFAElement.SetAttribute("fabricCode", faData.Polstercode);
				standardFAElement.SetAttribute("series", faData.Series);
				standardFAElement.SetAttribute("timeCriteria", faData.Zeitkriterium);
				standardFAElement.SetAttribute("typeKey", faData.Type);
				faElement.AppendChild(standardFAElement);

				XmlElement eCodesElement = xmlDoc.CreateElement("ns1", "eCodes", nameSpace);
				foreach (string eWord in faData.EWords) {
					XmlElement eCodeElement = xmlDoc.CreateElement("ns1", "eCode", nameSpace);
					eCodeElement.InnerText = eWord;
					eCodesElement.AppendChild(eCodeElement);
				}
				standardFAElement.AppendChild(eCodesElement);

				XmlElement saCodesElement = xmlDoc.CreateElement("ns1", "saCodes", nameSpace);
				foreach (string saCode in faData.Salapas) {
					XmlElement saCodeElement = xmlDoc.CreateElement("ns1", "saCode", nameSpace);
					saCodeElement.InnerText = saCode;
					saCodesElement.AppendChild(saCodeElement);
				}
				standardFAElement.AppendChild(saCodesElement);

				XmlElement hoCodesElement = xmlDoc.CreateElement("ns1", "hoCodes", nameSpace);
				foreach (string hoWord in faData.HoWords) {
					XmlElement hoCodeElement = xmlDoc.CreateElement("ns1", "hoCode", nameSpace);
					hoCodeElement.InnerText = hoWord;
					hoCodesElement.AppendChild(hoCodeElement);
				}
				standardFAElement.AppendChild(hoCodesElement);

				using (var stream = new FileStream(filePath, FileMode.Create)) {
					var settings = new XmlWriterSettings {
						Encoding = new UTF8Encoding(false),
						Indent = true,
						IndentChars = "    ",
						NewLineHandling = NewLineHandling.Replace,
						NewLineChars = "\n" 
					};

					using (var writer = XmlWriter.Create(stream, settings)) {
						xmlDoc.Save(writer);
					}
				}
				string xmlContent = File.ReadAllText(filePath);
				xmlContent = xmlContent.Replace("utf-8", "UTF-8");
				xmlContent = xmlContent.Replace(" />", "/>");
				xmlContent = xmlContent.TrimEnd() + "\n";
				File.WriteAllText(filePath, xmlContent);

				return true; 
			} catch (Exception ex) {
				MessageBox.Show("Ошибка при создании XML: " + ex.Message);
				return false;
			}
		}
        
		public static List<string> GetCodesAsList(XmlDocument xmlDoc, string xpath)
		{
			XmlNodeList nodeList = xmlDoc.SelectNodes(xpath, GetNamespaceManager(xmlDoc));
			List<string> codes = new List<string>();
			if (nodeList != null) {
				foreach (XmlNode node in nodeList) {
					codes.Add(node.InnerText);
				}
			}
			return codes;
		}
	}
}
