using System;
using System.Collections.Generic;

public class FAData
{
	public string FaVersion { get; set; }
	public string Zeitkriterium { get; set; }
	public string Series { get; set; }
	public string Type { get; set; }
	public string Lackcode { get; set; }
	public string Polstercode { get; set; }

	public string CreatedBy { get; set; }
	public string Date { get; set; }
	public string Time { get; set; }
	public string Vin { get; set; }
    
	public List<string> Salapas { get; set; }
	public List<string> EWords { get; set; }
	public List<string> HoWords { get; set; }

	public FAData()
	{
		Salapas = new List<string>();
		EWords = new List<string>();
		HoWords = new List<string>();
	}
	public void ClearFAData()
	{
		FaVersion = string.Empty;
		Zeitkriterium = string.Empty;
		Series = string.Empty;
		Type = string.Empty;
		Lackcode = string.Empty;
		Polstercode = string.Empty;
		CreatedBy = string.Empty;
		Date = string.Empty;
		Time = string.Empty;
		Vin = string.Empty;
		Salapas.Clear();
		EWords.Clear();
		HoWords.Clear();
	}
}


