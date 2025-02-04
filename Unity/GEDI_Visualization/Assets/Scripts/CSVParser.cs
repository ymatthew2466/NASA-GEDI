using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;

public class CSVParser : MonoBehaviour {
    public string filePath = "Assets/Data/gedi_dataframe.csv";  // Path to the CSV file
    public List<GEDIDataPoint> dataPoints;

    public class GEDIDataPoint{
        public float latitude;
        public float longitude;
        public float elevation;
        public float rh2;
        public float rh50;
        public float rh98;
        public string rawWaveform;
        public string rhWaveform;
        public GEDIDataPoint(float latitude, float longitude, float elevation, float rh2, float rh50, float rh98, string rhWaveform, string rawWaveform)
        {
            this.latitude = latitude;
            this.longitude = longitude;
            this.elevation = elevation;
            this.rh2 = rh2;
            this.rh50 = rh50;
            this.rh98 = rh98;
            this.rhWaveform = rhWaveform;
            this.rawWaveform = rawWaveform;
        }

    }

    /*
    * Loads data from CSV into data structure
    */
    public void loadCSV()
    {
        this.dataPoints = new List<GEDIDataPoint>();
        string[] dataLines = File.ReadAllLines(filePath);

        for (int i = 1; i < dataLines.Length; i++)
        {
            string line = dataLines[i];
            List<string> fields = ParseCSVLine(line);

            // Ensure we have the correct number of fields
            if (fields.Count < 8)
            {
                Debug.LogError($"Incorrect number of fields in line {i}: {line}");
                continue;
            }

            // Parse the fields
            float latitude = float.Parse(fields[0]);
            float longitude = float.Parse(fields[1]);
            float elevation = float.Parse(fields[2]);
            float rh2 = float.Parse(fields[3]);
            float rh50 = float.Parse(fields[4]);
            float rh98 = float.Parse(fields[5]);
            string rhWaveform = fields[6];
            string rawWaveform = fields[7];

            // Create a new data point and add it to the list
            GEDIDataPoint dataPoint = new GEDIDataPoint (latitude, longitude, elevation, rh2, rh50, rh98, rhWaveform, rawWaveform);

            dataPoints.Add(dataPoint);
        }
        // Debug.Log(dataPoints[1].rawWaveform);
    }

    /*
    *  Parses each CSV line according to how it's preprocessed
    */
    public static List<string> ParseCSVLine(string line) {
        List<string> result = new List<string>();
        bool inQuotes = false;
        StringBuilder value = new StringBuilder();

        for (int i = 0; i < line.Length; i+=1)
        {
            char c = line[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (c == ',' && !inQuotes)
            {
                result.Add(value.ToString());
                value.Clear();
            }
            else
            {
                value.Append(c);
            }
    }

        result.Add(value.ToString()); // add the last field
        return result;
    }

    public List<GEDIDataPoint> getDataPoints(){
        // if(this.dataPoints == null){
        //     Debug.Log("dataPoints is null");
        // }
        return this.dataPoints;
    }

    // Start is called before the first frame update
    void Start()
    {
        loadCSV();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
